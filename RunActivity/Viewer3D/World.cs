using ACadSharp;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tourmaline.Viewer3D.Popups;
using TOURMALINE.Common;


/*
    public void ComputePosition(Traveller traveler, bool backToFront, float elapsedTimeS, float distance, float speed)
    {
        for (var j = 0; j < Parts.Count; j++)
            Parts[j].InitLineFit();
        //var tileX = traveler.TileX;
        //var tileZ = traveler.TileZ;
        if (Flipped == backToFront)
        {
            var o = -CarLengthM / 2 - CentreOfGravityM.Z;
            for (var k = 0; k < WheelAxles.Count; k++)
            {
                var d = WheelAxles[k].OffsetM - o;
                o = WheelAxles[k].OffsetM;
                traveler.Move(d);
                var x = traveler.X;// + 2048 * (traveler.TileX - tileX);
                var y = traveler.Y;
                var z = traveler.Z;// + 2048 * (traveler.TileZ - tileZ);
                WheelAxles[k].Part.AddWheelSetLocation(1, o, x, y, z, 0, traveler);
            }
            o = CarLengthM / 2 - CentreOfGravityM.Z - o;
            traveler.Move(o);
        }
        else
        {
            var o = CarLengthM / 2 - CentreOfGravityM.Z;
            for (var k = WheelAxles.Count - 1; k >= 0; k--)
            {
                var d = o - WheelAxles[k].OffsetM;
                o = WheelAxles[k].OffsetM;
                traveler.Move(d);
                var x = traveler.X;// + 2048 * (traveler.TileX - tileX);
                var y = traveler.Y;
                var z = traveler.Z;// + 2048 * (traveler.TileZ - tileZ);
                WheelAxles[k].Part.AddWheelSetLocation(1, o, x, y, z, 0, traveler);
            }
            o = CarLengthM / 2 + CentreOfGravityM.Z + o;
            traveler.Move(o);
        }

        TrainCarPart p0 = Parts[0];
        for (int i = 1; i < Parts.Count; i++)
        {
            TrainCarPart p = Parts[i];
            p.FindCenterLine();
            if (p.SumWgt > 1.5)
                p0.AddPartLocation(1, p);
        }
        p0.FindCenterLine();
        Vector3 fwd = new Vector3(p0.B[0], p0.B[1], -p0.B[2]);
        // Check if null vector - The Length() is fine also, but may be more time consuming - By GeorgeS
        if (fwd.X != 0 && fwd.Y != 0 && fwd.Z != 0)
            fwd.Normalize();
        Vector3 side = Vector3.Cross(Vector3.Up, fwd);
        // Check if null vector - The Length() is fine also, but may be more time consuming - By GeorgeS
        if (side.X != 0 && side.Y != 0 && side.Z != 0)
            side.Normalize();
        Vector3 up = Vector3.Cross(fwd, side);
        Matrix m = Matrix.Identity;
        m.M11 = side.X;
        m.M12 = side.Y;
        m.M13 = side.Z;
        m.M21 = up.X;
        m.M22 = up.Y;
        m.M23 = up.Z;
        m.M31 = fwd.X;
        m.M32 = fwd.Y;
        m.M33 = fwd.Z;
        m.M41 = p0.A[0];
        m.M42 = p0.A[1] + 0.275f;
        m.M43 = -p0.A[2];
        //WorldPosition.XNAMatrix = m;
        //WorldPosition.TileX = tileX;
        //WorldPosition.TileZ = tileZ;

        UpdatedTraveler(traveler, elapsedTimeS, distance, speed);

        // calculate truck angles
        for (int i = 1; i < Parts.Count; i++)
        {
            TrainCarPart p = Parts[i];
            if (p.SumWgt < .5)
                continue;
            if (p.SumWgt < 1.5)
            {   // single axle pony trunk
                float d = p.OffsetM - p.SumOffset / p.SumWgt;
                if (-.2 < d && d < .2)
                    continue;
                p.AddWheelSetLocation(1, p.OffsetM, p0.A[0] + p.OffsetM * p0.B[0], p0.A[1] + p.OffsetM * p0.B[1], p0.A[2] + p.OffsetM * p0.B[2], 0, null);
                p.FindCenterLine();
            }
            Vector3 fwd1 = new Vector3(p.B[0], p.B[1], -p.B[2]);
            if (fwd1.X == 0 && fwd1.Y == 0 && fwd1.Z == 0)
            {
                p.Cos = 1;
            }
            else
            {
                fwd1.Normalize();
                p.Cos = Vector3.Dot(fwd, fwd1);
            }

            if (p.Cos >= .99999f)
                p.Sin = 0;
            else
            {
                p.Sin = (float)Math.Sqrt(1 - p.Cos * p.Cos);
                if (fwd.X * fwd1.Z < fwd.Z * fwd1.X)
                    p.Sin = -p.Sin;
            }
        }
    }
*/



namespace Tourmaline.Viewer3D
{
    public class World
    {
        readonly Viewer Viewer;
        public readonly TrainDrawer Trains;
        public readonly MapDrawer Map;
        public SkyViewer Sky { get; set; }
        private bool mvarFirstLoad = true;
        private bool mvarPerformanceTuner = false; //Ajuste dinámico de FPS
        private readonly int performanceInitialLODBias;
        private readonly int performanceInitialViewingDistance;
        private int lastSecond; //Usado para representar mensajitos



        [CallOnThread("Render")]
        public World(Viewer viewer, string mapFileName)
        {
            this.Viewer = viewer;            
            Trains = new TrainDrawer(viewer);
            Map = new MapDrawer();
            Map.mapFileName = System.IO.Path.Combine(viewer.Game.GeoPath, mapFileName + ".dwg");
            performanceInitialLODBias = (int)viewer.Game.LODBias;
            performanceInitialViewingDistance = (int)viewer.Game.ViewingDistance;
            Sky = new SkyViewer(viewer);
        }

        [CallOnThread("Loader")]
        public void Load()
        {
            Trains.Load();
            if(mvarFirstLoad)
            {
                Map.Load();
                Viewer.ShapeManager.Mark();
                Viewer.MaterialManager.Mark();
                Viewer.TextureManager.Mark();
                Trains.Mark();
                Viewer.Mark();
                Viewer.ShapeManager.Sweep();
                Viewer.MaterialManager.Sweep();
                Viewer.TextureManager.Sweep();
                mvarFirstLoad = false;
            }
        }

        [CallOnThread("Updater")]
        public void Update(long elapsed)
        {
            if(mvarPerformanceTuner && Viewer.RenderProcess.IsActive)
            {
                //Calculamos cuánto debemos cambiar el FPS actual para 
                //alcanzar el objetivo.
                //  +ve= por debajo/demasiado nivel de detalle
                //  -ve= por encima/no suficiente nivel de detalle
                float fpsTarget = Viewer.Game.PerformanceTunerTarget - Viewer.RenderProcess.FrameRate.SmoothedValue;

                //Si activamos la sincronía vertical sólo podemos poner 60FPS.
                //Esto significa que tenemos que bajar la velocidad a 57FPS.
                if (Viewer.Game.VerticalSync && Viewer.Game.PerformanceTunerTarget > 55)
                    fpsTarget -= 3;
                // Ajustamos los FPS a +1 (añadir detalle)
                //              0 (quedarnos como estamos)
                //                  -1 (quitar detalle)
                int fpsChange = fpsTarget < -2.5 ? +1 : fpsTarget > 2.5 ? -1 : 0;
                // Si no estamos limitados por la sincronía vertical no
                // importa calcular el cambio de CPU. Sólo asumiremos que el
                // nivel de detalle es correcto.
                float cpuTarget = 0f;
                int cpuChange = 1;
                if(Viewer.Game.VerticalSync)
                {
                    float cpuTargetRender = Viewer.RenderProcess.profiler.Wall.SmoothedValue - 90;
                    float cpuTargetUpdater = Viewer.UpdaterProcess.Profiler.Wall.SmoothedValue - 90;
                    cpuTarget = cpuTargetRender > cpuTargetUpdater ? cpuTargetRender : cpuTargetUpdater;

                    //Ajustamos el ajuste de FPS
                    cpuChange = cpuTarget < -2.5 ? +1 : cpuTarget > 2.5 ? -1 : 0;
                }

                //Ajustamos la distancia de visionado jugando con los FPS
                float oldViewingDistance = Viewer.Game.ViewingDistance;
                if (fpsChange < 0)
                    Viewer.Game.ViewingDistance -= (int)(fpsTarget - 1.5);
                else if (cpuChange < 0)
                    Viewer.Game.ViewingDistance -= (int)(cpuTarget - 1.5);
                else if (fpsChange > 0 && cpuChange > 0)
                    Viewer.Game.ViewingDistance += (int)(-fpsTarget - 1.5);
                Viewer.Game.ViewingDistance = (int)MathHelper.Clamp(Viewer.Game.ViewingDistance, 500, 10000);
                Viewer.Game.LODBias = (int)MathHelper.Clamp(performanceInitialLODBias * 100 * ((float)Viewer.Game.ViewingDistance / performanceInitialViewingDistance - 1), -100, 100);
                //Si hubiéramos cambiado la distancia de vista habrá que actualizar las matrices de las cámaras.
                if (oldViewingDistance != Viewer.Game.ViewingDistance)
                    Viewer.Camera.ScreenChanged();

                //Ya está hecho... en el próximo prep (cada 250ms) volveremos a hacer las comprobaciones
                mvarPerformanceTuner = false;
            }

            //Estos mensajitos se muestran desde este proceso. Es una prueba.            
            int segundo = DateTime.Now.Second;
            if (segundo % 6 == 0 && lastSecond != segundo)
            {
                Viewer.messageWindow.AddMessage(string.Format("Son las {0}", segundo), 1000);
                lastSecond = segundo;
            }

        }
        [CallOnThread("Updater")]
        public void LoadPrep()
        {
            Trains.LoadPrep();
            Map.LoadPrep();
            mvarPerformanceTuner = Viewer.Game.PerformanceTuner;
        }

        [CallOnThread("Updater")]
        public void PrepareFrame(RenderFrame frame,long elapsed)
        {
            Trains.PepareFrame(frame,elapsed);
        }

    }
}
