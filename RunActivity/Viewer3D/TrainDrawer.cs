using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tourmaline.Simulation.RollingStocks;
using Tourmaline.Viewer3D.RollingStock;
using TOURMALINE.Common;
using Tourmaline.Simulation;

namespace Tourmaline.Viewer3D
{

    public class TrainDrawer
    {
        readonly Viewer viewer;

        public Dictionary<TrainCar, TrainCarViewer> cars = new Dictionary<TrainCar, TrainCarViewer>();
        List<TrainCar> visibleCars = new List<TrainCar>();

        public TrainDrawer(Viewer viewer)
        {
            this.viewer = viewer;
            viewer.microSim.QueryCarViewerLoaded += MicroSim_QueryCarViewerLoaded;

        }

        private void MicroSim_QueryCarViewerLoaded(object sender, MicroSim.QueryCarViewerLoadedEventArgs e)
        {
            Dictionary<TrainCar, TrainCarViewer> coches = this.cars;
            if (coches.ContainsKey(e.car))
                e.loaded = true;
        }

        public bool carLoaded(TrainCar car)
        {
            return cars.ContainsKey(car); 
        }

        [CallOnThread("Loader")]
        public void Load()
        {
            CancellationToken cancellation = viewer.LoaderProcess.CancellationToken;
            List<TrainCar> xvisibleCars = this.visibleCars;
            Dictionary<TrainCar, TrainCarViewer> xcars = this.cars;
            if(xvisibleCars.Any(c=>!xcars.ContainsKey(c))||xcars.Keys.Any(c=>!xvisibleCars.Contains(c)))
            {
                Dictionary<TrainCar, TrainCarViewer> nuevos = new Dictionary<TrainCar, TrainCarViewer>();
                foreach(TrainCar car in xvisibleCars)
                {
                    if (cancellation.IsCancellationRequested) break;
                    try
                    {
                        if (xcars.ContainsKey(car))
                            nuevos.Add(car, xcars[car]);
                        else
                            nuevos.Add(car, loadCar(car));
                    }
                    catch (Exception error)
                    {
                        Trace.WriteLine(new FileLoadException(car.WagFilePath, error));
                    }
                }
                this.cars = nuevos;
                //Los coches que no sean visibles ahora serán descargados.
                foreach(KeyValuePair<TrainCar,TrainCarViewer> xcar in xcars)
                {
                    xcar.Value.Unload();
                }
            }               
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            Dictionary<TrainCar, TrainCarViewer> xCars = this.cars;
            foreach(TrainCarViewer xcar in xCars.Values)
            {
                xcar.Mark();
            }
        }

        [CallOnThread("Updater")]
        public TrainCarViewer GetViewer(TrainCar car)
        {
            Dictionary<TrainCar, TrainCarViewer> xcars = this.cars;
            if (xcars.ContainsKey(car))
                return xcars[car];
            Dictionary<TrainCar, TrainCarViewer> nuevos = new Dictionary<TrainCar, TrainCarViewer>();
            nuevos.Add(car, loadCar(car));
            this.cars = nuevos;
            return nuevos[car];
        }

        [CallOnThread("Updater")]
        public void LoadPrep()
        {
            List<TrainCar> colvisibleCars = new List<TrainCar>();
            float removeDistance = viewer.Game.ViewingDistance * 1.5f;
            //visibleCars.Add(viewer.PlayerLocomotive);
            foreach (TrainCar car in viewer.microSim.PlayerTrain.Cars)
            {
                colvisibleCars.Add(car);
            }
            this.visibleCars = colvisibleCars;          
        }

        [CallOnThread("Updater")]
        public void PepareFrame(RenderFrame frame, long elapsedTime)
        {
            Dictionary<TrainCar, TrainCarViewer> xCars = this.cars;
            foreach (TrainCarViewer xcar in xCars.Values)
                xcar.PrepareFrame(frame, elapsedTime);
        }

        internal TrainCarViewer loadCar(TrainCar car)
        {
            Trace.Write("C");
            TrainCarViewer carViewer = car is MSTSWagon ? new WagonViewer(viewer, car as MSTSWagon,car.position) : null;
            return carViewer;
        }
    }
}
