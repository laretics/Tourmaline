using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tourmaline.Common;
using TOURMALINE.Common;
using Tourmaline.Formats.Msts;
using Tourmaline.Simulation.RollingStocks;
using System.IO;
using Microsoft.Xna.Framework;

namespace Tourmaline.Simulation
{
    /// <summary>
    /// Contiene todo lo necesario para la representación de la pantalla de conducción
    /// En principio estoy intentando buscar la forma de representar el tren 3D en miniatura
    /// Más adelante añadiré las superficies para renderizar las vistas de las cámaras y
    /// el mapa de Onice
    /// </summary>
    public class MicroSim
    {
        //public readonly UserSettings Settings;
        public CommandLog Log { get; set; }
        public DateTime ClockTime { get; set; }
        public Microsoft.Xna.Framework.GameTime gameTime { get; set; }
        public bool Paused { get; set; }

        public string consistFileName { get; set; }
        public string contentPath;

        public TrackDatabaseFile TDB;
        public TrackSectionsFile TSectionDat;

        public TourmalineTrain PlayerTrain;
        public TrainCar PlayerLocomotive { get; set; }

        public MicroSim(string basePath)
        {
            Trace.Write(" DAT");
            contentPath = basePath;

            //Inicia el tren de la animación
            //PlayerTrain = new TourmalineTrain(this);


            Log = new CommandLog(this);
        }

        public void Start(CancellationToken cancellation)
        {
            gameTime = new Microsoft.Xna.Framework.GameTime();
            gameTime.TotalGameTime = DateTime.Now.TimeOfDay;
        }

        private bool OnQueryCarViewerLoaded(TrainCar car)
        {
            QueryCarViewerLoadedEventArgs query = new QueryCarViewerLoadedEventArgs(car); ;
            EventHandler<QueryCarViewerLoadedEventArgs> mapeador = QueryCarViewerLoaded;
            if(null!=mapeador)
                mapeador(this, query);
            return query.loaded;
        }
        public void Stop()
        {

        }
        public void Update(long elapsed)
        {
            //Aquí actualizamos las físicas de la simulación, si algo se mueve en
            //el trenecito que mostramos.
            //Elapsed es el lapso de tiempo que ha pasado desde la última actualización            

        }

        public void SetExplore(string consist)
        {
            consistFileName = consist;
            PlayerTrain = initializePlayerTrain();

        }

        private TourmalineTrain initializePlayerTrain()
        {
            TourmalineTrain salida = new TourmalineTrain(this);

            string consistPath = Path.Combine(contentPath, "Consists", consistFileName + ".con");

            ConsistFile conFile = new ConsistFile(consistPath);            
            foreach (Wagon coche in conFile.Train.TrainCfg.WagonList)
            {
                string wagonFolder = contentPath + @"\trainset\" + coche.Folder;
                string wagonFilePath = wagonFolder + @"\" + coche.Name + ".wag";
                if (coche.IsEngine)
                    wagonFilePath = Path.ChangeExtension(wagonFilePath, ".eng");

                if (!File.Exists(wagonFilePath))
                {
                    Trace.TraceWarning($"No se encontró {(coche.IsEngine ? "la locomotora" : "el coche")}{wagonFilePath} en la composición {consistFileName}");
                    continue;
                }

                try
                {
                    TrainCar car = RollingStock.Load(this, wagonFilePath);                   
                    car.Flipped = coche.Flip;
                    car.UiD = coche.UiD;
                    salida.Cars.Add(car);
                    car.Train = salida;
                    car.position.XNAMatrix.Translation = new Vector3(salida.Length, 0,0);
                    salida.Length += car.CarLengthM;                    
                }
                catch (Exception error)
                {
                    if (coche == conFile.Train.TrainCfg.WagonList[0])
                        throw new FileLoadException(wagonFilePath, error);
                    Trace.WriteLine(new FileLoadException(wagonFilePath, error));
                }
            }
            return salida;
        }

        public class QueryCarViewerLoadedEventArgs : EventArgs
        {
            public readonly TrainCar car;
            public bool loaded;
            public QueryCarViewerLoadedEventArgs(TrainCar car)
            {
                this.car = car;
            }
        }
        public event System.EventHandler<QueryCarViewerLoadedEventArgs> QueryCarViewerLoaded;
    }
}
