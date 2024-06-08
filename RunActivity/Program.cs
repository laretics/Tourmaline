using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Tourmaline.Common;
using Tourmaline.Simulation;
using Tourmaline.Viewer3D;
//using Tourmaline.Viewer3D.Debugging;
using Tourmaline.Viewer3D.Processes;
using TOURMALINE.Common;
//using TOURMALINE.Settings;

namespace Tourmaline
{
    static class NativeMethods
    {
        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern bool SetDllDirectory(string pathName);
    }


    static class Program
    {
        public static MicroSim MicroSim;
        public static Viewer Viewer;
        public static ORTraceListener ORTraceListener;
        public static string logFileName = "";

        [ThreadName("Render")]
        static void Main(string[] args)
        {
            var options = args.Where(a => a.StartsWith("-") || a.StartsWith("/")).Select(a => a.Substring(1));
            //var settings = new UserSettings(options);

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Native");
            path = Path.Combine(path, (Environment.Is64BitProcess) ? "X64" : "X86");
            NativeMethods.SetDllDirectory(path);

            //var game = new Game(settings);
            var game = new Game();
            game.PushState(new GameStateRunActivity(args));
            game.Run();
        }
    }
}
