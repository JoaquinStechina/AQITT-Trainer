using System;
using UnityEngine;

namespace AQT
{
    // Entry point: the injector calls AQT.Loader.Load() from a native thread
    // (not Unity's main thread). That is why we do NOT create Unity objects here:
    // we leave a hook that runs on the main thread as soon as the game
    // renders a frame (or the window focus changes), and only then do we create the trainer.
    public static class Loader
    {
        internal const string DomainKey = "AQT.Trainer.Loaded";
        private static readonly object Gate = new object();
        private static bool _created;

        public static void Load()
        {
            try
            {
                if (AppDomain.CurrentDomain.GetData(DomainKey) != null)
                {
                    Log.Info("Already loaded; ignoring the second injection.");
                    return;
                }
                AppDomain.CurrentDomain.SetData(DomainKey, "1");
                Log.Info("Load() executed. Waiting for Unity's main thread...");

                Application.onBeforeRender += OnMainThreadHook;
                Application.focusChanged += OnFocusChanged;
            }
            catch (Exception e)
            {
                Log.Error("Loader.Load", e);
                throw;
            }
        }

        // Called by the menu when unloading the trainer, to allow a new injection.
        internal static void ResetForReload()
        {
            lock (Gate) { _created = false; }
        }

        private static void OnFocusChanged(bool focus)
        {
            OnMainThreadHook();
        }

        private static void OnMainThreadHook()
        {
            lock (Gate)
            {
                if (_created) return;
                _created = true;
            }

            try
            {
                Application.onBeforeRender -= OnMainThreadHook;
                Application.focusChanged -= OnFocusChanged;

                GameObject go = new GameObject("AQT_Trainer");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.HideAndDontSave;
                go.AddComponent<TrainerBehaviour>();
                Log.Info("Trainer created on the main thread. Menu: F10 or Insert.");
            }
            catch (Exception e)
            {
                Log.Error("Loader.OnMainThreadHook", e);
                lock (Gate) { _created = false; }
                // retry on the next frame
                Application.onBeforeRender += OnMainThreadHook;
                Application.focusChanged += OnFocusChanged;
            }
        }
    }
}
