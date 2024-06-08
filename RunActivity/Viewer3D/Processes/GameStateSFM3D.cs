using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tourmaline.Viewer3D.Processes
{
    internal class GameStateSFM3D:GameState
    {
        internal readonly Viewer mvarVisor;
        bool mvarFirstFrame = true;
        DateTime mvarLastTime; //El tiempo anterior entre elapseds
        DateTime mvarLastLoading; //La última vez que se cargó contenido

        public GameStateSFM3D(Viewer viewer)
        {
            mvarVisor = viewer;
            mvarLastTime = DateTime.Now;
            mvarLastLoading = DateTime.MinValue;
        }

        internal override void BeginRender(RenderFrame frame)
        {            
            if (mvarFirstFrame)
            {
                //Apagamos el modo de 10FPS para ir a una velocidad de refresco tan rápida como sea posible.
                Game.IsFixedTimeStep = false;
                Game.InactiveSleepTime = TimeSpan.Zero;
                mvarFirstFrame = false;
            }
            mvarVisor.BeginRender(frame); 
        }

        internal override void EndRender(RenderFrame frame)
        {
            mvarVisor.EndRender(frame);
        }

        internal override void Update(RenderFrame frame, DateTime now)
        {
            //Cada 250 milisegundos buscamos nuevo contenido para cargar
            //y eliminamos el cargador.
            long elapsed = now.Subtract(mvarLastTime).Ticks / 10000; //En milisegundos
            mvarLastTime = now;
            if(now.Subtract(mvarLastLoading).Ticks > 25000 && Game.LoaderProcess.Finished)
            {
                mvarVisor.World.LoadPrep();
                Game.LoaderProcess.StartLoad();
                mvarLastLoading= DateTime.Now;
            }
            Game.RenderProcess.ComputeFPS(elapsed);
            mvarVisor.Update(frame, elapsed); //En este modo es el visor quien se encarga de dibujar.
        }

        internal override void Load()
        {
            mvarVisor.Load();
        }

        internal override void Dispose()
        {
            mvarVisor.Terminate();
            if (null != Program.MicroSim)
                Program.MicroSim.Stop();
            base.Dispose();
        }
    }
}
