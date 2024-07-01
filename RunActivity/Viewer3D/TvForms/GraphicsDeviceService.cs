using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tourmaline.Viewer3D.TvForms
{
    /// <summary>
    /// Clase responsable de crear y manejar el GraphicsDevice.
    /// Todas las instancias comparten el mismo dispositivo físico, de forma que
    /// por muchos controles que haya, todos trabajarán sobre el mismo GraphicsDevice.
    /// El servicio implementa la interface IGraphicsDeviceService, que proporciona
    /// eventos de notificación si el dispositivo se reinicia o se elimina.
    /// </summary>
    internal class GraphicsDeviceService : IGraphicsDeviceService
    {
        #region Fields


        // Instancia singleton del servicio.
        static GraphicsDeviceService singletonInstance;


        // Lleva la cuenta del número de controles que comparten la instancia singleton.
        static int referenceCount;


        #endregion


        /// <summary>
        /// El constructor es privado porque es una clase singleton:
        /// Los controles del cliente tienen que usar el método AddRef.
        /// </summary>
        GraphicsDeviceService(IntPtr windowHandle, int width, int height)
        {
            parameters = new PresentationParameters();

            parameters.BackBufferWidth = Math.Max(width, 1);
            parameters.BackBufferHeight = Math.Max(height, 1);
            parameters.BackBufferFormat = SurfaceFormat.Color;
            parameters.DepthStencilFormat = DepthFormat.Depth24Stencil8;
            parameters.DeviceWindowHandle = windowHandle;
            parameters.PresentationInterval = PresentInterval.Immediate;
            parameters.IsFullScreen = false;

            graphicsDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter,
#if DEFERRED
                                                GraphicsProfile.HiDef,
#else
                                                GraphicsProfile.Reach,
#endif                                          
                                                parameters);
            return;
        }

        /// <summary>
        /// Obtiene una referencia de la instancia singleton.
        /// </summary>
        public static GraphicsDeviceService AddRef(IntPtr windowHandle,
                                                   int width, int height)
        {
            // Incrementa el contador de referencias.
            if (Interlocked.Increment(ref referenceCount) == 1)
            {
                // Siendo el primero control que comienza a usar el
                // dispositivo, hay que crear la instancia singleton.
                singletonInstance = new GraphicsDeviceService(windowHandle,
                                                              width, height);
            }

            return singletonInstance;
        }


        /// <summary>
        /// Libera una referencia de la instancia singleton.
        /// </summary>
        public void Release(bool disposing)
        {
            // Decrementa el contador de referencias.
            if (Interlocked.Decrement(ref referenceCount) == 0)
            {
                // Si este es el último control en terminar de usar
                // el dipositivo, eliminaremos la instancia singleton.
                if (disposing)
                {
                    if (DeviceDisposing != null)
                        DeviceDisposing(this, EventArgs.Empty);

                    graphicsDevice.Dispose();
                }

                graphicsDevice = null;
            }
        }


        /// <summary>
        /// Reinicia el dispositivo gráfico si el tamaño o resolución actuales
        /// son mayores.
        /// De esta forma el dispositivo se redimensionará al tamaño del mayor de
        /// los clientes.
        /// </summary>
        public void ResetDevice(int width, int height)
        {
            if (DeviceResetting != null)
                DeviceResetting(this, EventArgs.Empty);

            parameters.BackBufferWidth = Math.Max(parameters.BackBufferWidth, width);
            parameters.BackBufferHeight = Math.Max(parameters.BackBufferHeight, height);

            graphicsDevice.Reset(parameters);

            if (DeviceReset != null)
                DeviceReset(this, EventArgs.Empty);
        }


        /// <summary>
        /// Obtiene el dispositivo gráfico actual.
        /// </summary>
        public GraphicsDevice GraphicsDevice
        {
            get { return graphicsDevice; }
        }

        GraphicsDevice graphicsDevice;


        // Almacena los ajustes actuales.
        PresentationParameters parameters;


        // Eventos IGraphicsService.
        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceDisposing;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceResetting;
    }
}
