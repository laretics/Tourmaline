using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Tourmaline.Common;
using Tourmaline.Simulation;
using Tourmaline.Simulation.RollingStocks;
using Tourmaline.Viewer3D;
//using Tourmaline.Viewer3D.Debugging;
using Tourmaline.Viewer3D.Processes;
using Tourmaline.Viewer3D.TvForms;
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
        public static TvForm mainForm;

        [ThreadName("Render")]
        static void Main(string[] args)
        {
            var options = args.Where(a => a.StartsWith("-") || a.StartsWith("/")).Select(a => a.Substring(1));
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Native");
            path = Path.Combine(path, (Environment.Is64BitProcess) ? "X64" : "X86");
            NativeMethods.SetDllDirectory(path);           

            //Código original
            //var game = new Game();
            //game.PushState(new GameStateRunActivity(args));
            //game.Run();

            //Vamos a modificarlo para cargar los recursos nada más comenzar la carga.
            //Microsoft.Xna.Framework.Game game = new Microsoft.Xna.Framework.Game(); //Creamos el objeto del juego.
            //Antes de comenzar el juego llamamos al cargador.
            Application.EnableVisualStyles();
            FirstLoadProcess cargador = FirstLoadProcess.Instance;
            TourmalineTrain auxTren = cargador.loadTrain("fgc4");
            mainForm = new TvForm();            
            mainForm.Show();
            mainForm.mvarFondo.Train = auxTren;

            Application.Run(mainForm); //Cedemos el control al formulario principal.
        }
    }
}
