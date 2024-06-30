using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tourmaline.Common;
using Tourmaline.Formats.Msts;
using Tourmaline.Simulation;
//using Tourmaline.Viewer3D.Debugging;
using TOURMALINE.Common;
//using TOURMALINE.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Runtime;
using System.Net.Http.Headers;
using Tourmaline.Maps;
using ACadSharp.IO;
using ACadSharp;

namespace Tourmaline.Viewer3D.Processes
{
    public class GameStateRunActivity : GameState
    {
        static string[] Arguments;
        static string CabinType; //Cabina desde donde se ejecuta laq simulación.
        static MicroSim MicroSim { get { return Program.MicroSim; } set { Program.MicroSim = value; } }

        static Viewer Viewer { get { return Program.Viewer; } set { Program.Viewer = value; } }
        static ORTraceListener ORTraceListener { get { return Program.ORTraceListener; } set { Program.ORTraceListener = value; } }
        static string logFileName { get { return Program.logFileName; } set { Program.logFileName = value; } }

        struct savedValues
        {
            public string pathName;
            public float initialTileX;
            public float initialTileZ;
            public string[] args;
            public string acttype;
        }

        LoadingPrimitive Loading;
        LoadingScreenPrimitive LoadingScreen;
        LoadingBarPrimitive LoadingBar;
        Matrix LoadingMatrix = Matrix.Identity;

        public GameStateRunActivity(string[] args)
        {
            Arguments = args;
        }

        internal override void Update(RenderFrame frame,DateTime elapsedTime)
        {
            //En este proceso especial pintamos a mano en el renderFrame durante la carga
            //no usamos el 3DViewer.
            UpdateLoading();

            if (Loading != null)
            {
                frame.AddPrimitive(Loading.Material, Loading, RenderPrimitiveGroup.Overlay, ref LoadingMatrix);
            }

            if (LoadingScreen != null)
            {
                frame.AddPrimitive(LoadingScreen.Material, LoadingScreen, RenderPrimitiveGroup.Overlay, ref LoadingMatrix);
            }

            if (LoadingBar != null)
            {
                LoadingBar.Material.Shader.LoadingPercent = LoadedPercent;
                frame.AddPrimitive(LoadingBar.Material, LoadingBar, RenderPrimitiveGroup.Overlay, ref LoadingMatrix);
            }

            base.Update(frame,elapsedTime);
        }

        internal override void Load()
            //Proceso de carga de recursos.
        {
            // Primero carga en memoria los gráficos de la pantalla de inicio. (Fase de carga de recursos)
            if (Loading == null)
                Loading = new LoadingPrimitive(Game);
            if (LoadingBar == null)
                LoadingBar = new LoadingBarPrimitive(Game);

            var args = Arguments;

            // Busca la acción a ejecutar.
            var action = "";
            var actions = new[] { "start", "debug","develop","test" };
            //start: Inicia el sistema de forma normal
            //debug: Inicia el sistema en modo de depuración
            //develop: Inicia el sistema en modo de mantenimiento (desarrollo de software)
            //test: Inicio de pruebas... sólo para comprobar que el proceso de carga funciona, sin más pretensiones
            foreach (var possibleAction in actions)
                if (args.Contains("-" + possibleAction) || args.Contains("/" + possibleAction, StringComparer.OrdinalIgnoreCase))
                {
                    action = possibleAction;
                }

            //Localiza la configuración de la cabina donde está este HMI
            var cabinType = "";
            var acttypes = new[] { "even", "odd", "none" };
            foreach (var possibleCabinType in acttypes)
                if (args.Contains("-" + possibleCabinType) || args.Contains("/" + possibleCabinType, StringComparer.OrdinalIgnoreCase))
                    cabinType = possibleCabinType;

            CabinType = cabinType;

            // Extrae todas las opciones de configuración (las que no implican un cambio de actividad)
            var options = args.Where(a => (a.StartsWith("-") || a.StartsWith("/")) && !actions.Contains(a.Substring(1)) && !cabinType.Contains(a.Substring(1))).Select(a => a.Substring(1)).ToArray();

            // Transforma todas estas opciones en datos.
            var data = args.Where(a => !a.StartsWith("-") && !a.StartsWith("/")).ToArray();

            //var settings = Game.Settings;

            Action doAction = () =>
            {
                InitLoading(args);
                Test(data);


                //// Do the action specified or write out some help.
                //switch (action)
                //{
                //    case "start":
                //    case "debug":
                //    case "develop":
                //        //InitLogging(settings, args);
                //        InitLoading(args);
                //        Start( cabinType, data);
                //        break;
                //    case "test":
                //        //InitLogging(settings, args, true);
                //        InitLoading(args);
                //        Test( data);
                //        break;

                //    default:
                //        MessageBox.Show("No ha podido iniciar " + Application.ProductName + " debudo a un fallo de configuración.\n\n"
                //            +"Para depurar este componente inicie la rutina con los parámetros adecuados.\n\n"
                //            +Application.ProductName+" "+VersionInfo.VersionOrBuild);
                //        Game.Exit();
                //        break;
                //}
            };
            if (Debugger.IsAttached) //Separa el flujo del código durante la depuración para que el IDE se detenga en el problema y no ante el código que lanza el mensaje.
            {
                doAction();
            }
            else
            {
                try
                {
                    doAction();
                }
                catch (Exception error)
                {
                    // Vamos a terminar el proceso. Hay que detener el watchdog.
                    Game.WatchdogProcess.Stop();
                    Trace.WriteLine(new FatalException(error));
                    //if (settings.ShowErrorDialogs)
                    if(true)
                    {
                        // Si ha ocurrido un error de carga pero el error interno se puede gestionar lo extraeremos
                        // y descartaremos la información extra relacionada con el sistema de archivos.
                        var loadError = error as FileLoadException;
                        if (loadError != null && (error.InnerException is FileNotFoundException || error.InnerException is DirectoryNotFoundException))
                            error = error.InnerException;

                        if (error is IncompatibleSaveException)
                        {
                            MessageBox.Show(String.Format(
                                "Save file is incompatible with this version of {0}.\n\n" +
                                "    {1}\n\n" +
                                "Saved version: {2}\n" +
                                "Current version: {3}",
                                Application.ProductName,
                                ((IncompatibleSaveException)error).SaveFile,
                                ((IncompatibleSaveException)error).VersionOrBuild,
                                VersionInfo.VersionOrBuild),
                                Application.ProductName + " " + VersionInfo.VersionOrBuild, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else if (error is InvalidCommandLine)
                            MessageBox.Show(String.Format(
                                "{0} comenzó con una línea de comandos que no es válida. {1} Argumentos dados:\n\n{2}",
                                Application.ProductName,
                                error.Message,
                                String.Join("\n", data.Select(d => "\u2022 " + d).ToArray())),
                                Application.ProductName + " " + VersionInfo.VersionOrBuild, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        //else if (error is Traveller.MissingTrackNodeException)
                        //    MessageBox.Show(String.Format("Tourmaline ha detectado una sección de vía que no está en tsection.dat y no puede continuar.\n\n" +
                        //        "Lo más probable es que no tenga XTracks o YTracks en esta ruta."));
                        else if (error is FileNotFoundException)
                        {
                            MessageBox.Show(String.Format(
                                    "Falta un archivo esencial y {0} no puede continuar.\n\n" +
                                    "    {1}",
                                    Application.ProductName, (error as FileNotFoundException).FileName),
                                    Application.ProductName + " " + VersionInfo.VersionOrBuild, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else if (error is DirectoryNotFoundException)
                        {
                            // Ñapa para intentar extraer el nombre del archivo actual desde el mensaje de excepción.
                            // No está disponible en ningún otro lado.
                            var re = new Regex("'([^']+)'").Match(error.Message);
                            var fileName = re.Groups[1].Success ? re.Groups[1].Value : error.Message;
                            MessageBox.Show(String.Format(
                                    "Falta un directorio esencial y {0} no puede continuar.\n\n" +
                                    "    {1}",
                                    Application.ProductName, fileName),
                                    Application.ProductName + " " + VersionInfo.VersionOrBuild, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            var errorSummary = error.GetType().FullName + ": " + error.Message;
                            //var logFile = Path.Combine(settings.LoggingPath, settings.LoggingFilename);
                            var logFile = string.Empty;
                            var openTracker = MessageBox.Show(String.Format(
                                "Se ha producido un error fatal y {0} no puede continuar.\n\n"+
                                    "    {1}\n\n" +
                                    "Este error puede ser debido a un bug o a datos corruptos.\n\n",
                                    Application.ProductName, errorSummary, logFile),
                                    Application.ProductName + " " + VersionInfo.VersionOrBuild, MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
                            if (openTracker == DialogResult.OK)
                                Process.Start("http://launchpad.net/or");
                        }
                    }
                    // Salimos del juego tras gestionar un error.
                    Game.Exit();
                }
            }
            UninitLoading();
        }

        /// <summary>
        /// Iniciamos el proceso de producción.
        /// </summary>
        void Start(string acttype, string[] args)
        {
            InitSimulator(args, "", acttype);

            MicroSim.Start();
            Viewer = new Viewer(MicroSim, Game);            

            //Game.ReplaceState(new GameStateViewer3D(Viewer));

            //Iniciamos el visualizador Tesla de SFM.
            Game.ReplaceState(new GameStateSFM3D(Viewer));
        }

        /// <summary>
        /// Prueba para verificar que RunActivity.exe se puede ejecutar, pero sin ejecutarlo.
        /// </summary>
        void Test(string[] args)
        {
            DateTime startTime = DateTime.Now;
            var exitGameState = new GameStateViewer3DTest(args);
            try
            {
                InitSimulator(args, "Test");
                MicroSim.Start();
                
                Viewer = new Viewer(MicroSim, Game);
                //Viewer.World.Map.mapFileName = "T3-1";

                Game.ReplaceState(exitGameState);
                //Game.PushState(new GameStateViewer3D(Viewer));
                Game.ReplaceState(new GameStateSFM3D(Viewer));
                exitGameState.LoadTime = (DateTime.Now.Subtract(startTime)).TotalMilliseconds;
                exitGameState.Passed = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Game.ReplaceState(exitGameState);
            }
        }

        class GameStateViewer3DTest : GameState
        {
            public bool Passed;
            public double LoadTime;

            readonly string[] Args;

            public GameStateViewer3DTest(string[] args)
            {
                Args = args;
            }

            internal override void Load()
            {
                Game.PopState();
            }

            internal override void Dispose()
            {
                //ExportTestSummary(Game.Settings, Args, Passed, LoadTime);
                Environment.ExitCode = Passed ? 0 : 1;

                base.Dispose();
            }

            //static void ExportTestSummary(UserSettings settings, string[] args, bool passed, double loadTime)
            //{
            //    // Append to CSV file in format suitable for Excel
            //    var summaryFileName = Path.Combine(UserSettings.UserDataFolder, "TestingSummary.csv");
            //    // Could fail if already opened by Excel
            //    try
            //    {
            //        using (var writer = File.AppendText(summaryFileName))
            //        {
            //            // Route, Activity, Passed, Errors, Warnings, Infos, Load Time, Frame Rate
            //            writer.WriteLine("{0},{1},{2},{3},{4:F1},{5:F1}",
            //                passed ? "Yes" : "No",
            //                ORTraceListener != null ? ORTraceListener.Counts[0] + ORTraceListener.Counts[1] : 0,
            //                ORTraceListener != null ? ORTraceListener.Counts[2] : 0,
            //                ORTraceListener != null ? ORTraceListener.Counts[3] : 0,
            //                loadTime,
            //                Viewer != null && Viewer.RenderProcess != null ? Viewer.RenderProcess.FrameRate.SmoothedValue : 0);
            //        }
            //    }
            //    catch { } // Ignore any errors
            //}
        }

        void InitLogging(string[] args)
        {
            InitLogging(args, false);
        }

        void InitLogging(string[] args, bool appendLog)
        {
            string loggingPath = "C:\\Users\\pcarrasco\\desktop";
            string loggingFileName = "TourmalineLog";
            //if (settings.Logging && (settings.LoggingPath.Length > 0) && Directory.Exists(settings.LoggingPath))
            if(Directory.Exists(loggingPath))
            {
                string fileName;
                try
                {
                    TimeSpan ahora = DateTime.Now.Subtract(new DateTime(0));
                    //fileName = string.Format(settings.LoggingFilename, Application.ProductName, VersionInfo.VersionOrBuild, VersionInfo.Version, VersionInfo.Build, ahora);
                    fileName = string.Format(loggingFileName, Application.ProductName, VersionInfo.VersionOrBuild, VersionInfo.Version, VersionInfo.Build, ahora);
                }
                catch (FormatException)
                {
                    fileName = loggingFileName;
                }
                foreach (var ch in Path.GetInvalidFileNameChars())
                    fileName = fileName.Replace(ch, '.');

                logFileName = Path.Combine(loggingPath, fileName);
                // Ensure we start with an empty file.
                if (!appendLog)
                    File.Delete(logFileName);
                // Make Console.Out go to the log file AND the output stream.
                Console.SetOut(new FileTeeLogger(logFileName, Console.Out));
                // Make Console.Error go to the new Console.Out.
                Console.SetError(Console.Out);
            }

            // Captures Trace.Trace* calls and others and formats.
            ORTraceListener = new ORTraceListener(Console.Out, false);
            ORTraceListener.TraceOutputOptions = TraceOptions.Callstack;
            // Trace.Listeners and Debug.Listeners are the same list.
            Trace.Listeners.Add(ORTraceListener);

            Console.WriteLine("This is a log file for {0}. Please include this file in bug reports.", Application.ProductName);
            LogSeparator();
            //if (settings.Logging)
            if(true)
            {
                //SystemInfo.WriteSystemDetails(Console.Out);
                LogSeparator();
                Console.WriteLine("Version    = {0}", VersionInfo.Version.Length > 0 ? VersionInfo.Version : "<none>");
                Console.WriteLine("Build      = {0}", VersionInfo.Build);
                if (logFileName.Length > 0)
                    Console.WriteLine("Logfile    = {0}", logFileName);
                Console.WriteLine("Executable = {0}", Path.GetFileName(Application.ExecutablePath));
                foreach (var arg in args)
                    Console.WriteLine("Argument   = {0}", arg);
                LogSeparator();
                //settings.Log();
                LogSeparator();
            }
            else
            {
                Console.WriteLine("Logging is disabled, only fatal errors will appear here.");
                LogSeparator();
            }
        }

        #region Loading progress indication calculations

        const int LoadingSampleCount = 100;

        string LoadingDataKey;
        string LoadingDataFilePath;
        long LoadingBytesInitial;
        int LoadingTime;
        TimeSpan LoadingStart;
        long[] LoadingBytesExpected;
        List<long> LoadingBytesActual;
        TimeSpan LoadingBytesSampleRate;
        TimeSpan LoadingNextSample = TimeSpan.MinValue;
        float LoadedPercent = -1;

        void InitLoading(string[] args)
        {
            // Obtiene los bytes iniciales. Se restan de todos los siguientes usos de GetProcessBytesLoaded().
            LoadingBytesInitial = GetProcessBytesLoaded();

            // Se mezclan todos los argumentos adecuados hacia el progama como clave para el archivo cache de carga.
            // Se ignoran todos los argumentos que no tengan un punto o los que comiencen con '/', ya que se entienden
            // como opciones de configuración de una actividad de explorador (hora, estación del año, etc.) o flags
            // como /test que no queremos cambiar.
            LoadingDataKey = String.Join(" ", args.Where(a => a.Contains('.') && !a.StartsWith("-") && !a.StartsWith("/")).ToArray()).ToLowerInvariant();
            var hash = new MD5CryptoServiceProvider();
            hash.ComputeHash(Encoding.Default.GetBytes(LoadingDataKey));
            var loadingHash = String.Join("", hash.Hash.Select(h => h.ToString("x2")).ToArray());
            //var dataPath = Path.Combine(UserSettings.UserDataFolder, "Load Cache");
            var dataPath = Path.Combine(AppContext.BaseDirectory, "Load Cache");
            LoadingDataFilePath = Path.Combine(dataPath, loadingHash + ".dat");

            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);

            var loadingTime = 0;
            var bytesExpected = new long[LoadingSampleCount];
            var bytesActual = new List<long>(LoadingSampleCount);
            // The loading of the cached data doesn't matter if anything goes wrong; we'll simply have no progress bar.
            try
            {
                using (var data = File.OpenRead(LoadingDataFilePath))
                {
                    using (var reader = new BinaryReader(data))
                    {
                        reader.ReadString();
                        loadingTime = reader.ReadInt32();
                        for (var i = 0; i < LoadingSampleCount; i++)
                            bytesExpected[i] = reader.ReadInt64();
                    }
                }
            }
            catch { }

            LoadingTime = loadingTime;
            LoadingStart = DateTime.Now.Subtract(new DateTime(0));
            LoadingBytesExpected = bytesExpected;
            LoadingBytesActual = bytesActual;
            // Using the cached loading time, pick a sample rate that will get us ~100 samples. Clamp to 100ms < x < 10,000ms.
            LoadingBytesSampleRate = new TimeSpan(0, 0, 0, 0, (int)MathHelper.Clamp(loadingTime / LoadingSampleCount, 100, 10000));
            LoadingNextSample = LoadingStart + LoadingBytesSampleRate;

#if DEBUG_LOADING
            Console.WriteLine("Loader: Cache key  = {0}", LoadingDataKey);
            Console.WriteLine("Loader: Cache file = {0}", LoadingDataFilePath);
            Console.WriteLine("Loader: Expected   = {0:N0} bytes", LoadingBytesExpected[LoadingSampleCount - 1]);
            Console.WriteLine("Loader: Sampler    = {0:N0} ms", LoadingBytesSampleRate);
            LogSeparator();
#endif
        }

        void UpdateLoading()
        {
            if (LoadingBytesActual == null)
                return;

            var bytes = GetProcessBytesLoaded() - LoadingBytesInitial;

            // Negative indicates no progress data; this happens if the loaded bytes exceeds the cached maximum expected bytes.
            LoadedPercent = -(float)(DateTime.Now.Subtract(LoadingStart).Second) / 15;
            for (var i = 0; i < LoadingSampleCount; i++)
            {
                // Find the first expected sample with more bytes. This means we're currently in the (i - 1) to (i) range.
                if (bytes <= LoadingBytesExpected[i])
                {
                    // Calculate the position within the (i - 1) to (i) range using straight interpolation.
                    var expectedP = i == 0 ? 0 : LoadingBytesExpected[i - 1];
                    var expectedC = LoadingBytesExpected[i];
                    var index = i + (float)(bytes - expectedP) / (expectedC - expectedP);
                    LoadedPercent = index / LoadingSampleCount;
                    break;
                }
            }

            if (DateTime.Now.Ticks > LoadingNextSample.Ticks)
            {
                // Record a sample every time we should.
                LoadingBytesActual.Add(bytes);
                LoadingNextSample += LoadingBytesSampleRate;
            }
        }

        void UninitLoading()
        {
            if (LoadingDataKey == null)
                return;

            var loadingTime = DateTime.Now.Subtract(LoadingStart);
            var bytes = GetProcessBytesLoaded() - LoadingBytesInitial;
            LoadingBytesActual.Add(bytes);

            // Convert from N samples to 100 samples.
            var bytesActual = new long[LoadingSampleCount];
            for (var i = 0; i < LoadingSampleCount; i++)
            {
                var index = (float)(i + 1) / LoadingSampleCount * (LoadingBytesActual.Count - 1);
                var indexR = index - Math.Floor(index);
                bytesActual[i] = (int)(LoadingBytesActual[(int)Math.Floor(index)] * indexR + LoadingBytesActual[(int)Math.Ceiling(index)] * (1 - indexR));
            }

            var bytesExpected = LoadingBytesExpected;
            var expected = bytesExpected[LoadingSampleCount - 1];
            var difference = bytes - expected;

            Console.WriteLine("Loader: Time       = {0:N0} ms", loadingTime.ToString());
            Console.WriteLine("Loader: Expected   = {0:N0} bytes", expected);
            Console.WriteLine("Loader: Actual     = {0:N0} bytes", bytes);
            Console.WriteLine("Loader: Difference = {0:N0} bytes ({1:P1})", difference, (float)difference / expected);
#if DEBUG_LOADING
            for (var i = 0; i < LoadingSampleCount; i++)
                Console.WriteLine("Loader: Sample {0,2}  = {1,13:N0} / {2,13:N0} ({3:N0})", i, bytesExpected[i], bytesActual[i], bytesActual[i] - bytesExpected[i]);
#endif
            Console.WriteLine();

            // Smoothly move all expected values towards actual values, by 10% each run. First run will just copy actual values.
            for (var i = 0; i < LoadingSampleCount; i++)
                bytesExpected[i] = bytesExpected[i] > 0 ? bytesExpected[i] * 9 / 10 + bytesActual[i] / 10 : bytesActual[i];

            // Like loading, saving the loading cache data doesn't matter if it fails. We'll just have no data to show progress with.
            try
            {
                using (var data = File.OpenWrite(LoadingDataFilePath))
                {
                    data.SetLength(0);
                    using (var writer = new BinaryWriter(data))
                    {
                        writer.Write(LoadingDataKey);
                        writer.Write((int)loadingTime.Millisecond);
                        for (var i = 0; i < LoadingSampleCount; i++)
                            writer.Write(bytesExpected[i]);
                    }
                }
            }
            catch { }
        }

        #endregion

        static void CopyLog(string toFile)
        {
            if (logFileName.Length == 0) return;
            File.Copy(logFileName, toFile, true);
        }

        void InitSimulator(string[] args, string mode)
        {
            InitSimulator(args, mode, "");
        }

        void InitSimulator( string[] args, string mode, string acttype)
        {
            Console.WriteLine(mode.Length <= 0 ? "Mode       = {1}" : acttype.Length > 0 ? "Mode       = {0}" : "Mode       = {0} {1}", mode, acttype);
            LogSeparator();

            Arguments = args;

            MicroSim = new MicroSim( Game.ContentPath);
            if (LoadingScreen == null)
                LoadingScreen = new LoadingScreenPrimitive(Game);
            //if (String.Compare(mode, "start", true) != 0) // no specific action for start, handled in start_timetable
            //{
            //    // for resume and replay : set timetable file and selected train info                
            //    MicroSim.ResourcesPath = String.Copy(args[0]);
            //}            
            //MicroSim.SetExplore("Velaro4");
            MicroSim.SetExplore("fgc4");
        }



        private bool HasExtension(string path, string ext) => Path.GetExtension(path).Equals(ext, StringComparison.OrdinalIgnoreCase);

        string GetTime(string timeString)
        {
            string[] time = timeString.Split(':');
            if (time.Length == 0)
                return null;

            string ts = null;
            try
            {
                ts = new TimeSpan(int.Parse(time[0]), time.Length > 1 ? int.Parse(time[1]) : 0, time.Length > 2 ? int.Parse(time[2]) : 0).ToString();
            }
            catch (ArgumentOutOfRangeException) { }
            catch (FormatException) { }
            catch (OverflowException) { }
            return ts;
        }

        void LogSeparator()
        {
            Console.WriteLine(new String('-', 80));
        }

        string GetSaveFile(string[] args)
        {
            if (args.Length == 0)
            {
                return GetMostRecentSave();
            }
            string saveFile = args[0];
            if (!saveFile.EndsWith(".save")) { saveFile += ".save"; }
            //return Path.Combine(UserSettings.UserDataFolder, saveFile);
            return Path.Combine(AppContext.BaseDirectory, saveFile);
        }

        string GetMostRecentSave()
        {
            //var directory = new DirectoryInfo(UserSettings.UserDataFolder);
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            var file = directory.GetFiles("*.save")
             .OrderByDescending(f => f.LastWriteTime)
             .First();
            if (file == null) throw new FileNotFoundException(String.Format(
               "Activity Save file '*.save' not found in folder {0}", directory));
            return file.FullName;
        }

        savedValues GetSavedValues(BinaryReader inf)
        {
            savedValues values = default(savedValues);
            // Skip the heading data used in Menu.exe
            // Done so even if not elegant to be compatible with existing save files
            var routeNameOrMultipl = inf.ReadString();
            if (routeNameOrMultipl == "$Multipl$")
                inf.ReadString(); // Route name
            values.pathName = inf.ReadString();    // Path name
            inf.ReadInt32();     // Time elapsed in game (secs)
            inf.ReadInt64();     // Date and time in real world
            inf.ReadSingle();    // Current location of player train TileX
            inf.ReadSingle();    // Current location of player train TileZ

            // Read initial position and pass to Simulator so it can be written out if another save is made.
            values.initialTileX = inf.ReadSingle();  // Initial location of player train TileX
            values.initialTileZ = inf.ReadSingle();  // Initial location of player train TileZ

            // Read in the real data...
            var savedArgs = new string[inf.ReadInt32()];
            for (var i = 0; i < savedArgs.Length; i++)
                savedArgs[i] = inf.ReadString();
            values.acttype = inf.ReadString();
            values.args = savedArgs;
            return values;
        }

        long GetProcessBytesLoaded()
        {
            NativeMathods.IO_COUNTERS counters;
            if (NativeMathods.GetProcessIoCounters(Process.GetCurrentProcess().Handle, out counters))
                return (long)counters.ReadTransferCount;

            return 0;
        }

        class LoadingPrimitive : RenderPrimitive
        {
            public readonly LoadingMaterial Material;
            readonly VertexBuffer VertexBuffer;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
            public LoadingPrimitive(Game game)
            {
                Material = GetMaterial(game);
                var verticies = GetVerticies(game);
                VertexBuffer = new VertexBuffer(game.GraphicsDevice, typeof(VertexPositionTexture), verticies.Length, BufferUsage.WriteOnly);
                VertexBuffer.SetData(verticies);
            }

            virtual protected LoadingMaterial GetMaterial(Game game)
            {
                return new LoadingMaterial(game);
            }

            virtual protected VertexPositionTexture[] GetVerticies(Game game)
            {
                var dd = (float)Material.Texture.Width / 2;
                return new[] {
                    new VertexPositionTexture(new Vector3(-dd - 0.5f, +dd + 0.5f, -3), new Vector2(0, 0)),
                    new VertexPositionTexture(new Vector3(+dd - 0.5f, +dd + 0.5f, -3), new Vector2(1, 0)),
                    new VertexPositionTexture(new Vector3(-dd - 0.5f, -dd + 0.5f, -3), new Vector2(0, 1)),
                    new VertexPositionTexture(new Vector3(+dd - 0.5f, -dd + 0.5f, -3), new Vector2(1, 1)),
                };
            }

            public override void Draw(GraphicsDevice graphicsDevice)
            {
                graphicsDevice.SetVertexBuffer(VertexBuffer);
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            }
        }

        class LoadingScreenPrimitive : LoadingPrimitive
        {
            public LoadingScreenPrimitive(Game game)
                : base(game)
            {
            }

            protected override LoadingMaterial GetMaterial(Game game)
            {
                return new LoadingScreenMaterial(game);
            }

            protected override VertexPositionTexture[] GetVerticies(Game game)
            {
                float w, h;

                if (Material.Texture == null)
                {
                    w = h = 0;
                }
                else
                {
                    w = (float)Material.Texture.Width;
                    h = (float)Material.Texture.Height;
                    var scaleX = (float)game.RenderProcess.DisplaySize.X / w;
                    var scaleY = (float)game.RenderProcess.DisplaySize.Y / h;
                    var scale = scaleX < scaleY ? scaleX : scaleY;
                    w = w * scale / 2;
                    h = h * scale / 2;
                }
                return new[] {
                    new VertexPositionTexture(new Vector3(-w - 0.5f, +h + 0.5f, -2), new Vector2(0, 0)),
                    new VertexPositionTexture(new Vector3(+w - 0.5f, +h + 0.5f, -2), new Vector2(1, 0)),
                    new VertexPositionTexture(new Vector3(-w - 0.5f, -h + 0.5f, -2), new Vector2(0, 1)),
                    new VertexPositionTexture(new Vector3(+w - 0.5f, -h + 0.5f, -2), new Vector2(1, 1)),
                };
            }
        }

        class LoadingBarPrimitive : LoadingPrimitive
        {
            public LoadingBarPrimitive(Game game)
                : base(game)
            {
            }

            protected override LoadingMaterial GetMaterial(Game game)
            {
                return new LoadingBarMaterial(game);
            }

            protected override VertexPositionTexture[] GetVerticies(Game game)
            {
                var w = game.RenderProcess.DisplaySize.X;
                var h = 10;
                var x = -w / 2 - 0.5f;
                var y = game.RenderProcess.DisplaySize.Y / 2 - h - 0.5f;
                return new[] {
                    new VertexPositionTexture(new Vector3(x + 0, -y - 0, -1), new Vector2(0, 0)),
                    new VertexPositionTexture(new Vector3(x + w, -y - 0, -1), new Vector2(1, 0)),
                    new VertexPositionTexture(new Vector3(x + 0, -y - h, -1), new Vector2(0, 1)),
                    new VertexPositionTexture(new Vector3(x + w, -y - h, -1), new Vector2(1, 1)),
                };
            }
        }

        class LoadingMaterial : Material
        {
            public readonly LoadingShader Shader;
            public readonly Texture2D Texture;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
            public LoadingMaterial(Game game)
                : base(null, null)
            {
                Shader = new LoadingShader(game.RenderProcess.GraphicsDevice);
                Texture = GetTexture(game);
            }

            virtual protected Texture2D GetTexture(Game game)
            {
                return SharedTextureManager.Get(game.RenderProcess.GraphicsDevice, Path.Combine(game.ResourcesPath, "Loading.png"));
            }

            public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
            {
                Shader.CurrentTechnique = Shader.Techniques["Loading"];
                Shader.LoadingTexture = Texture;

                graphicsDevice.BlendState = BlendState.NonPremultiplied;
            }

            public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
            {
                foreach (var item in renderItems)
                {
                    Shader.WorldViewProjection = item.XNAMatrix * XNAViewMatrix * XNAProjectionMatrix;
                    Shader.CurrentTechnique.Passes[0].Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }

            public override void ResetState(GraphicsDevice graphicsDevice)
            {
                graphicsDevice.BlendState = BlendState.Opaque;
            }
        }

        class LoadingScreenMaterial : LoadingMaterial
        {
            public LoadingScreenMaterial(Game game)
                : base(game)
            {
            }

            private bool isWideScreen(Game game)
            {
                float x = game.RenderProcess.DisplaySize.X;
                float y = game.RenderProcess.DisplaySize.Y;

                return (x / y > 1.5);
            }

            //protected override Texture2D GetTexture(Game game)
            //{
            //    Texture2D texture;
            //    GraphicsDevice gd = game.RenderProcess.GraphicsDevice;
            //    string defaultScreen = "inicio.ace";
            //    string path = Path.Combine(MicroSim.ResourcesPath, defaultScreen);
            //    if (File.Exists(path))
            //    {
            //        texture = Tourmaline.Formats.Msts.AceFile.Texture2DFromFile(gd, path);
            //    }
            //    else
            //    {
            //        texture = null;
            //    }
            //    return texture;
            //}
            protected override Texture2D GetTexture(Game game)
            {
                string auxPath = Path.Combine(game.ResourcesPath, "loadScreen.png");
                FileStream corriente = new FileStream(auxPath, FileMode.Open);
                GraphicsDevice gd = game.RenderProcess.GraphicsDevice;
                Texture2D texture = Texture2D.FromStream(gd, corriente);
                corriente.Dispose();
                return texture;
            }
        }

        class LoadingBarMaterial : LoadingMaterial
        {
            public LoadingBarMaterial(Game game)
                : base(game)
            {
            }

            public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
            {
                base.SetState(graphicsDevice, previousMaterial);
                Shader.CurrentTechnique = Shader.Techniques["LoadingBar"];
            }
        }

        class LoadingShader : Shader
        {
            readonly EffectParameter worldViewProjection;
            readonly EffectParameter loadingPercent;
            readonly EffectParameter loadingTexture;

            public Matrix WorldViewProjection { set { worldViewProjection.SetValue(value); } }

            public float LoadingPercent { set { loadingPercent.SetValue(value); } }

            public Texture2D LoadingTexture { set { loadingTexture.SetValue(value); } }

            public LoadingShader(GraphicsDevice graphicsDevice)
                : base(graphicsDevice, "Loading")
            {
                worldViewProjection = Parameters["WorldViewProjection"];
                loadingPercent = Parameters["LoadingPercent"];
                loadingTexture = Parameters["LoadingTexture"];
            }
        }

        static class NativeMathods
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool GetProcessIoCounters(IntPtr hProcess, out IO_COUNTERS lpIoCounters);

            [StructLayout(LayoutKind.Sequential)]
            public struct IO_COUNTERS
            {
                public UInt64 ReadOperationCount;
                public UInt64 WriteOperationCount;
                public UInt64 OtherOperationCount;
                public UInt64 ReadTransferCount;
                public UInt64 WriteTransferCount;
                public UInt64 OtherTransferCount;
            };
        }
    }

    public sealed class IncompatibleSaveException : Exception
    {
        public readonly string SaveFile;
        public readonly string VersionOrBuild;

        public IncompatibleSaveException(string saveFile, string versionOrBuild, Exception innerException)
            : base(null, innerException)
        {
            SaveFile = saveFile;
            VersionOrBuild = versionOrBuild;
        }

        public IncompatibleSaveException(string saveFile, string versionOrBuild)
            : this(saveFile, versionOrBuild, null)
        {
        }
    }

    public sealed class InvalidCommandLine : Exception
    {
        public InvalidCommandLine(string message)
            : base(message)
        {
        }
    }
}
