using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tourmaline.Formats.Msts;

namespace Tourmaline.Simulation.RollingStocks
{
    public class TourmalineTrain //Este es el tren que vamos a mostrar en el visor.
    {
        public List<TrainCar> Cars = new List<TrainCar>();
        public float Length {  get; set; }
        private MicroSim mvarSimulator;
        //public Traveller FrontTDBTraveller;
        //public Traveller RearTDBTraveller;
        public float SpeedMpS { get; set; }

        public TourmalineTrain(MicroSim simulator)
        {
            mvarSimulator = simulator;
//            FrontTDBTraveller = new Traveller();
//            RearTDBTraveller = new Traveller();
        }
        public TrainCar FirstCar { get => Cars[0]; }
        public TrainCar LastCar { get => Cars[Cars.Count - 1]; }
        public void locateWagons()
        {
            float x = 0;
            foreach (var car in Cars) 
            {
                car.position = new TOURMALINE.Common.WorldPosition(new Microsoft.Xna.Framework.Vector3(0, 0, x));
                car.SetUpWheels();
                car.ComputePosition();
                x += car.CarLengthM;
            }
        }


    }
}
