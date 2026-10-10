/*
 * libaqtloader.so - native loader that puts AQTAgent.dll into the Mono runtime of the game (Linux).
 *
 * It does the same job as the Windows injector (SharpMonoInjector): attach a thread to the root Mono domain,
 * load AQTAgent.dll from memory and call AQT.Loader.Load(). It can be used in two ways:
 *
 *   1) LD_PRELOAD (recommended, no root needed): set the Steam launch option
 *          LD_PRELOAD="/path/to/AQTrainer/linux/libaqtloader.so" %command%
 *      The constructor below starts a background thread that waits until Mono and Assembly-CSharp are loaded.
 *
 *   2) Attach to a running game with aqt-inject, which makes the game call dlopen() on this library.
 *
 * Environment variables (all optional):
 *   AQT_AGENT          full path to AQTAgent.dll (default: next to this library, then ./AQTAgent.dll)
 *   AQT_WAIT_ASSEMBLY  assembly to wait for before injecting (default "Assembly-CSharp"; "-" = do not wait)
 *   AQT_DELAY_MS       extra delay after that assembly is loaded (default 2000)
 *
 * Log: $TMPDIR/AQTrainer.log (or /tmp/AQTrainer.log) - the same file the managed agent writes to.
 */
#define _GNU_SOURCE
#include <dlfcn.h>
#include <pthread.h>
#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <unistd.h>

#define AGENT_FILE   "AQTAgent.dll"
#define MAX_WAIT_SEC 600

static pthread_once_t g_once = PTHREAD_ONCE_INIT;

/* ------------------------------------------------------------------ log */

static void log_line(const char *fmt, ...)
{
    const char *tmp = getenv("TMPDIR");
    char path[1024];
    snprintf(path, sizeof path, "%s/AQTrainer.log", (tmp && *tmp) ? tmp : "/tmp");

    FILE *f = fopen(path, "a");
    if (!f) return;

    time_t t = time(NULL);
    struct tm tmv;
    localtime_r(&t, &tmv);
    fprintf(f, "%02d:%02d:%02d INFO  [loader] ", tmv.tm_hour, tmv.tm_min, tmv.tm_sec);

    va_list ap;
    va_start(ap, fmt);
    vfprintf(f, fmt, ap);
    va_end(ap);
    fputc('\n', f);
    fclose(f);
}

/* ------------------------------------------------------------------ mono API (resolved at run time) */

typedef void *(*fn_root_domain)(void);
typedef void *(*fn_thread_attach)(void *domain);
typedef void (*fn_thread_detach)(void *thread);
typedef void *(*fn_name_new)(const char *name);
typedef void (*fn_name_free)(void *name);
typedef void *(*fn_assembly_loaded)(void *name);
typedef void *(*fn_open_from_data)(char *data, unsigned len, int need_copy, int *status);
typedef void *(*fn_load_from_full)(void *image, const char *fname, int *status, int refonly);
typedef void *(*fn_get_image)(void *assembly);
typedef void *(*fn_class_from_name)(void *image, const char *ns, const char *name);
typedef void *(*fn_get_method)(void *klass, const char *name, int param_count);
typedef void *(*fn_invoke)(void *method, void *obj, void **params, void **exc);
typedef const char *(*fn_strerror)(int status);
typedef void *(*fn_obj_to_string)(void *obj, void **exc);
typedef char *(*fn_string_utf8)(void *str);
typedef void (*fn_free)(void *p);

static struct {
    void *handle;
    fn_root_domain root_domain;
    fn_thread_attach thread_attach;
    fn_thread_detach thread_detach;
    fn_name_new name_new;
    fn_name_free name_free;
    fn_assembly_loaded assembly_loaded;
    fn_open_from_data open_from_data;
    fn_load_from_full load_from_full;
    fn_get_image get_image;
    fn_class_from_name class_from_name;
    fn_get_method get_method;
    fn_invoke invoke;
    fn_strerror strerror_;
    fn_obj_to_string obj_to_string;
    fn_string_utf8 string_utf8;
    fn_free mfree;
} M;

/* Finds the library that exports mono_get_root_domain: first in the global scope, then by scanning our own mappings
 * (the game may have loaded libmonobdwgc-2.0.so with RTLD_LOCAL). */
static int find_mono_handle(void **out)
{
    /* note: RTLD_DEFAULT is (void *)0 on glibc, so success is reported through the return value */
    if (dlsym(RTLD_DEFAULT, "mono_get_root_domain")) { *out = RTLD_DEFAULT; return 1; }

    FILE *f = fopen("/proc/self/maps", "r");
    if (!f) return 0;
    char line[2048];
    void *found = NULL;
    while (!found && fgets(line, sizeof line, f)) {
        char *p = strchr(line, '/');
        if (!p || !strstr(p, "libmono")) continue;
        char *nl = strchr(p, '\n');
        if (nl) *nl = 0;
        void *h = dlopen(p, RTLD_NOW | RTLD_NOLOAD);
        if (h && dlsym(h, "mono_get_root_domain")) found = h;
    }
    fclose(f);
    if (!found) return 0;
    *out = found;
    return 1;
}

#define RESOLVE(field, name) M.field = (void *)dlsym(M.handle, name)

static int resolve_mono(void)
{
    void *h = NULL;
    if (!find_mono_handle(&h)) return 0;
    M.handle = h;
    RESOLVE(root_domain, "mono_get_root_domain");
    RESOLVE(thread_attach, "mono_thread_attach");
    RESOLVE(thread_detach, "mono_thread_detach");
    RESOLVE(name_new, "mono_assembly_name_new");
    RESOLVE(name_free, "mono_assembly_name_free");
    RESOLVE(assembly_loaded, "mono_assembly_loaded");
    RESOLVE(open_from_data, "mono_image_open_from_data");
    RESOLVE(load_from_full, "mono_assembly_load_from_full");
    RESOLVE(get_image, "mono_assembly_get_image");
    RESOLVE(class_from_name, "mono_class_from_name");
    RESOLVE(get_method, "mono_class_get_method_from_name");
    RESOLVE(invoke, "mono_runtime_invoke");
    RESOLVE(strerror_, "mono_image_strerror");
    RESOLVE(obj_to_string, "mono_object_to_string");
    RESOLVE(string_utf8, "mono_string_to_utf8");
    RESOLVE(mfree, "mono_free");

    const char *missing = !M.root_domain ? "mono_get_root_domain" : !M.thread_attach ? "mono_thread_attach" :
        !M.open_from_data ? "mono_image_open_from_data" : !M.load_from_full ? "mono_assembly_load_from_full" :
        !M.get_image ? "mono_assembly_get_image" : !M.class_from_name ? "mono_class_from_name" :
        !M.get_method ? "mono_class_get_method_from_name" : !M.invoke ? "mono_runtime_invoke" : NULL;
    if (missing) {
        static int reported;
        if (!reported) { reported = 1; log_line("this Mono does not export %s", missing); }
        return 0;
    }
    return 1;
}

/* ------------------------------------------------------------------ helpers */

/* own parser: avoids the glibc 2.38-only __isoc23_strtol symbol so the binary also runs on older glibc */
static int parse_int(const char *s)
{
    int v = 0;
    while (*s >= '0' && *s <= '9') v = v * 10 + (*s++ - '0');
    return v;
}

static void msleep(int ms)
{
    struct timespec ts = { ms / 1000, (long)(ms % 1000) * 1000000L };
    nanosleep(&ts, NULL);
}

static void agent_path(char *out, size_t cap)
{
    const char *env = getenv("AQT_AGENT");
    if (env && *env) { snprintf(out, cap, "%s", env); return; }

    Dl_info info;
    if (dladdr((void *)&agent_path, &info) && info.dli_fname) {
        char dir[1024];
        snprintf(dir, sizeof dir, "%s", info.dli_fname);
        char *slash = strrchr(dir, '/');
        if (slash) {
            *slash = 0;
            snprintf(out, cap, "%s/%s", dir, AGENT_FILE);
            if (access(out, R_OK) == 0) return;
            /* the library sits in linux/, the dll in the folder above */
            snprintf(out, cap, "%s/../%s", dir, AGENT_FILE);
            if (access(out, R_OK) == 0) return;
        }
    }
    snprintf(out, cap, "%s", AGENT_FILE);
}

static char *read_file(const char *path, long *size)
{
    FILE *f = fopen(path, "rb");
    if (!f) return NULL;
    fseek(f, 0, SEEK_END);
    long n = ftell(f);
    fseek(f, 0, SEEK_SET);
    if (n <= 0) { fclose(f); return NULL; }
    char *buf = (char *)malloc((size_t)n);
    if (buf && fread(buf, 1, (size_t)n, f) != (size_t)n) { free(buf); buf = NULL; }
    fclose(f);
    *size = n;
    return buf;
}

/* true if the assembly with this simple name is already loaded in the domain. Must run on an attached thread. */
static int assembly_is_loaded(const char *name)
{
    if (!M.name_new || !M.assembly_loaded) return 1; /* cannot check: do not block */
    void *an = M.name_new(name);
    if (!an) return 0;
    void *asmb = M.assembly_loaded(an);
    if (M.name_free) M.name_free(an);
    return asmb != NULL;
}

/* ------------------------------------------------------------------ injection */

static int inject_agent(void)
{
    char path[1100];
    agent_path(path, sizeof path);
    long size = 0;
    char *data = read_file(path, &size);
    if (!data) { log_line("cannot read %s", path); return 0; }
    log_line("injecting %s (%ld bytes)", path, size);

    void *domain = M.root_domain();
    if (!domain) { log_line("mono_get_root_domain() returned NULL"); free(data); return 0; }

    void *thread = M.thread_attach(domain);
    int ok = 0;
    int status = 0;

    void *image = M.open_from_data(data, (unsigned)size, 1, &status);
    if (!image || status != 0) {
        log_line("mono_image_open_from_data() failed: %s", M.strerror_ ? M.strerror_(status) : "?");
        goto done;
    }
    status = 0;
    void *assembly = M.load_from_full(image, "", &status, 0);
    if (!assembly || status != 0) {
        log_line("mono_assembly_load_from_full() failed: %s", M.strerror_ ? M.strerror_(status) : "?");
        goto done;
    }
    void *img = M.get_image(assembly);
    void *klass = img ? M.class_from_name(img, "AQT", "Loader") : NULL;
    if (!klass) { log_line("class AQT.Loader not found in the agent"); goto done; }
    void *method = M.get_method(klass, "Load", 0);
    if (!method) { log_line("method AQT.Loader.Load() not found"); goto done; }

    void *exc = NULL;
    M.invoke(method, NULL, NULL, &exc);
    if (exc) {
        char *msg = NULL;
        if (M.obj_to_string && M.string_utf8) {
            void *s = M.obj_to_string(exc, NULL);
            if (s) msg = M.string_utf8(s);
        }
        log_line("AQT.Loader.Load() threw: %s", msg ? msg : "(unknown exception)");
        if (msg && M.mfree) M.mfree(msg);
        goto done;
    }
    ok = 1;
    log_line("injected OK. Menu: F10 or Insert.");

done:
    if (thread && M.thread_detach) M.thread_detach(thread);
    free(data);
    return ok;
}

static void *worker(void *arg)
{
    (void)arg;
    int waited_ms = 0;
    while (!resolve_mono()) {
        if (waited_ms > MAX_WAIT_SEC * 1000) return NULL;   /* not a Mono process (e.g. a Steam helper): stay silent */
        msleep(250);
        waited_ms += 250;
    }
    log_line("Mono found in pid %d, waiting for the game to finish loading...", (int)getpid());
    while (!M.root_domain()) {
        if (waited_ms > MAX_WAIT_SEC * 1000) { log_line("Mono root domain never appeared; giving up."); return NULL; }
        msleep(250);
        waited_ms += 250;
    }

    const char *wait_for = getenv("AQT_WAIT_ASSEMBLY");
    if (!wait_for || !*wait_for) wait_for = "Assembly-CSharp";
    if (strcmp(wait_for, "-") != 0) {
        for (;;) {
            void *t = M.thread_attach(M.root_domain());
            int loaded = assembly_is_loaded(wait_for);
            if (t && M.thread_detach) M.thread_detach(t);
            if (loaded) break;
            if (waited_ms > MAX_WAIT_SEC * 1000) { log_line("%s never loaded; giving up.", wait_for); return NULL; }
            msleep(500);
            waited_ms += 500;
        }
        const char *d = getenv("AQT_DELAY_MS");
        msleep((d && *d) ? parse_int(d) : 2000);
    }

    inject_agent();
    return NULL;
}

static void start_once(void)
{
    pthread_t th;
    pthread_attr_t attr;
    pthread_attr_init(&attr);
    pthread_attr_setdetachstate(&attr, PTHREAD_CREATE_DETACHED);
    if (pthread_create(&th, &attr, worker, NULL) != 0) log_line("could not start the loader thread");
    pthread_attr_destroy(&attr);
}

/* Runs when the library is loaded (LD_PRELOAD at start-up, or dlopen from aqt-inject). */
__attribute__((constructor)) static void aqt_init(void)
{
    pthread_once(&g_once, start_once);
}
