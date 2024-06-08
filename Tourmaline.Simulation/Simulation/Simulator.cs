using Microsoft.Xna.Framework;
using Tourmaline.Formats.Msts;
using Tourmaline.Simulation.RollingStocks;
using TOURMALINE.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Tourmaline.Common;
using TOURMALINE.Settings;
using Tourmaline.Simulation;
using Event = Tourmaline.Common.Event;

namespace Tourmaline.Simulation
{
    /// <summary>
    /// This contains all the essential code to operate trains along paths as defined
    /// in the activity.   It is meant to operate in a separate thread it handles the
    /// following:
    ///    track paths
    ///    switch track positions
    ///    signal indications
    ///    calculating positions and velocities of trains
    ///    
    /// Update is called regularly to
    ///     do physics calculations for train movement
    ///     compute new signal indications
    ///     operate ai trains
    ///     
    /// All keyboard input comes from the viewer class as calls on simulator's methods.
    /// </summary>
    public class Simulator
    {
        public static Random Random { get; private set; }
        public static double Resolution = 1000000; // resolution for calculation of random value with a pseudo-gaussian distribution
        public const float MaxStoppedMpS = 0.1f; // stopped is taken to be a speed less than this 

        public bool Paused = true;          // start off paused, set to true once the viewer is fully loaded and initialized
        public float GameSpeed = 1;
        /// <summary>
        /// Monotonically increasing time value (in seconds) for the simulation. Starts at 0 and only ever increases, at <see cref="GameSpeed"/>.
        /// Does not change if game is <see cref="Paused"/>.
        /// </summary>
        public double GameTime;
        /// <summary>
        /// "Time of day" clock value (in seconds) for the simulation. Starts at activity start time and may increase, at <see cref="GameSpeed"/>,
        /// or jump forwards or jump backwards.
        /// </summary>
        public double ClockTime;
        // while Simulator.Update() is running, objects are adjusted to this target time 
        // after Simulator.Update() is complete, the simulator state matches this time

        public readonly UserSettings Settings;

        public string BasePath;     // ie c:\program files\microsoft games\train simulator
        public string RoutePath;    // ie c:\program files\microsoft games\train simulator\routes\usa1  - may be different on different pc's

        // Primary Simulator Data 
        // These items represent the current state of the simulator 
        // In multiplayer games, these items must be kept in sync across all players
        // These items are what are saved and loaded in a game save.
        public string RoutePathName;    // ie LPS, USA1  represents the folder name
        public string RouteName;
        public string ActivityFileName;
        public string TimetableFileName;
        public bool TimetableMode;
        public bool PreUpdate;

        public TrackDatabaseFile TDB;
        public TrackSectionsFile TSectionDat;
        public List<int> StartReference = new List<int>();

        public string ExplorePathFile;
        public string ExploreConFile;
        public string patFileName;
        public string conFileName;

        public int DayAmbientLight;

        // Used in save and restore form
        public string PathName = "<unknown>";
        public float InitialTileX;
        public float InitialTileZ;

        // player locomotive
        public TrainCar PlayerLocomotive;    // Set by the Viewer - TODO there could be more than one player so eliminate this.

        public bool updaterWorking = false;

        public class QueryCarViewerLoadedEventArgs : EventArgs
        {
            public readonly TrainCar Car;
            public bool Loaded;

            public QueryCarViewerLoadedEventArgs(TrainCar car)
            {
                Car = car;
            }
        }

        public event System.EventHandler RequestTTDetachWindow;

        public Simulator(UserSettings settings, string activityPath, bool useTourmalineDirectory)
        {
            Random = new Random();

            TimetableMode = false;

            Settings = settings;
            RoutePath = Path.GetDirectoryName(Path.GetDirectoryName(activityPath));
            if (useTourmalineDirectory) RoutePath = Path.GetDirectoryName(RoutePath); // starting one level deeper!
            RoutePathName = Path.GetFileName(RoutePath);
            BasePath = Path.GetDirectoryName(Path.GetDirectoryName(RoutePath));
            DayAmbientLight = (int)Settings.DayAmbientLight;


            string ORfilepath = System.IO.Path.Combine(RoutePath, "OpenRails");

            Trace.Write(" DAT");
            if (Directory.Exists(RoutePath + @"\Openrails") && File.Exists(RoutePath + @"\Openrails\TSECTION.DAT"))
                TSectionDat = new TrackSectionsFile(RoutePath + @"\Openrails\TSECTION.DAT");
            else if (Directory.Exists(RoutePath + @"\GLOBAL") && File.Exists(RoutePath + @"\GLOBAL\TSECTION.DAT"))
                TSectionDat = new TrackSectionsFile(RoutePath + @"\GLOBAL\TSECTION.DAT");
            else
                TSectionDat = new TrackSectionsFile(BasePath + @"\GLOBAL\TSECTION.DAT");
            if (File.Exists(RoutePath + @"\TSECTION.DAT"))
                TSectionDat.AddRouteTSectionDatFile(RoutePath + @"\TSECTION.DAT");


#if ACTIVITY_EDITOR
            //  Where we try to load OR's specific data description (Station, connectors, etc...)
            orRouteConfig = ORRouteConfig.LoadConfig(TRK.Tr_RouteFile.FileName, RoutePath, TypeEditor.NONE);
            orRouteConfig.SetTraveller(TSectionDat, TDB);
#endif
            //Log = new CommandLog(this);
        }




        public void Start(CancellationToken cancellation)
        {
        }

        public void Stop()
        {

        }

        public void Restore(BinaryReader inf, string pathName, float initialTileX, float initialTileZ, CancellationToken cancellation)
        {
            ClockTime = inf.ReadDouble();
            PathName = pathName;
            InitialTileX = initialTileX;
            InitialTileZ = initialTileZ;
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write(ClockTime);
        }

        /// <summary>
        /// Which locomotive does the activity specified for the player.
        /// </summary>
        public TrainCar InitialPlayerLocomotive()
        {
            return PlayerLocomotive;
        }

        /// <summary>
        /// Convert and elapsed real time into clock time based on simulator
        /// running speed and paused state.
        /// </summary>
        public float GetElapsedClockSeconds(float elapsedRealSeconds)
        {
            return elapsedRealSeconds * (Paused ? 0 : GameSpeed);
        }

        /// <summary>
        /// Update the simulator state 
        /// elapsedClockSeconds represents the time since the last call to Simulator.Update
        /// Executes in the UpdaterProcess thread.
        /// </summary>
        [CallOnThread("Updater")]
        public void Update(float elapsedClockSeconds)
        {
            // Advance the times.
            GameTime += elapsedClockSeconds;
            ClockTime += elapsedClockSeconds;

            if (PlayerLocomotive != null)
            {
            }
        }

        /// <summary>
        /// The front end of a railcar is at MSTS world coordinates x1,y1,z1
        /// The other end is at x2,y2,z2
        /// Return a rotation and translation matrix for the center of the railcar.
        /// </summary>
        public static Matrix XNAMatrixFromMSTSCoordinates(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            // translate 1st coordinate to be relative to 0,0,0
            float dx = (float)(x1 - x2);
            float dy = (float)(y1 - y2);
            float dz = (float)(z1 - z2);

            // compute the rotational matrix  
            float length = (float)Math.Sqrt(dx * dx + dz * dz + dy * dy);
            float run = (float)Math.Sqrt(dx * dx + dz * dz);
            // normalize to coordinate to a length of one, ie dx is change in x for a run of 1
            if (length != 0)    // Avoid zero divide
            {
                dx /= length;
                dy /= length;   // ie if it is tilted back 5 degrees, this is sin 5 = 0.087
                run /= length;  //                              and   this is cos 5 = 0.996
                dz /= length;
            }
            else
            {                   // If length is zero all elements of its calculation are zero. Since dy is a sine and is zero,
                run = 1f;       // run is therefore 1 since it is cosine of the same angle?  See comments above.
            }


            // setup matrix values

            Matrix xnaTilt = new Matrix(1, 0, 0, 0,
                                     0, run, dy, 0,
                                     0, -dy, run, 0,
                                     0, 0, 0, 1);

            Matrix xnaRotation = new Matrix(dz, 0, dx, 0,
                                            0, 1, 0, 0,
                                            -dx, 0, dz, 0,
                                            0, 0, 0, 1);

            Matrix xnaLocation = Matrix.CreateTranslation((x1 + x2) / 2f, (y1 + y2) / 2f, -(z1 + z2) / 2f);
            return xnaTilt * xnaRotation * xnaLocation;
        }


        public CommandLog Log { get; set; }

        /// <summary>
        /// Derive log-file name from route path and activity name
        /// </summary>
        public string DeriveLogFile(string appendix)
        {
            string logfilebase = String.Empty;
            string logfilefull = String.Empty;

            if (!String.IsNullOrEmpty(ActivityFileName))
            {
                logfilebase = String.Copy(UserSettings.UserDataFolder);
                logfilebase = String.Concat(logfilebase, "_", ActivityFileName);
            }
            else
            {
                logfilebase = String.Copy(UserSettings.UserDataFolder);
                logfilebase = String.Concat(logfilebase, "_explorer");
            }

            logfilebase = String.Concat(logfilebase, appendix);
            logfilefull = String.Concat(logfilebase, ".csv");

            bool logExists = File.Exists(logfilefull);
            int logCount = 0;

            while (logExists && logCount < 100)
            {
                logCount++;
                logfilefull = String.Concat(logfilebase, "_", logCount.ToString("00"), ".csv");
                logExists = File.Exists(logfilefull);
            }

            if (logExists) logfilefull = String.Empty;

            return (logfilefull);
        }

    } // Simulator
}
