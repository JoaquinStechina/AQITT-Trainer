using System;
using UnityEngine;

namespace AQT
{
    // Punto de entrada: el inyector llama a AQT.Loader.Load() desde un hilo nativo
    // (no el hilo principal de Unity). Por eso NO creamos objetos de Unity aqui:
    // dejamos un gancho que se ejecuta en el hilo principal en cuanto el juego
    // renderiza un frame (o cambia el foco de la ventana) y recien ahi creamos el trainer.
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
                    Log.Info("Ya estaba cargado; se ignora la segunda inyeccion.");
                    return;
                }
                AppDomain.CurrentDomain.SetData(DomainKey, "1");
                Log.Info("Load() ejecutado. Esperando al hilo principal de Unity...");

                Application.onBeforeRender += OnMainThreadHook;
                Application.focusChanged += OnFocusChanged;
            }
            catch (Exception e)
            {
                Log.Error("Loader.Load", e);
                throw;
            }
        }

        // Lo llama el menu al descargar el trainer, para permitir una nueva inyeccion.
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
                Log.Info("Trainer creado en el hilo principal. Menu: F10 o Insert.");
            }
            catch (Exception e)
            {
                Log.Error("Loader.OnMainThreadHook", e);
                lock (Gate) { _created = false; }
                // reintentar en el proximo frame
                Application.onBeforeRender += OnMainThreadHook;
                Application.focusChanged += OnFocusChanged;
            }
        }
    }
}
