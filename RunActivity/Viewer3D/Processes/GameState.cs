using TOURMALINE.Common;
using Tourmaline.Common;
using System;

namespace Tourmaline.Viewer3D.Processes
{
    /// <summary>
    /// Representa el estado en que se encuentra el juego (cargando, ejecutando, en el menú).
    /// </summary>
    public abstract class GameState
    {
        internal Game Game { get; set; }

        /// <summary>
        /// Esta rutina se invoca justo antes de pintar un frame.
        /// </summary>
        /// <param name="frame">El <see cref="RenderFrame"/> que contiene todo lo necesario a pintar.</param>
        [CallOnThread("Render")]
        internal virtual void BeginRender(RenderFrame frame)
        {
        }

        /// <summary>
        /// Esta rutina se invoca justo después de pintar un frame.
        /// </summary>
        /// <param name="frame">El <see cref="RenderFrame"/> que contiene todo lo necesario a pintar.</param>
        [CallOnThread("Render")]
        internal virtual void EndRender(RenderFrame frame)
        {
        }

        /// <summary>
        /// Esta rutina se invoca para actualizar el juego y poblar un nuevo <see cref="RenderFrame"/>.
        /// </summary>
        /// <param name="frame">El nuevo <see cref="RenderFrame"/> que hay que poblar.</param>
        /// <param name="now">Tiempo en ticks desde el comienzo de la simulación.</param>
        [CallOnThread("Updater")]
        internal virtual void Update(RenderFrame frame, DateTime now)
        {
            // Por defecto, cada actualización intenta iniciar una carga.
            if (Game.LoaderProcess.Finished)
                Game.LoaderProcess.StartLoad();
        }

        /// <summary>
        /// Invocada para cargar nuevos contenidos y cuando sea necesaria.
        /// </summary>
        [CallOnThread("Loader")]
        internal virtual void Load()
        {
        }

        /// <summary>
        /// Libera todos los recursos que usó la instancia actual de la clase <see cref="GameState"/>.
        /// </summary>
        [CallOnThread("Loader")]
        internal virtual void Dispose()
        {
        }
    }
}
