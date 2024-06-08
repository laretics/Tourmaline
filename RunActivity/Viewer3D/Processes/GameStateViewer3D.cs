using System;

namespace Tourmaline.Viewer3D.Processes
{
    public class GameStateViewer3D : GameState
    {
        internal readonly Viewer Viewer;

        bool FirstFrame = true;
        int ProfileFrames = 0;

        public GameStateViewer3D(Viewer viewer)
        {
            Viewer = viewer;
            Viewer.microSim.Paused = true;
            //Viewer.QuitWindow.Visible = true;
        }

        internal override void BeginRender(RenderFrame frame)
        {
            // Do this here (instead of RenderProcess) because we only want to measure/time the running game.
            if (Game.Settings.Profiling)
                if ((Game.Settings.ProfilingFrameCount > 0 && ++ProfileFrames > Game.Settings.ProfilingFrameCount) 
                    || (Game.Settings.ProfilingTime > 0 && Viewer != null && 
                        Viewer.RealTime.Ticks >= Game.Settings.ProfilingTime/1000))
                    Game.PopState();

            if (FirstFrame)
            {
                // Turn off the 10FPS fixed-time-step and return to running as fast as we can.
                Game.IsFixedTimeStep = false;
                Game.InactiveSleepTime = TimeSpan.Zero;

                // We must create these forms on the main thread (Render) or they won't pump events correctly.

                Viewer.SoundDebugFormEnabled = false;

                FirstFrame = false;
            }
            Viewer.BeginRender(frame);
        }

        internal override void EndRender(RenderFrame frame)
        {
            Viewer.EndRender(frame);
        }

        double LastLoadRealTime;
        TimeSpan LastTotalRealSeconds = new TimeSpan(-1);
        double[] AverageElapsedRealTime = new double[10];
        int AverageElapsedRealTimeIndex;

        internal override void Update(RenderFrame frame, TimeSpan span)
        {
            // Every 250ms, check for new things to load and kick off the loader.
            float totalRealSeconds = span.Ticks / 1000;
            if (LastLoadRealTime + 0.25 < totalRealSeconds && Game.LoaderProcess.Finished)
            {
                LastLoadRealTime = totalRealSeconds;
                //Viewer.World.LoadPrep();
                Game.LoaderProcess.StartLoad();
            }

            // The first time we update, the TotalRealSeconds will be ~time
            // taken to load everything. We'd rather not skip that far through
            // the simulation so the first time we deliberately have an
            // elapsed real and clock time of 0.0s.
            if (LastTotalRealSeconds.Ticks<0 )
                LastTotalRealSeconds = span;
            // We would like to avoid any large jumps in the simulation, so
            // this is a 4FPS minimum, 250ms maximum update time.
            else if (span.Subtract(LastTotalRealSeconds).Ticks>2500f)
                LastTotalRealSeconds = span;

            TimeSpan elapsedRealTime = span.Subtract(LastTotalRealSeconds);
            LastTotalRealSeconds = span;

            if (elapsedRealTime.Ticks > 0)
            {
                // Store the elapsed real time, but also loop through overwriting any blank entries.
                do
                {
                    AverageElapsedRealTime[AverageElapsedRealTimeIndex] = elapsedRealTime.Ticks;
                    AverageElapsedRealTimeIndex = (AverageElapsedRealTimeIndex + 1) % AverageElapsedRealTime.Length;
                } while (AverageElapsedRealTime[AverageElapsedRealTimeIndex] == 0);

                // Elapsed real time is now the average.
                double tiempoMedio = 0;
                for (var i = 0; i < AverageElapsedRealTime.Length; i++)
                    tiempoMedio += AverageElapsedRealTime[i] / AverageElapsedRealTime.Length;
                Game.RenderProcess.ComputeFPS((float)tiempoMedio);
                Viewer.Update(frame, new TimeSpan((int)tiempoMedio));
            }
            else
            {
                Game.RenderProcess.ComputeFPS(0);
                Viewer.Update(frame, new TimeSpan(0));
            }
            // TODO: ComputeFPS should be called in UpdaterProcess.Update() but needs delta time.                       
        }

        internal override void Load()
        {
            Viewer.Load();
        }

        internal override void Dispose()
        {
            Viewer.Terminate();
            if (Program.MicroSim != null)
                Program.MicroSim.Stop();
            base.Dispose();
        }

    }
}
