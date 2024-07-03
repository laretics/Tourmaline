using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tourmaline.Viewer3D.TvForms
{
    using static System.Net.Mime.MediaTypeNames;
    // System.Drawing y el framework XNA definen los tipos Color y Rectangle
    // Para evitar conflictos se especifica cuál se usa. 
    using Color = System.Drawing.Color;
    using Rectangle = Microsoft.Xna.Framework.Rectangle;

    /// <summary>
    /// Control de usuario que usa un GraphicsDevice de XNA Framework para
    /// ser representado en un Windows Form.
    /// Las clases derivadas pueden sobreescribir los métodos Initialize y
    /// Draw para añadir su código de dibujo.
    /// </summary>
    abstract public class GraphicsDeviceControl : Control
    {
        // No importa cuántas instancias de este control existan. Todas ellas
        // comparten el mismo GraphicsDevice, gestionado por este servicio.
        GraphicsDeviceService graphicsDeviceService;

        SwapChainRenderTarget _renderTarget;

        /// <summary>
        /// Obtiene un GraphicsDevice que usaremos para pintar en el control.
        /// </summary>
        public GraphicsDevice GraphicsDevice { get =>graphicsDeviceService.GraphicsDevice; }

        //Lugar de destino del render.
        public RenderTarget2D DefaultRenderTarget { get => _renderTarget;  }

        /// <summary>
        /// Gets an IServiceProvider containing our IGraphicsDeviceService.
        /// This can be used with components such as the ContentManager,
        /// which use this service to look up the GraphicsDevice.
        /// </summary>
        public ServiceContainer Services { get => services; }

        ServiceContainer services = new ServiceContainer();

        internal _3dTrainViewer mvarViewer; //Visualizador de tren propio de este control.

        public GraphicsDeviceControl() : base()
        {
            mvarViewer = new _3dTrainViewer(this);
        }

        /// <summary>
        /// Inicia el control.
        /// </summary>
        protected override void OnCreateControl()
        {
            // No se iniciará el dispositivo gráfico si se muestra en el diseñador.
            if (!DesignMode)
            {
                graphicsDeviceService = GraphicsDeviceService.AddRef(Handle,
                                                                     ClientSize.Width,
                                                                     ClientSize.Height);

                int w = Math.Max(ClientSize.Width, 1);
                int h = Math.Max(ClientSize.Height, 1);
                _renderTarget = new SwapChainRenderTarget(GraphicsDevice, Handle, w, h);

                // SetKeyboardInput(true);

                // Registra el servicio para que lo encuentren otros componentes como ContentManager.
                //services.AddService<IGraphicsDeviceService>(graphicsDeviceService);

                // Hace que las clases derivadas llamen a sus respectivas rutinas de inicio.
                Initialize();

                //Permite que el ratón XNS use esta ventana
                //Mouse.WindowHandle = Handle;

                return;
            }

            base.OnCreateControl();
        }

        public void SetKeyboardInput(bool enable)
        {
            var keyboardType = typeof(Microsoft.Xna.Framework.Input.Keyboard);
            var methodInfo = keyboardType.GetMethod("SetActive", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            methodInfo.Invoke(null, new object[] { enable });
        }


        /// <summary>
        /// Desecha el control.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (graphicsDeviceService != null)
            {
                graphicsDeviceService.Release(disposing);
                graphicsDeviceService = null;
            }

            if (_renderTarget != null)
            {
                _renderTarget.Dispose();
                _renderTarget = null;
            }

            base.Dispose(disposing);
        }


        /// <summary>
        /// Repinta el control en respuesta a un mensaje de redibujo de Windows Forms
        /// </summary>        
        protected override void OnPaint(PaintEventArgs e)
        {
            // Si no tentemos graphicsDevice es porque estamos en el diseñador.
            if (graphicsDeviceService == null)
            {
                PaintUsingSystemDrawing(e.Graphics, Text + "\n\n" + GetType());
                return;
            }

            try
            {
                //Llama para que pinte
                BeginDraw();
                // Dibuja el control usando el GraphicsDevice.
                OnDraw(EventArgs.Empty);
                EndDraw();
            }
            catch (Exception ex)
            {
                PaintUsingSystemDrawing(e.Graphics, ex.Message + "\n\n" + ex.StackTrace);
            }
            return;
        }


        /// <summary>
        /// Intenta comenzar a pintar el control. Devuelve un mensaje de error si no
        /// fue posible. Puede ocurrir si se pierde el graphics device o si estamos
        /// en el diseñador de Forms.
        /// </summary>
        internal void BeginDraw()
        {
            // Nos aseguramos de que el dispositivo gráfico es suficientemente grande y no se ha perdido.
            HandleDeviceReset();

            GraphicsDevice.SetRenderTarget(_renderTarget);

            // Podemos tener muchas instancias de GraphicsDeviceControl que pueden
            // estar compartiendo el mismo GraphicsDevice. El backbuffer se 
            // redimensionará para coincidir con el más grande de todos. Pero ¿qué
            // pasa si pintamos en un control más pequeño?
            // Para evitar redimensionamientos extraños se asigna el viewport para
            // que use sólo la porción superior izquierda del backbuffer.
            if (this.Viewport.Width == 0 || this.Viewport.Height == 0)
                throw new Exception("Viewport size cannot be Zero.");
            GraphicsDevice.Viewport = this.Viewport;

            return;
        }

        public Viewport Viewport
        {
            get
            {
                int w = Math.Max(ClientSize.Width, 1);
                int h = Math.Max(ClientSize.Height, 1);
                return new Viewport(0, 0, w, h);
            }
        }


        /// <summary>
        /// Termina de pintar el control. Las clases derivadas llaman a este
        /// método cuando han terminado su propio método Draw y son responsables
        /// de presentar la imagen terminada en la pantalla usando el control
        /// Winforms apropiado para asegurarse de que lo muestra en el lugar
        /// correcto.
        /// </summary>
        internal void EndDraw()
        {
            try
            {
                Rectangle sourceRectangle = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);

                GraphicsDevice.SetRenderTarget(null);
                _renderTarget.Present();
            }
            catch
            {
                // Entrará por aquí si el dispositivo se pierde mientras 
                // estamos dibujando. En ese caso no pasa nada porque recuperaremos
                // el control en el siguiente "BeginDraw".
                // Por eso no se hará nada.
            }
        }


        /// <summary>
        /// BeginDraw usa este helper. Asegurará el estado del dispositivo
        /// gráfico confirmando que es suficientemente grande para mostrar el
        /// contgrol actual y que no hemos perdido el dispositivo.
        /// Devuelve un error si no se pudo reiniciar el dispositivo.
        /// </summary>
        void HandleDeviceReset()
        {
            bool deviceNeedsReset = false;

            switch (GraphicsDevice.GraphicsDeviceStatus)
            {
                case GraphicsDeviceStatus.Lost:
                    // Hemos perdido el dispositivo gráfico. No se puede usar.
                    throw new Exception("Graphics device lost");

                case GraphicsDeviceStatus.NotReset:
                    // Si el dispositivo está en un estado no reiniciable deberíamos inentar reiniciarlo.
                    deviceNeedsReset = true;
                    break;

                default:
                    // Una vez que el estado del dispositivo está bien, comprobaremos
                    // si tiene el tamaño suficiente.
                    int w = Math.Max(ClientSize.Width, 1);
                    int h = Math.Max(ClientSize.Height, 1);
                    deviceNeedsReset = (w != _renderTarget.Width) ||
                                       (h != _renderTarget.Height);
                    break;
            }

            // ¿Hace falta reiniciar el dispositivo?
            if (deviceNeedsReset)
            {
                try
                {
                    int w = Math.Max(ClientSize.Width, 1);
                    int h = Math.Max(ClientSize.Height, 1);
                    graphicsDeviceService.ResetDevice(w, h);

                    //recreamos el swapchain de ventanas.
                    _renderTarget.Dispose();
                    _renderTarget = new SwapChainRenderTarget(GraphicsDevice, Handle, w, h);
                }
                catch (Exception e)
                {
                    throw new Exception("Graphics device reset failed\n\n", e);
                }
            }

            return;
        }

        protected void ResetSwapChainRenderTarget()
        {
            if (_renderTarget != null)
                _renderTarget.Dispose();
            _renderTarget = new SwapChainRenderTarget(GraphicsDevice, Handle, ClientSize.Width, ClientSize.Height);
        }


        /// <summary>
        /// Si no tenemos un dispositivo gráfico válido (por ejemplo, si se ha
        /// perdido el dispositivo o se ejecuta en el diseñador), tenemos que
        /// usar el método System.Drawing para mostrar un mensaje de estado.
        /// </summary>
        protected virtual void PaintUsingSystemDrawing(Graphics graphics, string text)
        {
            graphics.Clear(Color.Black);

            using (Brush brush = new SolidBrush(Color.White))
            {
                using (StringFormat format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Near;
                    format.LineAlignment = StringAlignment.Near;

                    graphics.DrawString(text, Font, brush, ClientRectangle, format);
                }
            }
        }

        /// <summary>
        /// Ignoramos los mensajes de redibujar en background. La implementación
        /// por defecto podría borrar el control con el color de fondo actual, 
        /// haciendo que la imagen parpadee cuando nuestra implementación de
        /// OnPaint dibuje inmediatamente sobre otro color usando el dispositivo
        /// gráfico XNA.
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
        }

        /// <summary>
        /// Las clases derivadas deben sobreescribir esto para iniciar su
        /// código de dibujo.
        /// </summary>
        abstract protected void Initialize();

        public event EventHandler Draw;

        /// <summary>
        /// Las clases derivadas deben sobreescribir esto para dibujarse usando el
        /// GraphicsDevice.
        /// </summary>
        protected virtual void OnDraw(EventArgs e)
        {
            EventHandler handler = Draw;
            if (handler != null)
                handler(this, e);
        }

    }
}
