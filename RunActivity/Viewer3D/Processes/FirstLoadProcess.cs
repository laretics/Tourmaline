using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Tourmaline.Formats.Msts;
using Tourmaline.Simulation.RollingStocks;

//Proceso de una sola ejecución que cargará todos los recursos gráficos en la memoria.

namespace Tourmaline.Viewer3D.Processes
{
    internal class FirstLoadProcess
    {
        internal static FirstLoadProcess Instance = new FirstLoadProcess();


        /// Ajustes de usuario
        private string mvarContentPath = "C:\\Tourmaline";
        private string mvarContentPath2 = "A:\\Tourmaline\\Tourmaline";

        public float PerformanceTunerTarget = 60; //Objetivo 60FPS
        public bool PerformanceTuner = true; //Forzamos recálculo dinámico de FPS.
        public bool VerticalSync = false; //Sincronía vertical.
        public float ViewingDistance = 4000.0f; //Distancia máxima de visión
        public Vector3 LightDirection = new Vector3(10, 0, 0);
        public float ViewingFOV = 45.0f;
        public bool LODViewingExtention = true; //Extensión de LOD a la máxima distancia de visualización
        public float LODBias = 0; //Nivel de detalle
        public string resolution = "1280x800";
        public bool FullScreen = false; //Juego a pantalla completa        

        /// Carpeta con los contenidos del juego
        public string ContentPath { get; private set; }
        /// Carpeta con los recursos del programa principal
        public string ResourcesPath { get; private set; }
        /// Carpeta con los datos de cartografía
        public string GeoPath { get; private set; }


        private void locateContentPath()
        {
            if (System.IO.Directory.Exists(mvarContentPath))
                ContentPath = mvarContentPath;
            else
                ContentPath = mvarContentPath2;

            ResourcesPath = Path.Combine(ContentPath, "Resources");
            GeoPath = Path.Combine(ContentPath, "Geo");
        }
        internal FirstLoadProcess()
        {
            locateContentPath();
        }

        internal TourmalineTrain loadTrain(string consistName)
        {
            TourmalineTrain salida = new TourmalineTrain();
            string basePath = FirstLoadProcess.Instance.ContentPath;
            string consistPath = Path.Combine(basePath, "Consists", consistName + ".con");
            ConsistFile consistFile = new ConsistFile(consistPath);
            //Hemos obtenido toda la información del tren a cargar desde el archivo "consist"
            foreach (Wagon coche in consistFile.Train.TrainCfg.WagonList)
            {
                string wagonFolder = string.Format("{0}\\trainset\\{1}", basePath, coche.Folder);
                string wagonFilePath = string.Format("{0}\\{1}.wag", wagonFolder, coche.Name);
                if (coche.IsEngine)
                    wagonFilePath = Path.ChangeExtension(wagonFilePath, ".eng");

                if(!File.Exists(wagonFilePath))
                {
                    Trace.TraceWarning($"No se encontró {(coche.IsEngine?"la locomotora":"el coche")}{wagonFilePath} en los archivos de la composición {consistName}");
                    continue;
                }
                try
                {
                    TrainCar car = Tourmaline.Simulation.RollingStocks.RollingStock.Load(wagonFilePath);
                    car.Flipped = coche.Flip;
                    car.UiD = coche.UiD;
                    salida.Cars.Add(car);
                    car.Train = salida;
                    car.position.XNAMatrix.Translation = new Microsoft.Xna.Framework.Vector3(salida.Length, 0, 0);
                    salida.Length += car.CarLengthM;
                }
                catch(Exception error)
                {
                    if (coche == consistFile.Train.TrainCfg.WagonList[0])
                        throw new FileLoadException(wagonFilePath,error);
                    Trace.WriteLine(new FileLoadException(wagonFilePath,error));
                }
            }
            salida.locateWagons();
            return salida;
        }
    }
}
