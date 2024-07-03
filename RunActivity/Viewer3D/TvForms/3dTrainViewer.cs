using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tourmaline.Simulation.RollingStocks;
using Tourmaline.Viewer3D.Materials;
using Tourmaline.Viewer3D.RollingStock;
using TOURMALINE.Common;

namespace Tourmaline.Viewer3D.TvForms
{
    //Objeto que contiene la escena del tren 3D a visualizar...
    //Basicamente contiene el tren y una cámara que iremos moviendo.
    internal class _3dTrainViewer
    {
        internal GraphicsDeviceControl GControl { get; private set; }
        internal Materials.SharedTextureManager TextureManager { get; private set; }
        internal Materials.SharedMaterialManager MaterialManager { get; private set; }
        internal Materials.SharedShapeManager ShapeManager { get; private set; }

        internal _3dBaseCamera Camera { get; set; }
        internal static Viewport DefaultViewport;

        internal Vector3 NearPoint { get; private set; }
        internal Vector3 FarPoint { get;private set; }

        protected TourmalineTrain mvarTrain;
        internal TourmalineTrain Train
        {
            get => mvarTrain;
            set
            {
                mvarTrain = value;

            }
        }


        internal Dictionary<TrainCar, _3dTrainCarViewer> mcolCars;

        internal _3dTrainViewer(GraphicsDeviceControl ctrl)
        {
            this.GControl = ctrl;
            Camera = new _3dCamera(ctrl);
        }

        internal void Initialize()
        {
            DefaultViewport = GControl.GraphicsDevice.Viewport;
            TextureManager = new Materials.SharedTextureManager(this);
            MaterialManager = new Materials.SharedMaterialManager(this);
            ShapeManager = new Materials.SharedShapeManager(this);
            initDictionary(); //Cargamos los visualizadores de cada coche.
        }
                         

        internal void Update(Materials.RenderFrame frame, long elapsedTime)
        {
            if (frame.IsScreenChanged)
                Camera.ScreenChanged();
            Camera.Update(elapsedTime);

            frame.PrepareFrame(this);
            Camera.PrepareFrame(frame, elapsedTime);
            frame.PrepareFrame(elapsedTime);
            TrainPrepareFrame(frame, elapsedTime);



        }

        internal void TrainPrepareFrame(Materials.RenderFrame frame, long elapsedTime)
        {
            if (null == mvarTrain) return;
            foreach(_3dTrainCarViewer visor in mcolCars.Values)
            {
                visor.PrepareFrame(frame, elapsedTime);
            }
        }

        private void initDictionary()
        {
            //Iniciamos el diccionario de coches a pintar.
            mcolCars = new Dictionary<TrainCar, _3dTrainCarViewer>();
            foreach (TrainCar car in mvarTrain.Cars)
            {
                _3dTrainCarViewer carViewer = new _3dTrainCarViewer((MSTSWagon)car, this, car.position);
                mcolCars.Add(car, carViewer);
            }                      
        }
        
    }
}
