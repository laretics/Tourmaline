using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using TOURMALINE.Settings;
using TOURMALINE.Common;
using Tourmaline.Common;
using System.Configuration;
using System.Windows.Forms;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Tourmaline.Viewer3D.Processes
{
    [CallOnThread("Render")]
    public class Game: Microsoft.Xna.Framework.Game
    {
        public static Game Instance { get; set; }

        /// Ajustes de usuario
        private string mvarContentPath = "C:\\Tourmaline";
        private string mvarContentPath2 = "A:\\Tourmaline\\Tourmaline";

        public float PerformanceTunerTarget = 60; //Objetivo 60FPS
        public bool PerformanceTuner = true; //Forzamos recálculo dinámico de FPS.
        public bool VerticalSync = false; //Sincronía vertical.
        public float ViewingDistance = 2000.0f; //Distancia máxima de visión
        public float ViewingFOV = 45.0f;
        public bool LODViewingExtention = true; //Extensión de LOD a la máxima distancia de visualización
        public float LODBias = 0; //Nivel de detalle
        public bool FullScreen = false; //Juego a pantalla completa        

        /// Carpeta con los contenidos del juego
        public string ContentPath { get; private set; }
        /// Carpeta con los recursos del programa principal
        public string ResourcesPath { get; private set; }
        ///Acceso al proceso Watchdog del juego
        public WatchdogProcess WatchdogProcess { get; private set; }
        ///Acceso al proceso de render del juego
        public RenderProcess RenderProcess { get; private set; }
        ///Acceso al actualizador del juego
        public UpdaterProcess UpdaterProcess { get; private set; }
        ///Acceso al proceso que carga los componentes
        public LoaderProcess LoaderProcess { get; private set; }

        Stack<GameState> States;
        public GameState State { get => States.Count > 0 ? States.Peek() : null; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Game"/> based on the specified <see cref="UserSettings"/>.
        /// </summary>
        /// <param name="settings">The <see cref="UserSettings"/> for the game to use.</param>
        //public Game(UserSettings settings)
        public Game()
        {
            locateContentPath();            
            Exiting += new System.EventHandler<System.EventArgs>(Game_Exiting);
            WatchdogProcess = new WatchdogProcess(this);
            RenderProcess = new RenderProcess(this);
            UpdaterProcess = new UpdaterProcess(this);
            LoaderProcess = new LoaderProcess(this);
            //WebServerProcess = new WebServerProcess(this);
            States = new Stack<GameState>();
            Instance = this;
        }

        private void locateContentPath()
        {
            if (System.IO.Directory.Exists(mvarContentPath))
                ContentPath = mvarContentPath;
            else
                ContentPath = mvarContentPath2;

            ResourcesPath = Path.Combine(ContentPath, "Resources");
        }

        [ThreadName("Render")]
        protected override void BeginRun()
        {
            // En este punto, GraphicsDevice está iniciada y configurada.
            LoaderProcess.Start();
            UpdaterProcess.Start();
            RenderProcess.Start();
            WatchdogProcess.Start();
            base.BeginRun();
        }

        [ThreadName("Render")]
        protected override void Update(Microsoft.Xna.Framework.GameTime gameTime)
        {
            // La primera llamada a Update() ocurre antes de que se muestre la ventana, cuando gameTime ==0.
            // La segunda ya ocurre con la ventana en pantalla.
            if (State == null)
                Exit();
            else
                RenderProcess.Update(gameTime);
            base.Update(gameTime);
        }

        [ThreadName("Render")]
        protected override bool BeginDraw()
        {
            if (!base.BeginDraw())
                return false;
            RenderProcess.BeginDraw();
            return true;
        }

        [ThreadName("Render")]
        protected override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            RenderProcess.Draw();
            base.Draw(gameTime);
        }

        [ThreadName("Render")]
        protected override void EndDraw()
        {
            RenderProcess.EndDraw();
            base.EndDraw();
        }

        [ThreadName("Render")]
        protected override void EndRun()
        {
            base.EndRun();
            WatchdogProcess.Stop();
            RenderProcess.Stop();
            UpdaterProcess.Stop();
            LoaderProcess.Stop();
        }

        [ThreadName("Render")]
        void Game_Exiting(object sender, EventArgs e)
        {
            while (State != null)
                PopState();
        }

        [CallOnThread("Loader")]
        internal void PushState(GameState state)
        {
            state.Game = this;
            States.Push(state);
            Trace.TraceInformation("Game.PushState({0})  {1}", state.GetType().Name, String.Join(" | ", States.Select(s => s.GetType().Name).ToArray()));
        }

        [CallOnThread("Loader")]
        internal void PopState()
        {
            State.Dispose();
            States.Pop();
            Trace.TraceInformation("Game.PopState()  {0}", String.Join(" | ", States.Select(s => s.GetType().Name).ToArray()));
        }

        [CallOnThread("Loader")]
        internal void ReplaceState(GameState state)
        {
            if (State != null)
            {
                State.Dispose();
                States.Pop();
            }
            state.Game = this;
            States.Push(state);
            Trace.TraceInformation("Game.ReplaceState({0})  {1}", state.GetType().Name, String.Join(" | ", States.Select(s => s.GetType().Name).ToArray()));
        }

        /// <summary>
        /// Actualiza la llamada al hilo <see cref="Thread.CurrentUICulture"/> para cumplir con los ajustes del <see cref="Game"/>
        /// </summary>
        [CallOnThread("Render")]
        [CallOnThread("Updater")]
        [CallOnThread("Loader")]
        [CallOnThread("Watchdog")]
        public void SetThreadLanguage()
        {
            //if (Settings.Language.Length > 0)
            //{
            //    try
            //    {
            //        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(Settings.Language);
            //    }
            //    catch (CultureNotFoundException) { }
            //}
        }

        /// <summary>
        /// Informa cualquier <see cref="Exception"/> al archivo de log y/o al usuario, saliendo del juego en el proceso.
        /// </summary>
        /// <param name="error">The <see cref="Exception"/> to report.</param>
        [CallOnThread("Render")]
        [CallOnThread("Updater")]
        [CallOnThread("Loader")]
        [CallOnThread("Sound")]
        public void ProcessReportError(Exception error)
        {
            // Apagamos el watchdog ya que estamos saliendo del programa.
            WatchdogProcess.Stop();
            // Reportamos el error antes que nada, por si el cuelgue es gordo.
            Trace.WriteLine(new FatalException(error));
            // Paramos el mundo!
            Exit();
            // Informamos al usuario de que ha ocurrido algo horriblemente malo.
            //if (Settings.ShowErrorDialogs)
                System.Windows.Forms.MessageBox.Show(error.ToString(), Application.ProductName + " " + VersionInfo.VersionOrBuild);
        }

    }
}
