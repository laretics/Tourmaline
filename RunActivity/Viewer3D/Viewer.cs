using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Tourmaline.Common;
using Tourmaline.Formats.Msts;
using Tourmaline.Simulation;
using Tourmaline.Simulation.RollingStocks;
using Tourmaline.Viewer3D.Popups;
using Tourmaline.Viewer3D.Processes;
using TOURMALINE.Common;
using TOURMALINE.Common.Input;
using Event = Tourmaline.Common.Event;

namespace Tourmaline.Viewer3D
{
    public class Viewer
    {
        // Procesos concurrentes.
        public LoaderProcess LoaderProcess { get; private set; }
        public UpdaterProcess UpdaterProcess { get; private set; }
        public RenderProcess RenderProcess { get; private set; }
        // Acceso a la clase de juego de XNA
        public GraphicsDevice GraphicsDevice { get; private set; }
        public string ContentPath { get; private set; }
        public string ResourcesPath { get; private set; }
        public SharedTextureManager TextureManager { get; private set; }
        public SharedMaterialManager MaterialManager { get; private set; }
        public SharedShapeManager ShapeManager { get; private set; }

        public Point DisplaySize { get { return RenderProcess.DisplaySize; } }
        // Componentes
        public Tourmaline.Viewer3D.Processes.Game Game { get; private set; }
        public long runningGameTime { get; set; } = 0;
        public MicroSim microSim { get; private set; }
        public World World { get; private set; }
        public float Elevation { get; set; }

        /// <summary>
        /// Valor de tiempo que se incrementa constantemente (en segundos) para el visor del juego.
        /// Empieza valiendo cero y no dejará de incrementar en tiempo real.
        /// </summary>
        public MessageWindow messageWindow { get; private set; } //Ventana de mensajes. Especial. Siempre visible.
        public WindowManager WindowManager { get; private set; }
        public TrackTypesFile TrackTypes { get; private set; }
        public HUDWindow HUDWindow { get; private set; } // F5 hud

        // Cámaras
        public Camera Camera { get; set; } // Cámara actual
        public TrackingCamera FrontCamera { get; private set; } // Camera 2
        public TrackingCamera BackCamera { get; private set; } // Camera 3
        //public Camera AbovegroundCamera { get; private set; } // Cámara previa a la que saltaremos automáticamente tras la vista de cabina.
        //public TracksideCamera TracksideCamera { get; private set; } // Camera 4
        //public SpecialTracksideCamera SpecialTracksideCamera { get; private set; } // Camera 4 for special points (platforms and level crossings)
        //public List<FreeRoamCamera> FreeRoamCameraList = new List<FreeRoamCamera>();
        //public FreeRoamCamera FreeRoamCamera { get { return FreeRoamCameraList[0]; } } // Camera 8

        List<Camera> WellKnownCameras; // Providing Camera save functionality by GeorgeS

        MouseState originalMouseState;      // Current mouse coordinates.

        // This is the train we are controlling
        public TrainCar PlayerLocomotive { get { return microSim.PlayerLocomotive; } set { microSim.PlayerLocomotive = value; } }

        // This is the train we are viewing

        void CameraActivate()
        {
            if(null!=Camera)
                Camera.Activate();
        }

        bool ForceMouseVisible;
        long MouseVisibleTillRealTime;
        public Cursor ActualCursor = Cursors.Default;
        public static Viewport DefaultViewport;

        public bool SaveScreenshot { get; set; }
        public bool SaveActivityThumbnail { get; private set; }
        public string SaveActivityFileStem { get; private set; }

        public Vector3 NearPoint { get; private set; }
        public Vector3 FarPoint { get; private set; }

        public bool DebugViewerEnabled { get; set; }

        enum VisibilityState
        {
            Visible,
            Hidden,
            ScreenshotPending,
        };

        VisibilityState Visibility = VisibilityState.Visible;

        public CommandLog Log { get { return microSim.Log; } }

        public long LoadMemoryThreshold; // Above this threshold loader doesn't bulk load day or night textures
        public bool tryLoadingNightTextures = false;
        public bool tryLoadingDayTextures = false;

        public int poscounter = 1; // counter for print position info

        public Camera SuspendedCamera { get; private set; }

        public static double DbfEvalAutoPilotTimeS = 0;//Debrief eval
        public static double DbfEvalIniAutoPilotTimeS = 0;//Debrief eval  
        public bool DbfEvalAutoPilot = false;//DebriefEval

        /// <summary>
        /// Initializes a new instances of the <see cref="Viewer3D"/> class based on the specified <paramref name="simulator"/> and <paramref name="game"/>.
        /// </summary>
        /// <param name="simulator">The <see cref="Simulator"/> with which the viewer runs.</param>
        /// <param name="game">The <see cref="Game"/> with which the viewer runs.</param>
        [CallOnThread("Loader")]
        public Viewer(MicroSim microSim, Tourmaline.Viewer3D.Processes.Game game)
        {
            //Random = new Random();
            this.microSim = microSim;
            Game = game;
            //Settings = microSim.Settings;
            //Use3DCabProperty = Settings.GetSavingProperty<bool>("Use3DCab");

            RenderProcess = game.RenderProcess;
            UpdaterProcess = game.UpdaterProcess;
            LoaderProcess = game.LoaderProcess;

            FrontCamera = new TrackingCamera(this, TrackingCamera.AttachedTo.Front);
            BackCamera = new TrackingCamera(this, TrackingCamera.AttachedTo.Rear);
            //WellKnownCameras = new List<Camera>();
            //WellKnownCameras.Add(TracksideCamera = new TracksideCamera(this));
            //WellKnownCameras.Add(SpecialTracksideCamera = new SpecialTracksideCamera(this));

            //Camera = WellKnownCameras[0];
            Camera = FrontCamera;

            ContentPath = Game.ContentPath;
            ResourcesPath = game.ResourcesPath;

            Trace.Write(" TTYPE");
            //TrackTypes = new TrackTypesFile(microSim.RoutePath + @"\TTYPE.DAT");

            Initialize();
        }

        [CallOnThread("Updater")]
        public void Save(BinaryWriter outf, string fileStem)
        {
            WindowManager.Save(outf);

            //outf.Write(WellKnownCameras.IndexOf(Camera));
            //foreach (var camera in WellKnownCameras)
            //    camera.Save(outf);
            //Camera.Save(outf);
            //outf.Write(CabYOffsetPixels);
            //outf.Write(CabXOffsetPixels);

            // Set these so RenderFrame can use them when its thread gets control.
            SaveActivityFileStem = fileStem;
            SaveActivityThumbnail = true;
            //outf.Write(NightTexturesNotLoaded);
            //outf.Write(DayTexturesNotLoaded);
            //World.WeatherControl.SaveWeatherParameters(outf);
        }

        /// <summary>
        /// Called once after the graphics device is ready
        /// to load any static graphics content, background
        /// processes haven't started yet.
        /// </summary>
        [CallOnThread("Loader")]
        internal void Initialize()
        {
            GraphicsDevice = RenderProcess.GraphicsDevice;
            UpdateAdapterInformation(GraphicsDevice.Adapter);
            DefaultViewport = GraphicsDevice.Viewport;
            
            TextureManager = new SharedTextureManager(this, GraphicsDevice);            

            MaterialManager = new SharedMaterialManager(this);
            ShapeManager = new SharedShapeManager(this);

            WindowManager = new WindowManager(this);

            ///Aquí están todas las ventanas flotantes de OpenRails.
            ///Mi nuevo sistema estará basado en otras ventanas diferentes, pero basadas
            ///en la misma arquitectura.
            ///Por eso voy a comentar este código, que luego iré rescatando como pueda.

            HUDWindow = new HUDWindow(WindowManager);
            messageWindow = new MessageWindow(WindowManager);
            WindowManager.Initialize();

            World = new World(this);
            CameraActivate();

            // Prepara el mundo para que se pueda cargar en el hilo correcto para depuración o traza.
            //Esto asegura que a) tenemos los objetos requeridos cargados cuando se inicia la vista 3D.
            // y b) que toda la tarea de carga se ejecuta en un solo hilo, que se puede depurar.
            World.LoadPrep();
            Load();

            // MUST be after loading is done! (Or we try and load shapes on the main thread.)
            //PlayerLocomotiveViewer = World.Trains.GetViewer(PlayerLocomotive);

            SetCommandReceivers();
            //InitReplay();
            HUDWindow.Visible = false;
        }

        /// <summary>
        /// Each Command needs to know its Receiver so it can call a method of the Receiver to action the command.
        /// The Receiver is a static property as all commands of the same class share the same Receiver
        /// and it needs to be set before the command is used.
        /// </summary>
        public void SetCommandReceivers()
        {
            /*
            ReverserCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            NotchedThrottleCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            ContinuousThrottleCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            TrainBrakeCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            EngineBrakeCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            BrakemanBrakeCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            DynamicBrakeCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            InitializeBrakesCommand.Receiver = PlayerLocomotive.Train;
            ResetOutOfControlModeCommand.Receiver = PlayerLocomotive.Train;
            EmergencyPushButtonCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            HandbrakeCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            BailOffCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            QuickReleaseCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            BrakeOverchargeCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            RetainersCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            BrakeHoseConnectCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            ToggleWaterScoopCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            if (PlayerLocomotive is MSTSSteamLocomotive)
            {
                ContinuousReverserCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                ContinuousInjectorCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                ContinuousSmallEjectorCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                ContinuousLargeEjectorCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                ToggleInjectorCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                ToggleBlowdownValveCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                ContinuousBlowerCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                ContinuousDamperCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                ContinuousFiringRateCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                ToggleManualFiringCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                ToggleCylinderCocksCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                ToggleCylinderCompoundCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                FireShovelfullCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                AIFireOnCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                AIFireOffCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
                AIFireResetCommand.Receiver = (MSTSSteamLocomotive)PlayerLocomotive;
            }

            ImmediateRefillCommand.Receiver = (MSTSLocomotiveViewer)PlayerLocomotiveViewer;
            RefillCommand.Receiver = (MSTSLocomotiveViewer)PlayerLocomotiveViewer;
            ToggleOdometerCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            ResetOdometerCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            ToggleOdometerDirectionCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            SanderCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            AlerterCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            HornCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            BellCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            ToggleCabLightCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            WipersCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            HeadlightCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            ChangeCabCommand.Receiver = this;
            ToggleDoorsLeftCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            ToggleDoorsRightCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            ToggleMirrorsCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            CabRadioCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            ToggleSwitchAheadCommand.Receiver = this;
            ToggleSwitchBehindCommand.Receiver = this;
            ToggleAnySwitchCommand.Receiver = this;
            UncoupleCommand.Receiver = this;
            SaveScreenshotCommand.Receiver = this;
            ActivityCommand.Receiver = ActivityWindow;  // and therefore shared by all sub-classes
            UseCameraCommand.Receiver = this;            
            ToggleHelpersEngineCommand.Receiver = (MSTSLocomotive)PlayerLocomotive;
            BatterySwitchCommand.Receiver = (PlayerLocomotive as MSTSLocomotive).LocomotivePowerSupply;
            BatterySwitchCloseButtonCommand.Receiver = (PlayerLocomotive as MSTSLocomotive).LocomotivePowerSupply;
            BatterySwitchOpenButtonCommand.Receiver = (PlayerLocomotive as MSTSLocomotive).LocomotivePowerSupply;
            ToggleMasterKeyCommand.Receiver = (PlayerLocomotive as MSTSLocomotive).LocomotivePowerSupply;
            ServiceRetentionButtonCommand.Receiver = (PlayerLocomotive as MSTSLocomotive).LocomotivePowerSupply;
            ServiceRetentionCancellationButtonCommand.Receiver = (PlayerLocomotive as MSTSLocomotive).LocomotivePowerSupply;
            ElectricTrainSupplyCommand.Receiver = (PlayerLocomotive as MSTSLocomotive).LocomotivePowerSupply;
            TCSButtonCommand.Receiver = (PlayerLocomotive as MSTSLocomotive).TrainControlSystem;
            TCSSwitchCommand.Receiver = (PlayerLocomotive as MSTSLocomotive).TrainControlSystem;
            */
            MoveCameraCommand.Receiver = this;
        }

        /*
        public void ChangeToPreviousFreeRoamCamera()
        {
            if (Camera == FreeRoamCamera)
            {
                // If 8 is the current camera, rotate the list and then activate a different camera.
                RotateFreeRoamCameraList();
                FreeRoamCamera.Activate();
            }
            else
            {
                FreeRoamCamera.Activate();
                RotateFreeRoamCameraList();
            }
        }
        */

        /*
        void RotateFreeRoamCameraList()
        {
            // Rotate list moving 1 to 0 etc. (by adding 0 to end, then removing 0)
            FreeRoamCameraList.Add(FreeRoamCamera);
            FreeRoamCameraList.RemoveAt(0);
        }
        */


        string adapterDescription;
        public string AdapterDescription { get { return adapterDescription; } }

        uint adapterMemory;
        public uint AdapterMemory { get { return adapterMemory; } }

        [CallOnThread("Updater")]
        internal void UpdateAdapterInformation(GraphicsAdapter graphicsAdapter)
        {
            adapterDescription = graphicsAdapter.Description;
            try
            {
                // Note that we might find multiple adapters with the same
                // description; however, the chance of such adapters not having
                // the same amount of video memory is very slim.
                foreach (ManagementObject videoController in new ManagementClass("Win32_VideoController").GetInstances())
                    if (((string)videoController["Description"] == adapterDescription) && (videoController["AdapterRAM"] != null))
                        adapterMemory = (uint)videoController["AdapterRAM"];
            }
            catch (ManagementException error)
            {
                Trace.WriteLine(error);
            }
            catch (UnauthorizedAccessException error)
            {
                Trace.WriteLine(error);
            }
        }

        [CallOnThread("Loader")]
        public void Load()
        {
            World.Load();
            WindowManager.Load();
        }

        [CallOnThread("Updater")]
        public void Update(RenderFrame frame, long elapsedTime)
            //Esta es la rutina en la que pintamos sobre el RenderFrame todos los
            //objetos destinados a ser representados en el programa.
            //ElapsedTime es el tiempo en milisegundos desde la última actualización.
        {
            runningGameTime += elapsedTime; //Esto es el tiempo que lleva el simulador en marcha

            HandleUserInput(elapsedTime); //Entrada del usuario (todavía inactiva)
            microSim.Update(elapsedTime); //Actualizamos la simulación (físicas)               
            World.Update(elapsedTime); //Actualizamos el material a pintar (escenario y trenes)

            if (frame.IsScreenChanged)
                Camera.ScreenChanged();

            //En ORTS la cámara no suele estar quieta, sino en movimiento.
            //En este método estamos cambiando su ubicación, el objetivo al que apunta y posiblemente el zoom.
            Camera.Update(elapsedTime);

            frame.PrepareFrame(this); //Iniciamos el fotograma indicando que este es el visor
            Camera.PrepareFrame(frame, elapsedTime); //Iniciamos la representación en la cámara
            frame.PrepareFrame(elapsedTime); //Actualizamos las cosas que se mueven (cielo, meteo y fondo).
            World.PrepareFrame(frame, elapsedTime); //Imprimimos los objetos del mundo (render).
            WindowManager.PrepareFrame(frame, elapsedTime); //Actualizamos las ventanitas flotantes.
        }


        [CallOnThread("Updater")]
        void HandleUserInput(long elapsedTime)
        {
            
            //var train = Program.Viewer.PlayerLocomotive.Train;//DebriefEval

            if (UserInput.IsMouseLeftButtonDown) // || (Camera is ThreeDimCabCamera && RenderProcess.IsMouseVisible))
            {
                Vector3 nearsource = new Vector3((float)UserInput.MouseX, (float)UserInput.MouseY, 0f);
                Vector3 farsource = new Vector3((float)UserInput.MouseX, (float)UserInput.MouseY, 1f);
                Matrix world = Matrix.CreateTranslation(0, 0, 0);
                NearPoint = DefaultViewport.Unproject(nearsource, Camera.XnaProjection, Camera.XnaView, world);
                FarPoint = DefaultViewport.Unproject(farsource, Camera.XnaProjection, Camera.XnaView, world);
            }

            if (UserInput.IsPressed(UserCommand.CameraReset))
                Camera.Reset();

            Camera.HandleUserInput(elapsedTime);

            //if (PlayerLocomotiveViewer != null)
            //    PlayerLocomotiveViewer.HandleUserInput(elapsedTime);

            //InfoDisplay.HandleUserInput(elapsedTime);
            //WindowManager.HandleUserInput(elapsedTime);

            // Check for game control keys
            //if (MPManager.IsMultiPlayer() && UserInput.IsPressed(UserCommand.GameMultiPlayerTexting))
            //{
            //    if (ComposeMessageWindow == null) ComposeMessageWindow = new ComposeMessage(WindowManager);
            //    ComposeMessageWindow.InitMessage();
            //}
            //if ((MPManager.IsMultiPlayer() || (Settings.MultiplayerClient && MPManager.Simulator.Confirmer != null)) && UserInput.IsPressed(UserCommand.DisplayMultiPlayerWindow)) { MultiPlayerWindow.Visible = !MultiPlayerWindow.Visible; }
            //if (!MPManager.IsMultiPlayer() && UserInput.IsPressed(UserCommand.GamePauseMenu)) { QuitWindow.Visible = Simulator.Paused = !QuitWindow.Visible; }
            //if (MPManager.IsMultiPlayer() && UserInput.IsPressed(UserCommand.GamePauseMenu)) { if (Simulator.Confirmer != null) Simulator.Confirmer.Information(Viewer.Catalog.GetString("In MP, use Alt-F4 to quit directly")); }

            if (UserInput.IsPressed(UserCommand.GameFullscreen)) { RenderProcess.ToggleFullScreen(); }
            //if (!MPManager.IsMultiPlayer() && UserInput.IsPressed(UserCommand.GamePause)) Simulator.Paused = !Simulator.Paused;
            //if (!MPManager.IsMultiPlayer() && UserInput.IsPressed(UserCommand.DebugSpeedUp))
            //{
            //    Simulator.GameSpeed *= 1.5f;
            //    Simulator.Confirmer.ConfirmWithPerCent(CabControl.SimulationSpeed, CabSetting.Increase, Simulator.GameSpeed * 100);
            //}
            //if (!MPManager.IsMultiPlayer() && UserInput.IsPressed(UserCommand.DebugSpeedDown))
            //{
            //    Simulator.GameSpeed /= 1.5f;
            //    Simulator.Confirmer.ConfirmWithPerCent(CabControl.SimulationSpeed, CabSetting.Decrease, Simulator.GameSpeed * 100);
            //}
            //if (UserInput.IsPressed(UserCommand.DebugSpeedReset))
            //{
            //    Simulator.GameSpeed = 1;
            //    Simulator.Confirmer.ConfirmWithPerCent(CabControl.SimulationSpeed, CabSetting.Off, Simulator.GameSpeed * 100);
            //}
            //if (UserInput.IsPressed(UserCommand.GameSave)) { GameStateRunActivity.Save(); }
            //if (UserInput.IsPressed(UserCommand.DisplayHelpWindow)) if (UserInput.IsDown(UserCommand.DisplayNextWindowTab)) HelpWindow.TabAction(); else HelpWindow.Visible = !HelpWindow.Visible;
            //if (UserInput.IsPressed(UserCommand.DisplayTrackMonitorWindow)) if (UserInput.IsDown(UserCommand.DisplayNextWindowTab)) TrackMonitorWindow.TabAction(); else TrackMonitorWindow.Visible = !TrackMonitorWindow.Visible;
            //if (UserInput.IsPressed(UserCommand.DisplayTrainDrivingWindow)) if (UserInput.IsDown(UserCommand.DisplayNextWindowTab)) TrainDrivingWindow.TabAction(); else TrainDrivingWindow.Visible = !TrainDrivingWindow.Visible;
            //if (UserInput.IsPressed(UserCommand.DisplayHUD)) if (UserInput.IsDown(UserCommand.DisplayNextWindowTab)) HUDWindow.TabAction(); else HUDWindow.Visible = !HUDWindow.Visible;
            //if (UserInput.IsPressed(UserCommand.DisplayStationLabels))
            //{
            //    if (UserInput.IsDown(UserCommand.DisplayNextWindowTab)) OSDLocations.TabAction(); else OSDLocations.Visible = !OSDLocations.Visible;
            //    if (OSDLocations.Visible)
            //    {
            //        switch (OSDLocations.CurrentDisplayState)
            //        {
            //            case OSDLocations.DisplayState.Auto:
            //                MessagesWindow.AddMessage(Catalog.GetString("Automatic platform and siding labels visible."), 5);
            //                break;
            //            case OSDLocations.DisplayState.All:
            //                MessagesWindow.AddMessage(Catalog.GetString("Platform and siding labels visible."), 5);
            //                break;
            //            case OSDLocations.DisplayState.Platforms:
            //                MessagesWindow.AddMessage(Catalog.GetString("Platform labels visible."), 5);
            //                break;
            //            case OSDLocations.DisplayState.Sidings:
            //                MessagesWindow.AddMessage(Catalog.GetString("Siding labels visible."), 5);
            //                break;
            //        }
            //    }
            //    else
            //    {
            //        MessagesWindow.AddMessage(Catalog.GetString("Platform and siding labels hidden."), 5);
            //    }
            //}
            //if (UserInput.IsPressed(UserCommand.DisplayCarLabels))
            //{
            //    if (UserInput.IsDown(UserCommand.DisplayNextWindowTab)) OSDCars.TabAction(); else OSDCars.Visible = !OSDCars.Visible;
            //    if (OSDCars.Visible)
            //    {
            //        switch (OSDCars.CurrentDisplayState)
            //        {
            //            case OSDCars.DisplayState.Trains:
            //                MessagesWindow.AddMessage(Catalog.GetString("Train labels visible."), 5);
            //                break;
            //            case OSDCars.DisplayState.Cars:
            //                MessagesWindow.AddMessage(Catalog.GetString("Car labels visible."), 5);
            //                break;
            //        }
            //    }
            //    else
            //    {
            //        MessagesWindow.AddMessage(Catalog.GetString("Train and car labels hidden."), 5);
            //    }
            //}
            //if (UserInput.IsPressed(UserCommand.DisplaySwitchWindow)) if (UserInput.IsDown(UserCommand.DisplayNextWindowTab)) SwitchWindow.TabAction(); else SwitchWindow.Visible = !SwitchWindow.Visible;
            //if (UserInput.IsPressed(UserCommand.DisplayTrainOperationsWindow)) if (UserInput.IsDown(UserCommand.DisplayNextWindowTab)) TrainOperationsWindow.TabAction(); else { TrainOperationsWindow.Visible = !TrainOperationsWindow.Visible; if (!TrainOperationsWindow.Visible) CarOperationsWindow.Visible = false; }
            //if (UserInput.IsPressed(UserCommand.DisplayNextStationWindow)) if (UserInput.IsDown(UserCommand.DisplayNextWindowTab)) NextStationWindow.TabAction(); else NextStationWindow.Visible = !NextStationWindow.Visible;
            //if (UserInput.IsPressed(UserCommand.DisplayCompassWindow)) if (UserInput.IsDown(UserCommand.DisplayNextWindowTab)) CompassWindow.TabAction(); else CompassWindow.Visible = !CompassWindow.Visible;
            //if (UserInput.IsPressed(UserCommand.DebugTracks)) if (UserInput.IsDown(UserCommand.DisplayNextWindowTab)) TracksDebugWindow.TabAction(); else TracksDebugWindow.Visible = !TracksDebugWindow.Visible;
            //if (UserInput.IsPressed(UserCommand.DebugSignalling)) if (UserInput.IsDown(UserCommand.DisplayNextWindowTab)) SignallingDebugWindow.TabAction(); else SignallingDebugWindow.Visible = !SignallingDebugWindow.Visible;
            //if (UserInput.IsPressed(UserCommand.DisplayTrainListWindow)) TrainListWindow.Visible = !TrainListWindow.Visible;


            //if (UserInput.IsPressed(UserCommand.GameChangeCab))
            //{
            //    if (PlayerLocomotive.ThrottlePercent >= 1
            //        || Math.Abs(PlayerLocomotive.SpeedMpS) > 1
            //        || !IsReverserInNeutral(PlayerLocomotive))
            //    {
            //        Simulator.Confirmer.Warning(CabControl.ChangeCab, CabSetting.Warn2);
            //    }
            //    else
            //    {
            //        new ChangeCabCommand(Log);
            //    }
            //}

            //if (UserInput.IsPressed(UserCommand.CameraCab))
            //{
            //    if (CabCamera.IsAvailable || ThreeDimCabCamera.IsAvailable)
            //    {
            //        new UseCabCameraCommand(Log);
            //    }
            //    else
            //    {
            //        Simulator.Confirmer.Warning(Viewer.Catalog.GetString("Cab view not available"));
            //    }
            //}
            //if (UserInput.IsPressed(UserCommand.CameraToggleThreeDimensionalCab))
            //{
            //    if (!CabCamera.IsAvailable)
            //    {
            //        Simulator.Confirmer.Warning(Viewer.Catalog.GetString("This car doesn't have a 2D cab"));
            //    }
            //    else if (!ThreeDimCabCamera.IsAvailable)
            //    {
            //        Simulator.Confirmer.Warning(Viewer.Catalog.GetString("This car doesn't have a 3D cab"));
            //    }
            //    else
            //    {
            //        new ToggleThreeDimensionalCabCameraCommand(Log);
            //    }
            //}
            if (UserInput.IsPressed(UserCommand.CameraOutsideFront))
            {
                CheckReplaying();
                new UseFrontCameraCommand(Log);
            }
            if (UserInput.IsPressed(UserCommand.CameraOutsideRear))
            {
                CheckReplaying();
                new UseBackCameraCommand(Log);
            }
            //if (UserInput.IsPressed(UserCommand.CameraJumpingTrains)) RandomSelectTrain(); //hit Alt-9 key, random selected train to have 2 and 3 camera attached to

            if (UserInput.IsPressed(UserCommand.CameraVibrate))
            {
                //Program.Simulator.CarVibrating = (Program.Simulator.CarVibrating + 1) % 4;
                //Simulator.Confirmer.Message(ConfirmLevel.Information, Catalog.GetStringFmt("Vibrating at level {0}", Program.Simulator.CarVibrating));
                //Settings.CarVibratingLevel = Program.Simulator.CarVibrating;
                //Settings.Save("CarVibratingLevel");
            }

            if (UserInput.IsPressed(UserCommand.DebugToggleConfirmations))
            {
                //Simulator.Settings.SuppressConfirmations = !Simulator.Settings.SuppressConfirmations;
                //if (Simulator.Settings.SuppressConfirmations)
                //    Simulator.Confirmer.Message(ConfirmLevel.Warning, Catalog.GetString("Confirmations suppressed"));
                //else
                //    Simulator.Confirmer.Message(ConfirmLevel.Warning, Catalog.GetString("Confirmations visible"));
                //Settings.SuppressConfirmations = Simulator.Settings.SuppressConfirmations;
                //Settings.Save();
            }

            //hit 9 key, get back to player train
            if (UserInput.IsPressed(UserCommand.CameraJumpBackPlayer))
            {
                //SelectedTrain = PlayerTrain;
                //CameraActivate();
            }
            if (UserInput.IsPressed(UserCommand.CameraTrackside))
            {
                //CheckReplaying();
                //new UseTracksideCameraCommand(Log);
            }
            if (UserInput.IsPressed(UserCommand.CameraSpecialTracksidePoint))
            {
                //CheckReplaying();
                //new UseSpecialTracksideCameraCommand(Log);
            }
            // Could add warning if PassengerCamera not available.
            //if (UserInput.IsPressed(UserCommand.CameraPassenger) && PassengerCamera.IsAvailable)
            //{
                //CheckReplaying();
                //new UsePassengerCameraCommand(Log);
            //}
            if (UserInput.IsPressed(UserCommand.CameraBrakeman))
            {
                //CheckReplaying();
                //new UseBrakemanCameraCommand(Log);
            }
            if (UserInput.IsPressed(UserCommand.CameraFree))
            {
                //CheckReplaying();
                //new UseFreeRoamCameraCommand(Log);
                //Simulator.Confirmer.Message(ConfirmLevel.None, Catalog.GetPluralStringFmt(
                //    "{0} viewpoint stored. Use Shift+8 to restore viewpoints.", "{0} viewpoints stored. Use Shift+8 to restore viewpoints.", FreeRoamCameraList.Count - 1));
            }
            if (UserInput.IsPressed(UserCommand.CameraPreviousFree))
            {
                //if (FreeRoamCameraList.Count > 0)
                //{
                //    CheckReplaying();
                //    new UsePreviousFreeRoamCameraCommand(Log);
                //}
            }
            //if (UserInput.IsPressed(UserCommand.CameraHeadOutForward) && HeadOutForwardCamera.IsAvailable)
            //{
                //CheckReplaying();
                //new UseHeadOutForwardCameraCommand(Log);
            //}
            //if (UserInput.IsPressed(UserCommand.CameraHeadOutBackward) && HeadOutBackCamera.IsAvailable)
            //{
                //CheckReplaying();
                //new UseHeadOutBackCameraCommand(Log);
            //}
            if (UserInput.IsPressed(UserCommand.GameSwitchAhead))
            {
                //if (PlayerTrain.ControlMode == Train.TRAIN_CONTROL.MANUAL || PlayerTrain.ControlMode == Train.TRAIN_CONTROL.EXPLORER)
                //    new ToggleSwitchAheadCommand(Log);
                //else
                //    Simulator.Confirmer.Warning(CabControl.SwitchAhead, CabSetting.Warn1);
            }
            if (UserInput.IsPressed(UserCommand.GameSwitchBehind))
            {
                //if (PlayerTrain.ControlMode == Train.TRAIN_CONTROL.MANUAL || PlayerTrain.ControlMode == Train.TRAIN_CONTROL.EXPLORER)
                //    new ToggleSwitchBehindCommand(Log);
                //else
                //    Simulator.Confirmer.Warning(CabControl.SwitchBehind, CabSetting.Warn1);
            }
            //if (UserInput.IsPressed(UserCommand.GameClearSignalForward)) PlayerTrain.RequestSignalPermission(Direction.Forward);
            //if (UserInput.IsPressed(UserCommand.GameClearSignalBackward)) PlayerTrain.RequestSignalPermission(Direction.Reverse);
            //if (UserInput.IsPressed(UserCommand.GameResetSignalForward)) PlayerTrain.RequestResetSignal(Direction.Forward);
            //if (UserInput.IsPressed(UserCommand.GameResetSignalBackward)) PlayerTrain.RequestResetSignal(Direction.Reverse);

            //if (UserInput.IsPressed(UserCommand.GameSwitchManualMode)) PlayerTrain.RequestToggleManualMode();
            //if (UserInput.IsPressed(UserCommand.GameResetOutOfControlMode)) new ResetOutOfControlModeCommand(Log);

            //if (UserInput.IsPressed(UserCommand.GameMultiPlayerDispatcher)) { DebugViewerEnabled = !DebugViewerEnabled; return; }
            //if (UserInput.IsPressed(UserCommand.DebugSoundForm)) { SoundDebugFormEnabled = !SoundDebugFormEnabled; return; }

            if (UserInput.IsPressed(UserCommand.CameraJumpSeeSwitch))
            {
            //    if (Program.DebugViewer != null && Program.DebugViewer.Enabled && (Program.DebugViewer.switchPickedItem != null || Program.DebugViewer.signalPickedItem != null))
            //    {
            //        WorldLocation wos;
            //        TrJunctionNode nextSwitchTrack = Program.DebugViewer.switchPickedItem?.Item?.TrJunctionNode;
            //        if (nextSwitchTrack != null)
            //        {
            //            wos = new WorldLocation(nextSwitchTrack.TN.UiD.TileX, nextSwitchTrack.TN.UiD.TileZ, nextSwitchTrack.TN.UiD.X, nextSwitchTrack.TN.UiD.Y + 8, nextSwitchTrack.TN.UiD.Z);
            //        }
            //        else
            //        {
            //            var s = Program.DebugViewer.signalPickedItem.Item;
            //            wos = new WorldLocation(s.TileX, s.TileZ, s.X, s.Y + 8, s.Z);
            //        }
            //        if (FreeRoamCameraList.Count == 0)
            //        {
            //            new UseFreeRoamCameraCommand(Log);
            //        }
            //        FreeRoamCamera.SetLocation(wos);
            //        //FreeRoamCamera
            //        FreeRoamCamera.Activate();
            //    }                
            }

            // Turntable commands
            //if (Simulator.MovingTables != null)
            //{
            //    if (UserInput.IsPressed(UserCommand.ControlTurntableClockwise))
            //    {
            //        Simulator.ActiveMovingTable = FindActiveMovingTable();
            //        if (Simulator.ActiveMovingTable != null)
            //        {
            //            TurntableClockwiseCommand.Receiver = Simulator.ActiveMovingTable;
            //            new TurntableClockwiseCommand(Log);
            //        }
            //    }
            //    else if (UserInput.IsReleased(UserCommand.ControlTurntableClockwise) && Simulator.ActiveMovingTable != null)
            //    {
            //        TurntableClockwiseTargetCommand.Receiver = Simulator.ActiveMovingTable;
            //        new TurntableClockwiseTargetCommand(Log);
            //    }

            //    if (UserInput.IsPressed(UserCommand.ControlTurntableCounterclockwise))
            //    {
            //        Simulator.ActiveMovingTable = FindActiveMovingTable();
            //        if (Simulator.ActiveMovingTable != null)
            //        {
            //            TurntableCounterclockwiseCommand.Receiver = Simulator.ActiveMovingTable;
            //            new TurntableCounterclockwiseCommand(Log);
            //        }
            //    }

            //    else if (UserInput.IsReleased(UserCommand.ControlTurntableCounterclockwise) && Simulator.ActiveMovingTable != null)
            //    {
            //        TurntableCounterclockwiseTargetCommand.Receiver = Simulator.ActiveMovingTable;
            //        new TurntableCounterclockwiseTargetCommand(Log);
            //    }
            //}

            //if (UserInput.IsPressed(UserCommand.GameAutopilotMode))
            //{
            //    if (PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING)
            //    {
            //        var success = ((AITrain)PlayerLocomotive.Train).SwitchToPlayerControl();
            //        if (success)
            //        {
            //            Simulator.Confirmer.Message(ConfirmLevel.Information, Viewer.Catalog.GetString("Switched to player control"));
            //            DbfEvalAutoPilot = false;//Debrief eval
            //        }
            //    }
            //    else if (PlayerLocomotive.Train.TrainType == Train.TRAINTYPE.AI_PLAYERDRIVEN)
            //    {
            //        if (PlayerLocomotive.Train.ControlMode == Train.TRAIN_CONTROL.MANUAL)
            //            Simulator.Confirmer.Message(ConfirmLevel.Warning, Viewer.Catalog.GetString("You can't switch from manual to autopilot mode"));
            //        else
            //        {
            //            var success = ((AITrain)PlayerLocomotive.Train).SwitchToAutopilotControl();
            //            if (success)
            //            {
            //                Simulator.Confirmer.Message(ConfirmLevel.Information, Viewer.Catalog.GetString("Switched to autopilot"));
            //                DbfEvalIniAutoPilotTimeS = Simulator.ClockTime;//Debrief eval
            //                DbfEvalAutoPilot = true;//Debrief eval
            //            }
            //        }
            //    }
            //}

            //if (DbfEvalAutoPilot && (Simulator.ClockTime - DbfEvalIniAutoPilotTimeS) > 1.0000)
            //{
            //    DbfEvalAutoPilotTimeS = DbfEvalAutoPilotTimeS + (Simulator.ClockTime - DbfEvalIniAutoPilotTimeS);//Debrief eval
            //    train.DbfEvalValueChanged = true;
            //    DbfEvalIniAutoPilotTimeS = Simulator.ClockTime;//Debrief eval
            //}
            //if (UserInput.IsPressed(UserCommand.DebugDumpKeymap))
            //{
            //    var textPath = Path.Combine(Settings.LoggingPath, "OpenRailsKeyboard.txt");
            //    Settings.Input.DumpToText(textPath);
            //    MessagesWindow.AddMessage(Catalog.GetStringFmt("Keyboard map list saved to '{0}'.", textPath), 10);

            //    var graphicPath = Path.Combine(Settings.LoggingPath, "OpenRailsKeyboard.png");
            //    Settings.Input.DumpToGraphic(graphicPath);
            //    MessagesWindow.AddMessage(Catalog.GetStringFmt("Keyboard map image saved to '{0}'.", graphicPath), 10);
            //}

            



            /*
            //in the dispatcher window, when one clicks a train and "See in Game", will jump to see that train
            if (Program.DebugViewer != null && Program.DebugViewer.ClickedTrain == true)
            {
                Program.DebugViewer.ClickedTrain = false;
                if (SelectedTrain != Program.DebugViewer.PickedTrain)
                {
                    SelectedTrain = Program.DebugViewer.PickedTrain;
                    Simulator.AI.aiListChanged = true;

                    if (SelectedTrain.Cars == null || SelectedTrain.Cars.Count == 0) SelectedTrain = PlayerTrain;

                    CameraActivate();
                }
            }

            //in TrainSwitcher, when one clicks a train, Viewer will jump to see that train
            if (Simulator.TrainSwitcher.ClickedTrainFromList == true)
            {
                Simulator.TrainSwitcher.ClickedTrainFromList = false;
                if (SelectedTrain != Simulator.TrainSwitcher.PickedTrainFromList && SelectedTrain.Cars != null || SelectedTrain.Cars.Count != 0)
                {
                    SelectedTrain = Simulator.TrainSwitcher.PickedTrainFromList;
                    Simulator.AI.aiListChanged = true;

                    CameraActivate();
                }
            }

            if (!Simulator.Paused && UserInput.IsDown(UserCommand.GameSwitchWithMouse))
            {
                ForceMouseVisible = true;
                if (UserInput.IsMouseLeftButtonPressed)
                {
                    TryThrowSwitchAt();
                    UserInput.Handled();
                }
            }
            else if (!Simulator.Paused && UserInput.IsDown(UserCommand.GameUncoupleWithMouse))
            {
                ForceMouseVisible = true;
                if (UserInput.IsMouseLeftButtonPressed)
                {
                    TryUncoupleAt();
                    UserInput.Handled();
                }
            }
            else
            {
                ForceMouseVisible = false;
            }

            // reset cursor type when needed

            if (!(Camera is CabCamera) && !(Camera is ThreeDimCabCamera) && ActualCursor != Cursors.Default) ActualCursor = Cursors.Default;

            // Mouse control for 2D cab

            if (Camera is CabCamera && (PlayerLocomotiveViewer as MSTSLocomotiveViewer)._hasCabRenderer)
            {
                if (UserInput.IsMouseLeftButtonPressed)
                {
                    foreach (var controlRenderer in (PlayerLocomotiveViewer as MSTSLocomotiveViewer)._CabRenderer.ControlMap.Values)
                    {
                        if (controlRenderer is ICabViewMouseControlRenderer mouseRenderer && mouseRenderer.IsMouseWithin())
                        {
                            MouseChangingControl = mouseRenderer;
                            break;
                        }
                    }
                }

                if (MouseChangingControl != null)
                {
                    MouseChangingControl.HandleUserInput();
                    if (UserInput.IsMouseLeftButtonReleased)
                    {
                        MouseChangingControl = null;
                        UserInput.Handled();
                    }
                }
            }

            // explore 2D cabview controls

            if (Camera is CabCamera && (PlayerLocomotiveViewer as MSTSLocomotiveViewer)._hasCabRenderer && MouseChangingControl == null &&
                RenderProcess.IsMouseVisible)
            {
                if (!UserInput.IsMouseLeftButtonPressed)
                {
                    foreach (var controlRenderer in (PlayerLocomotiveViewer as MSTSLocomotiveViewer)._CabRenderer.ControlMap.Values)
                    {
                        if (controlRenderer is ICabViewMouseControlRenderer mouseRenderer && mouseRenderer.IsMouseWithin())
                        {
                            MousePickedControl = mouseRenderer;
                            break;
                        }
                    }
                    if (MousePickedControl != null & MousePickedControl != OldMousePickedControl)
                    {
                        // say what control you have here
                        Simulator.Confirmer.Message(ConfirmLevel.None, MousePickedControl.GetControlName());
                    }
                    if (MousePickedControl != null) ActualCursor = Cursors.Hand;
                    else if (ActualCursor == Cursors.Hand) ActualCursor = Cursors.Default;
                    OldMousePickedControl = MousePickedControl;
                    MousePickedControl = null;
                }
            }

            // mouse for 3D camera

            if (Camera is ThreeDimCabCamera && (PlayerLocomotiveViewer as MSTSLocomotiveViewer)._has3DCabRenderer)
            {
                if (UserInput.IsMouseLeftButtonPressed)
                {
                    var trainCarShape = (PlayerLocomotiveViewer as MSTSLocomotiveViewer).ThreeDimentionCabViewer.TrainCarShape;
                    var animatedParts = (PlayerLocomotiveViewer as MSTSLocomotiveViewer).ThreeDimentionCabViewer.AnimateParts;
                    var controlMap = (PlayerLocomotiveViewer as MSTSLocomotiveViewer).ThreeDimentionCabRenderer.ControlMap;
                    float bestD = 0.015f;  // 15 cm squared click range
                    CabViewControlRenderer cabRenderer;
                    foreach (var animatedPart in animatedParts)
                    {
                        var key = animatedPart.Value.Key;
                        try
                        {
                            cabRenderer = controlMap[key];
                        }
                        catch
                        {
                            continue;
                        }
                        if (cabRenderer is CabViewDiscreteRenderer)
                        {
                            foreach (var iMatrix in animatedPart.Value.MatrixIndexes)
                            {
                                var matrix = Matrix.Identity;
                                var hi = iMatrix;
                                while (hi >= 0 && hi < trainCarShape.Hierarchy.Length && trainCarShape.Hierarchy[hi] != -1)
                                {
                                    Matrix.Multiply(ref matrix, ref trainCarShape.XNAMatrices[hi], out matrix);
                                    hi = trainCarShape.Hierarchy[hi];
                                }
                                matrix = Matrix.Multiply(matrix, trainCarShape.Location.XNAMatrix);
                                var matrixWorldLocation = trainCarShape.Location.WorldLocation;
                                matrixWorldLocation.Location.X = matrix.Translation.X;
                                matrixWorldLocation.Location.Y = matrix.Translation.Y;
                                matrixWorldLocation.Location.Z = -matrix.Translation.Z;
                                Vector3 xnaCenter = Camera.XnaLocation(matrixWorldLocation);
                                float d = ORTSMath.LineSegmentDistanceSq(xnaCenter, NearPoint, FarPoint);
                                if (bestD > d)
                                {
                                    MouseChangingControl = cabRenderer as CabViewDiscreteRenderer;
                                    bestD = d;
                                }
                            }
                        }
                    }
                }

                if (MouseChangingControl != null)
                {
                    MouseChangingControl.HandleUserInput();
                    if (UserInput.IsMouseLeftButtonReleased)
                    {
                        MouseChangingControl = null;
                        UserInput.Handled();
                    }
                }
            }

            // explore 3D cabview controls

            if (Camera is ThreeDimCabCamera && (PlayerLocomotiveViewer as MSTSLocomotiveViewer)._has3DCabRenderer && MouseChangingControl == null &&
                RenderProcess.IsMouseVisible)
            {
                if (!UserInput.IsMouseLeftButtonPressed)
                {
                    var trainCarShape = (PlayerLocomotiveViewer as MSTSLocomotiveViewer).ThreeDimentionCabViewer.TrainCarShape;
                    var animatedParts = (PlayerLocomotiveViewer as MSTSLocomotiveViewer).ThreeDimentionCabViewer.AnimateParts;
                    var controlMap = (PlayerLocomotiveViewer as MSTSLocomotiveViewer).ThreeDimentionCabRenderer.ControlMap;
                    float bestD = 0.01f;  // 10 cm squared click range
                    CabViewControlRenderer cabRenderer;
                    foreach (var animatedPart in animatedParts)
                    {
                        var key = animatedPart.Value.Key;
                        try
                        {
                            cabRenderer = controlMap[key];
                        }
                        catch
                        {
                            continue;
                        }
                        if (cabRenderer is CabViewDiscreteRenderer)
                        {
                            foreach (var iMatrix in animatedPart.Value.MatrixIndexes)
                            {
                                var matrix = Matrix.Identity;
                                var hi = iMatrix;
                                while (hi >= 0 && hi < trainCarShape.Hierarchy.Length && trainCarShape.Hierarchy[hi] != -1)
                                {
                                    Matrix.Multiply(ref matrix, ref trainCarShape.XNAMatrices[hi], out matrix);
                                    hi = trainCarShape.Hierarchy[hi];
                                }
                                matrix = Matrix.Multiply(matrix, trainCarShape.Location.XNAMatrix);
                                var matrixWorldLocation = trainCarShape.Location.WorldLocation;
                                matrixWorldLocation.Location.X = matrix.Translation.X;
                                matrixWorldLocation.Location.Y = matrix.Translation.Y;
                                matrixWorldLocation.Location.Z = -matrix.Translation.Z;
                                Vector3 xnaCenter = Camera.XnaLocation(matrixWorldLocation);
                                float d = ORTSMath.LineSegmentDistanceSq(xnaCenter, NearPoint, FarPoint);

                                if (bestD > d)
                                {
                                    MousePickedControl = cabRenderer as CabViewDiscreteRenderer;
                                    bestD = d;
                                }
                            }
                        }
                    }
                    if (MousePickedControl != null & MousePickedControl != OldMousePickedControl)
                    {
                        // say what control you have here
                        Simulator.Confirmer.Message(ConfirmLevel.None, MousePickedControl.GetControlName());
                    }
                    if (MousePickedControl != null)
                    {
                        ActualCursor = Cursors.Hand;
                    }
                    else if (ActualCursor == Cursors.Hand)
                    {
                        ActualCursor = Cursors.Default;
                    }
                    OldMousePickedControl = MousePickedControl;
                    MousePickedControl = null;
                }
            }

            if (UserInput.RDState != null)
                UserInput.RDState.Handled();
            */

            MouseState currentMouseState = Mouse.GetState();

            if (currentMouseState.X != originalMouseState.X ||
                currentMouseState.Y != originalMouseState.Y)
                MouseVisibleTillRealTime = elapsedTime+1000;

            RenderProcess.IsMouseVisible = ForceMouseVisible || elapsedTime < MouseVisibleTillRealTime;
            originalMouseState = currentMouseState;
            RenderProcess.ActualCursor = ActualCursor;
        }


        /// <summary>
        /// Si el jugador cambia de cámara durante la repetición, todas las repeticiones de la cámara se suspenden.
        /// La cámara del jugador seguirá grabando comandos a pesar de la repetición.
        /// La repetición y la grabación de los comandos que no son de cámara como los controles continúa.
        /// </summary>
        public void CheckReplaying()
        {
            //if (microSim.IsReplaying)
            //{
            //    if (!Log.CameraReplaySuspended)
            //    {
            //        Log.CameraReplaySuspended = true;
            //        SuspendedCamera = Camera;
            //        microSim.Confirmer.Confirm(CabControl.Replay, CabSetting.Warn1);
            //    }
            //}
        }


        /// <summary>
        /// Replay of the camera is not resumed until the player opens the Quit Menu and then presses Esc to unpause the simulator.
        /// </summary>
        public void ResumeReplaying()
        {
            Log.CameraReplaySuspended = false;
            if (SuspendedCamera != null)
                SuspendedCamera.Activate();
        }
 
        [CallOnThread("Loader")]
        public void Mark()
        {
            WindowManager.Mark();
        }

        [CallOnThread("Render")]
        internal void Terminate()
        {
            //InfoDisplay.Terminate();

        }


        internal void BeginRender(RenderFrame frame)
        {
            if (frame.IsScreenChanged)
            {
                WindowManager.ScreenChanged();

            }

            MaterialManager.UpdateShaders();
        }

        internal void EndRender(RenderFrame frame)
        {
            // VisibilityState is used to delay calling SaveScreenshot() by one render cycle.
            // We want the hiding of the MessageWindow to take effect on the screen before the screen content is saved.
            if (Visibility == VisibilityState.Hidden)  // Test for Hidden state must come before setting Hidden state.
            {
                Visibility = VisibilityState.ScreenshotPending;  // Next state else this path would be taken more than once.
                //if (!Directory.Exists(Settings.ScreenshotPath))
                //    Directory.CreateDirectory(Settings.ScreenshotPath);
                //var fileName = Path.Combine(Settings.ScreenshotPath, System.Windows.Forms.Application.ProductName + " " + DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss")) + ".png";
                //SaveScreenshotToFile(Game.GraphicsDevice, fileName, false);
                SaveScreenshot = false; // cancel trigger
            }
            if (SaveScreenshot)
            {
                Visibility = VisibilityState.Hidden;
                // Hide MessageWindow
                //MessagesWindow.Visible = false;
                // Audible confirmation that screenshot taken
                //ViewerSounds.HandleEvent(Event.TakeScreenshot);
            }

            // SaveActivityThumbnail and FileStem set by Viewer3D
            // <CJComment> Intended to save a thumbnail-sized image but can't find a way to do this.
            // Currently saving a full screen image and then showing it in Menu.exe at a thumbnail size.
            // </CJComment>
            if (SaveActivityThumbnail)
            {
                SaveActivityThumbnail = false;
                //SaveScreenshotToFile(Game.GraphicsDevice, Path.Combine(UserSettings.UserDataFolder, SaveActivityFileStem + ".png"), true);
                SaveScreenshotToFile(Game.GraphicsDevice, Path.Combine(AppContext.BaseDirectory, SaveActivityFileStem + ".png"), true);
                //MessagesWindow.AddMessage(Catalog.GetString("Game saved"), 5);
            }
        }

        [CallOnThread("Render")]
        void SaveScreenshotToFile(GraphicsDevice graphicsDevice, string fileName, bool silent)
        {
            var width = graphicsDevice.PresentationParameters.BackBufferWidth;
            var height = graphicsDevice.PresentationParameters.BackBufferHeight;
            var data = new uint[width * height];

            graphicsDevice.GetBackBufferData(data);

            new Thread(() =>
            {
                try
                {
                    // Unfortunately, the back buffer includes an alpha channel. Although saving this might seem okay,
                    // it actually ruins the picture - nothing in the back buffer is seen on-screen according to its
                    // alpha, it's only used for blending (if at all). We'll remove the alpha here.
                    for (var i = 0; i < data.Length; i++)
                        data[i] |= 0xFF000000;

                    using (var screenshot = new Texture2D(graphicsDevice, width, height))
                    {
                        screenshot.SetData(data);

                        // Now save the modified image.
                        using (var stream = File.OpenWrite(fileName))
                        {
                            screenshot.SaveAsPng(stream, width, height);
                        }
                    }

                    if (!silent)
                        //MessagesWindow.AddMessage(String.Format("Saving screenshot to '{0}'.", fileName), 10);

                    Visibility = VisibilityState.Visible;
                    // Reveal MessageWindow
                    //MessagesWindow.Visible = true;
                }
                catch { }
            }).Start();
        }
    }
}
