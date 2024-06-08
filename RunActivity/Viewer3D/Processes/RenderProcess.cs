using Tourmaline.Processes;
using TOURMALINE.Common;
using System;
using System.Diagnostics;

using System.Windows.Forms;
//using static TOURMALINE.Settings.UserSettings;
using Tourmaline.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Tourmaline.Viewer3D.Processes
{
    [CallOnThread("Render")]
    public class RenderProcess
    {
        public const int ShadowMapCountMaximum = 4;

        public Point DisplaySize { get; private set; }
        public GraphicsDevice GraphicsDevice { get => Game.GraphicsDevice; }
        public bool IsActive { get => Game.IsActive; }

        public Viewer Viewer { get { return Game.State is GameStateSFM3D ? (Game.State as GameStateSFM3D).mvarVisor : null; } }

        public Profiler profiler { get; private set; }

        readonly Game Game;
        readonly Form GameForm;
        readonly Point GameWindowSize;
        readonly WatchdogToken watchdogToken;

        public GraphicsDeviceManager GraphicsDeviceManager { get;private set; }
        RenderFrame CurrentFrame; //Frame que contiene una lista de primitivas que dibujar en un momento determinado
        RenderFrame NextFrame; //Frame que vamos preparando en segundo plano mientras el actual se representa.

        public bool IsMouseVisible { get; set; } //Gestiona los fallos de multitarea al avisar al proceso de render de un cambio
        public Cursor ActualCursor = Cursors.Default;

        //Información de diagnóstico:
        public SmoothedData FrameRate { get; private set; } 
        public SmoothedDataWithPercentiles FrameTime { get; private set; }
        public int[] PrimitiveCount { get; private set; }
        public int[] PrimitivePerFrame { get; private set; }
        public int[] ShadowPrimitiveCount { get; private set; }
        public int[] ShadowPrimitivePerFrame { get; private set; }

        //Ajuste del mapa de sombras
        public static int ShadowMapCount = -1; // número de mapas de sombras
        public static int[] ShadowMapDistance; // Distancia del centro del mapa de sombras de la cámara
        public static int[] ShadowMapDiameter; // diámetro del mapa de sombras
        public static float[] ShadowMapLimit; // diámetro del lado más alejado del mapa de sombras desde la cámara

        internal RenderProcess(Game game)
        {
            Game=game;
            GameForm = (Form)Control.FromHandle(Game.Window.Handle);

            watchdogToken = new WatchdogToken(System.Threading.Thread.CurrentThread);

            profiler = new Profiler("Render");
            profiler.SetThread();

            //Game.SetThreadLanguage();

            Game.Window.Title = "Tourmaline";
            GraphicsDeviceManager = new GraphicsDeviceManager(game);

            //var windowsSizeParts = Game.Settings.WindowSize.Split(new[] { 'x' }, 2);
            //GameWindowSize = new Point(Convert.ToInt32(windowsSizeParts[0]), Convert.ToInt32(windowsSizeParts[1]));
            GameWindowSize = new Point(1024, 768);

            FrameRate = new SmoothedData();
            FrameTime = new SmoothedDataWithPercentiles();
            PrimitiveCount = new int[(int)RenderPrimitiveSequence.Sentinel];
            PrimitivePerFrame = new int[(int)RenderPrimitiveSequence.Sentinel];

            //Ejecuta el juego a 10FPS mientras muestra la pantalla de carga.
            //NO LO CAMBIES: Afecta a la velocidad de carga.
            Game.IsFixedTimeStep = true;
            Game.TargetElapsedTime = TimeSpan.FromMilliseconds(100);
            Game.InactiveSleepTime=TimeSpan.FromMilliseconds(100);

            //Asigna el resto de los gráficos de acuerdo a los ajustes.
            //GraphicsDeviceManager.SynchronizeWithVerticalRetrace = Game.Settings.VerticalSync;
            GraphicsDeviceManager.SynchronizeWithVerticalRetrace = true;
            GraphicsDeviceManager.PreferredBackBufferFormat = SurfaceFormat.Color;
            GraphicsDeviceManager.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            GraphicsDeviceManager.IsFullScreen = game.FullScreen;
            //GraphicsDeviceManager.PreferMultiSampling = (AntiAliasingMethod)Game.Settings.AntiAliasing != AntiAliasingMethod.None;
            GraphicsDeviceManager.PreferMultiSampling = true;
            //GraphicsDeviceManager.HardwareModeSwitch = !Game.Settings.FastFullScreenAltTab;
            GraphicsDeviceManager.HardwareModeSwitch = true;
            GraphicsDeviceManager.PreparingDeviceSettings += new EventHandler<PreparingDeviceSettingsEventArgs>(GDM_PreparingDeviceSettings);
        }

        void GDM_PreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            // Permite que PerfHud de NVIDIA pueda ejecutar en este programa.
            foreach (var adapter in GraphicsAdapter.Adapters)
            {
                if (adapter.Description.Contains("PerfHUD"))
                {
                    e.GraphicsDeviceInformation.Adapter = adapter;
                    GraphicsAdapter.UseReferenceDevice = true;
                    break;
                }
            }

            e.GraphicsDeviceInformation.GraphicsProfile = e.GraphicsDeviceInformation.Adapter.IsProfileSupported(GraphicsProfile.HiDef) ? GraphicsProfile.HiDef : GraphicsProfile.Reach;

            var pp = e.GraphicsDeviceInformation.PresentationParameters;
            //switch ((AntiAliasingMethod)Game.Settings.AntiAliasing)
            //{
            //    case AntiAliasingMethod.None:
            //    default:
            //        break;
            //    case AntiAliasingMethod.MSAA2x:
            //        pp.MultiSampleCount = 2;
            //        break;
            //    case AntiAliasingMethod.MSAA4x:
            //        pp.MultiSampleCount = 4;
            //        break;
            //    case AntiAliasingMethod.MSAA8x:
            //        pp.MultiSampleCount = 8;
            //        break;
            //    case AntiAliasingMethod.MSAA16x:
            //        pp.MultiSampleCount = 16;
            //        break;
            //    case AntiAliasingMethod.MSAA32x:
            //        pp.MultiSampleCount = 32;
            //        break;
            //}
            pp.MultiSampleCount = 32;
            if (pp.IsFullScreen)
            {
                var screen = Screen.FromControl(GameForm);
                pp.BackBufferWidth = screen.Bounds.Width;
                pp.BackBufferHeight = screen.Bounds.Height;
            }
            else
            {
                pp.BackBufferWidth = GameWindowSize.X;
                pp.BackBufferHeight = GameWindowSize.Y;
            }
        }

        internal void Start()
        {
            Game.WatchdogProcess.Register(watchdogToken);

            DisplaySize = new Point(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            // Comprueba que el nivel de prestaciones de DirectX sea uno que podamos entender
            //if (!Enum.IsDefined(typeof(DirectXFeature), "Level" + Game.Settings.DirectXFeatureLevel))
            //    Game.Settings.DirectXFeatureLevel = "";

            //if (Game.Settings.DirectXFeatureLevel == "")
            //{
            //    // Escoge el nivel de prestaciones por defecto basado en el perfil.
            //    if (GraphicsDevice.GraphicsProfile == GraphicsProfile.HiDef)
            //        Game.Settings.DirectXFeatureLevel = "10_0";
            //    else
            //        Game.Settings.DirectXFeatureLevel = "9_1";
            //}

            //if (Game.Settings.ShadowMapDistance == 0)
            //    Game.Settings.ShadowMapDistance = Game.Settings.ViewingDistance / 2;

            //ShadowMapCount = Game.Settings.ShadowMapCount;
            //if (!Game.Settings.DynamicShadows)
            //    ShadowMapCount = 0;
            //else if ((ShadowMapCount > 1) && !Game.Settings.IsDirectXFeatureLevelIncluded(DirectXFeature.Level9_3))
            //    ShadowMapCount = 1;
            //else if (ShadowMapCount < 0)
            //    ShadowMapCount = 0;
            //else if (ShadowMapCount > ShadowMapCountMaximum)
            //    ShadowMapCount = ShadowMapCountMaximum;
            //if (ShadowMapCount < 1)
            //    Game.Settings.DynamicShadows = false;

            ShadowMapCount = ShadowMapCountMaximum; //Lo he extraído del grupo de ifs que tengo arriba.

            ShadowMapDistance = new int[ShadowMapCount];
            ShadowMapDiameter = new int[ShadowMapCount];
            ShadowMapLimit = new float[ShadowMapCount];

            ShadowPrimitiveCount = new int[ShadowMapCount];
            ShadowPrimitivePerFrame = new int[ShadowMapCount];

            InitializeShadowMapLocations();

            CurrentFrame = new RenderFrame(Game);
            NextFrame = new RenderFrame(Game);
        }

        void InitializeShadowMapLocations()
        {
            var ratio = (float)DisplaySize.X / DisplaySize.Y;
            //var fov = MathHelper.ToRadians(Game.Settings.ViewingFOV);
            var fov = MathHelper.ToRadians(100);
            var n = (float)0.5;
            //var f = (float)Game.Settings.ShadowMapDistance;
            var f = 100.0f;
            if (f == 0)
                f = Game.ViewingDistance / 2;

            var m = (float)ShadowMapCount;
            var LastC = n;
            for (var shadowMapIndex = 0; shadowMapIndex < ShadowMapCount; shadowMapIndex++)
            {
                //     Clog  = split distance i using logarithmic splitting
                //         i
                // Cuniform  = split distance i using uniform splitting
                //         i
                //         n = near view plane
                //         f = far view plane
                //         m = number of splits
                //
                //                   i/m
                //     Clog  = n(f/n)
                //         i
                // Cuniform  = n+(f-n)i/m
                //         i

                // Calcula las dos Cs y las promedia para obtener un buen balance.
                var i = (float)(shadowMapIndex + 1);
                var Clog = n * (float)Math.Pow(f / n, i / m);
                var Cuniform = n + (f - n) * i / m;
                var C = (3 * Clog + Cuniform) / 4;

                // Este mapa de sombras va desde LastC a C; calcula el centro y diámetro correctos para la esfera desde el frustum de la vista.
                var height1 = (float)Math.Tan(fov / 2) * LastC;
                var height2 = (float)Math.Tan(fov / 2) * C;
                var width1 = height1 * ratio;
                var width2 = height2 * ratio;
                var corner1 = new Vector3(height1, width1, LastC);
                var corner2 = new Vector3(height2, width2, C);
                var cornerCenter = (corner1 + corner2) / 2;
                var length = cornerCenter.Length();
                cornerCenter.Normalize();
                var center = length / Vector3.Dot(cornerCenter, Vector3.UnitZ);
                var diameter = 2 * (float)Math.Sqrt(height2 * height2 + width2 * width2 + (C - center) * (C - center));

                ShadowMapDistance[shadowMapIndex] = (int)center;
                ShadowMapDiameter[shadowMapIndex] = (int)diameter;
                ShadowMapLimit[shadowMapIndex] = C;
                LastC = C;
            }
        }

        internal void Update(GameTime gameTime)
        {
            if (IsMouseVisible != Game.IsMouseVisible)
                Game.IsMouseVisible = IsMouseVisible;

            Cursor.Current = ActualCursor;

            if (ToggleFullScreenRequested)
            {
                GraphicsDeviceManager.ToggleFullScreen();
                ToggleFullScreenRequested = false;
                Viewer.DefaultViewport = GraphicsDevice.Viewport;
            }

            if (gameTime.TotalGameTime.TotalSeconds > 0.001)
            {
                Game.UpdaterProcess.WaitTillFinished();

                // La entrada del usuario tiene que ser en el hilo del juego XNA
                UserInput.Update(Game);

                // Intercambia frames e inicia la siguiente actualización.
                // El updater en monoproceso hace la actualización completa.
                SwapFrames(ref CurrentFrame, ref NextFrame);
                Game.UpdaterProcess.StartUpdate(NextFrame, gameTime.TotalGameTime);
            }
        }

        internal void BeginDraw()
        {
            if (Game.State == null)
                return;

            profiler.Start();
            watchdogToken.Ping();

            //Ñapa para permitir que Perfhud de NVIDIA se muestre correctamente.
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            CurrentFrame.IsScreenChanged = (DisplaySize.X != GraphicsDevice.Viewport.Width) || (DisplaySize.Y != GraphicsDevice.Viewport.Height);
            if (CurrentFrame.IsScreenChanged)
            {
                DisplaySize = new Point(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                InitializeShadowMapLocations();
            }

            Game.State.BeginRender(CurrentFrame);
        }

        [ThreadName("Render")]
        internal void Draw()
        {
            if (Debugger.IsAttached)
            {
                CurrentFrame.Draw(Game.GraphicsDevice);
            }
            else
            {
                try
                {
                    CurrentFrame.Draw(Game.GraphicsDevice);
                }
                catch (Exception error)
                {
                    Game.ProcessReportError(error);
                }
            }
        }

        internal void EndDraw()
        {
            if (Game.State == null)
                return;

            Game.State.EndRender(CurrentFrame);

            for (var i = 0; i < (int)RenderPrimitiveSequence.Sentinel; i++)
            {
                PrimitivePerFrame[i] = PrimitiveCount[i];
                PrimitiveCount[i] = 0;
            }
            for (var shadowMapIndex = 0; shadowMapIndex < ShadowMapCount; shadowMapIndex++)
            {
                ShadowPrimitivePerFrame[shadowMapIndex] = ShadowPrimitiveCount[shadowMapIndex];
                ShadowPrimitiveCount[shadowMapIndex] = 0;
            }

            //Ñapa para permitir que Perfhud de NVIDIA se muestre correctamente.
            GraphicsDevice.DepthStencilState = DepthStencilState.None;

            profiler.Stop();
        }

        internal void Stop()
        {
            Game.WatchdogProcess.Unregister(watchdogToken);
        }

        //Intercambiamos los frames para pintar en uno mientras representamos el otro.
        static void SwapFrames(ref RenderFrame frame1, ref RenderFrame frame2)
        {
            RenderFrame temp = frame1;
            frame1 = frame2;
            frame2 = temp;
        }

        bool ToggleFullScreenRequested;
        [CallOnThread("Updater")]
        public void ToggleFullScreen()
        {
            ToggleFullScreenRequested = true;
        }

        [CallOnThread("Render")]
        [CallOnThread("Updater")]
        public void ComputeFPS(float elapsedRealTime)
        {
            if (elapsedRealTime < 0.001)
                return;

            FrameRate.Update(elapsedRealTime, 1f / elapsedRealTime);
            FrameTime.Update(elapsedRealTime, elapsedRealTime);
        }
    }
}
