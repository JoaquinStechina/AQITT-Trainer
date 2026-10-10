/*
 * aqt-inject - attaches to a running game with ptrace and makes it dlopen() libaqtloader.so (Linux, x86-64).
 *
 *   aqt-inject [pid | name-fragment] [--lib /path/to/libaqtloader.so]
 *
 * Without arguments it looks for a process whose command line contains "allquiet" (spaces ignored, any case).
 * Needs permission to ptrace the game: run it as root (sudo), or set /proc/sys/kernel/yama/ptrace_scope to 0.
 * If you cannot or do not want to do that, use the LD_PRELOAD launch option instead (see README.md).
 */
#define _GNU_SOURCE
#include <ctype.h>
#include <dirent.h>
#include <dlfcn.h>
#include <errno.h>
#include <limits.h>
#include <signal.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/ptrace.h>
#include <sys/types.h>
#include <sys/user.h>
#include <sys/wait.h>
#include <unistd.h>

#ifndef __x86_64__
#error "aqt-inject only supports x86-64. On other CPUs use the LD_PRELOAD launch option."
#endif

#define LIB_FILE "libaqtloader.so"

/* own parsers: avoid the glibc 2.38-only __isoc23_strto* symbols so the binary also runs on older glibc */
static unsigned long long parse_hex(const char *s)
{
    unsigned long long v = 0;
    for (;; s++) {
        int c = (unsigned char)*s, d;
        if (c >= '0' && c <= '9') d = c - '0';
        else if (c >= 'a' && c <= 'f') d = c - 'a' + 10;
        else if (c >= 'A' && c <= 'F') d = c - 'A' + 10;
        else break;
        v = (v << 4) | (unsigned)d;
    }
    return v;
}

static int parse_int(const char *s)
{
    int v = 0;
    while (*s >= '0' && *s <= '9') v = v * 10 + (*s++ - '0');
    return v;
}

static int die(const char *msg)
{
    fprintf(stderr, "ERROR: %s\n", msg);
    return 1;
}

/* ------------------------------------------------------------------ finding the game */

static int cmdline_matches(const char *pid, const char *needle)
{
    char path[320], buf[4096];
    snprintf(path, sizeof path, "/proc/%s/cmdline", pid);
    FILE *f = fopen(path, "r");
    if (!f) return 0;
    size_t n = fread(buf, 1, sizeof buf - 1, f);
    fclose(f);
    char flat[4096];
    size_t j = 0;
    for (size_t i = 0; i < n && j < sizeof flat - 1; i++) {
        char c = buf[i];
        if (c == 0 || c == ' ') continue;
        flat[j++] = (char)tolower((unsigned char)c);
    }
    flat[j] = 0;

    char low[256];
    size_t k = 0;
    for (const char *p = needle; *p && k < sizeof low - 1; p++)
        if (*p != ' ') low[k++] = (char)tolower((unsigned char)*p);
    low[k] = 0;
    return k > 0 && strstr(flat, low) != NULL;
}

static pid_t find_game(const char *needle)
{
    DIR *d = opendir("/proc");
    if (!d) return 0;
    struct dirent *e;
    pid_t found = 0;
    pid_t self = getpid();
    while ((e = readdir(d)) != NULL) {
        if (!isdigit((unsigned char)e->d_name[0])) continue;
        pid_t pid = (pid_t)parse_int(e->d_name);
        if (pid == self) continue;
        /* skip Steam wrappers / this tool: we want the process that has Mono loaded */
        if (!cmdline_matches(e->d_name, needle)) continue;
        char maps[320], line[1024];
        snprintf(maps, sizeof maps, "/proc/%s/maps", e->d_name);
        FILE *f = fopen(maps, "r");
        if (!f) { if (!found) found = pid; continue; }
        int has_mono = 0;
        while (fgets(line, sizeof line, f)) if (strstr(line, "mono")) { has_mono = 1; break; }
        fclose(f);
        if (has_mono) { found = pid; break; }
        if (!found) found = pid;
    }
    closedir(d);
    return found;
}

/* ------------------------------------------------------------------ address of dlopen in the target */

/* base address of the lowest mapping of a file whose name ends with "base_name" */
static uintptr_t module_base(pid_t pid, const char *base_name)
{
    char path[64], line[1024];
    snprintf(path, sizeof path, "/proc/%d/maps", (int)pid);
    FILE *f = fopen(path, "r");
    if (!f) return 0;
    uintptr_t best = 0;
    while (fgets(line, sizeof line, f)) {
        char *slash = strrchr(line, '/');
        if (!slash) continue;
        char *nl = strchr(slash, '\n');
        if (nl) *nl = 0;
        if (strcmp(slash + 1, base_name) != 0) continue;
        uintptr_t start = (uintptr_t)parse_hex(line);
        if (best == 0 || start < best) best = start;
    }
    fclose(f);
    return best;
}

static uintptr_t remote_dlopen(pid_t pid)
{
    void *mine = dlsym(RTLD_DEFAULT, "dlopen");
    Dl_info info;
    if (!mine || !dladdr(mine, &info) || !info.dli_fname) return 0;
    const char *slash = strrchr(info.dli_fname, '/');
    const char *name = slash ? slash + 1 : info.dli_fname;
    uintptr_t base = module_base(pid, name);
    if (!base) return 0;
    return base + ((uintptr_t)mine - (uintptr_t)info.dli_fbase);
}

/* ------------------------------------------------------------------ ptrace helpers */

static int poke_bytes(pid_t pid, uintptr_t addr, const void *data, size_t len)
{
    const unsigned char *src = (const unsigned char *)data;
    for (size_t off = 0; off < len; off += sizeof(long)) {
        long word = 0;
        size_t chunk = len - off < sizeof(long) ? len - off : sizeof(long);
        if (chunk < sizeof(long)) {
            errno = 0;
            word = ptrace(PTRACE_PEEKDATA, pid, (void *)(addr + off), NULL);
            if (errno) return -1;
        }
        memcpy(&word, src + off, chunk);
        if (ptrace(PTRACE_POKEDATA, pid, (void *)(addr + off), (void *)word) == -1) return -1;
    }
    return 0;
}

/* Runs fn(arg1, arg2) inside the stopped thread and returns its result in *ret. The thread's registers are restored. */
static int remote_call(pid_t pid, uintptr_t fn, uintptr_t arg1, uintptr_t arg2, const char *str, uintptr_t *ret)
{
    struct user_regs_struct saved, regs;
    if (ptrace(PTRACE_GETREGS, pid, NULL, &saved) == -1) return -1;
    regs = saved;

    size_t slen = strlen(str) + 1;
    uintptr_t str_addr = (saved.rsp - 2048) & ~(uintptr_t)0xF;   /* well below the red zone */
    uintptr_t new_sp = str_addr - 256;                             /* 16-byte aligned */
    if (poke_bytes(pid, str_addr, str, slen) != 0) return -1;

    /* return address 0: when the function returns the thread faults with SIGSEGV at rip == 0, which we catch */
    new_sp -= 8;
    uint64_t zero = 0;
    if (poke_bytes(pid, new_sp, &zero, sizeof zero) != 0) return -1;

    regs.rsp = new_sp;               /* rsp % 16 == 8 at function entry, as the ABI requires */
    regs.rip = fn;
    regs.rdi = arg1 ? arg1 : str_addr;
    regs.rsi = arg2;
    regs.rax = 0;
    regs.orig_rax = (unsigned long long)-1;   /* never restart an interrupted system call */
    if (ptrace(PTRACE_SETREGS, pid, NULL, &regs) == -1) return -1;

    int sig = 0;
    for (;;) {
        if (ptrace(PTRACE_CONT, pid, NULL, (void *)(long)sig) == -1) return -1;
        int status;
        if (waitpid(pid, &status, __WALL) == -1) return -1;
        if (WIFEXITED(status) || WIFSIGNALED(status)) return -1;
        if (!WIFSTOPPED(status)) continue;
        sig = WSTOPSIG(status);
        if (sig == SIGSEGV) {
            struct user_regs_struct after;
            if (ptrace(PTRACE_GETREGS, pid, NULL, &after) == -1) return -1;
            if (after.rip == 0) { *ret = (uintptr_t)after.rax; break; }   /* our return */
            fprintf(stderr, "The game crashed inside dlopen (rip=0x%llx).\n", after.rip);
            return -1;
        }
        /* any other signal (Mono uses a few for GC suspension): hand it back to the thread */
    }

    return ptrace(PTRACE_SETREGS, pid, NULL, &saved) == -1 ? -1 : 0;
}

/* ------------------------------------------------------------------ main */

int main(int argc, char **argv)
{
    const char *target = NULL, *lib_arg = NULL;
    for (int i = 1; i < argc; i++) {
        if (strcmp(argv[i], "--lib") == 0 && i + 1 < argc) lib_arg = argv[++i];
        else if (strcmp(argv[i], "-h") == 0 || strcmp(argv[i], "--help") == 0) {
            printf("Usage: %s [pid | name-fragment] [--lib /path/to/%s]\n", argv[0], LIB_FILE);
            return 0;
        }
        else target = argv[i];
    }

    char lib[PATH_MAX];
    if (lib_arg) {
        if (!realpath(lib_arg, lib)) return die("cannot find the library given with --lib");
    } else {
        char self[PATH_MAX];
        ssize_t n = readlink("/proc/self/exe", self, sizeof self - 1);
        if (n <= 0) return die("cannot locate this program");
        self[n] = 0;
        char *slash = strrchr(self, '/');
        if (slash) *slash = 0;
        char cand[PATH_MAX + 32];
        snprintf(cand, sizeof cand, "%s/%s", self, LIB_FILE);
        if (!realpath(cand, lib)) return die("libaqtloader.so not found next to aqt-inject (build it with ./build.sh or use --lib)");
    }

    printf("=== All Quiet in the Trenches - Trainer (Linux) ===\n");
    pid_t pid = 0;
    if (target && target[0] && isdigit((unsigned char)target[0])) pid = (pid_t)parse_int(target);
    else pid = find_game(target ? target : "allquiet");
    if (pid <= 0) {
        fprintf(stderr, "ERROR: cannot find the running game.\n"
                        "Start 'All Quiet in the Trenches' and run this program again, or pass the pid / part of the name.\n");
        return 3;
    }
    printf("Game found: pid %d\nLibrary: %s\n", (int)pid, lib);

    uintptr_t dl = remote_dlopen(pid);
    if (!dl) return die("cannot locate dlopen() in the game process (different libc, or no permission to read its maps)");

    if (ptrace(PTRACE_ATTACH, pid, NULL, NULL) == -1) {
        fprintf(stderr, "ERROR: ptrace attach failed: %s\n", strerror(errno));
        if (errno == EPERM)
            fprintf(stderr, "Run this program with sudo, or run: echo 0 | sudo tee /proc/sys/kernel/yama/ptrace_scope\n"
                            "Alternative without root: use the LD_PRELOAD launch option (see README.md).\n");
        return 4;
    }
    int status;
    if (waitpid(pid, &status, __WALL) == -1 || !WIFSTOPPED(status)) {
        ptrace(PTRACE_DETACH, pid, NULL, NULL);
        return die("the game did not stop for ptrace");
    }

    uintptr_t handle = 0;
    int rc = remote_call(pid, dl, 0, 2 /* RTLD_NOW */, lib, &handle);
    ptrace(PTRACE_DETACH, pid, NULL, NULL);

    if (rc != 0) return die("could not run dlopen() inside the game");
    if (handle == 0) return die("dlopen() failed inside the game (check the path and that the library matches the game's architecture)");

    printf("Injected OK (library handle 0x%lx).\n", (unsigned long)handle);
    printf("The agent loads in a few seconds. Go back to the game window.\n"
           "  - Show/hide menu: F10 or Insert\n  - Diagnostic log: %s/AQTrainer.log\n",
           getenv("TMPDIR") && *getenv("TMPDIR") ? getenv("TMPDIR") : "/tmp");
    return 0;
}
