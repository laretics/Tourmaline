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

namespace Tourmaline.Viewer3D
{
    public class World
    {
        readonly Viewer Viewer;
        public readonly TrainDrawer Trains;
        public readonly MapDrawer Map;
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
            Map.PrepareFrame(frame, elapsed);
            Trains.PepareFrame(frame,elapsed);
        }

    }
}
