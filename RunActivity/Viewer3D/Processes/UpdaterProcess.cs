using Tourmaline.Processes;
using TOURMALINE.Common;
using System;
using System.Diagnostics;
using System.Threading;

namespace Tourmaline.Viewer3D.Processes
{
    public class UpdaterProcess
    {
        public readonly Profiler Profiler = new Profiler("Updater");
        readonly ProcessState State = new ProcessState("Updater");
        readonly Game Game;
        readonly Thread Thread;
        readonly WatchdogToken WatchdogToken;

        public UpdaterProcess(Game game)
        {
            Game = game;
            Thread = new Thread(UpdaterThread);
            WatchdogToken = new WatchdogToken(Thread);
        }

        public void Start()
        {
            Game.WatchdogProcess.Register(WatchdogToken);
            Thread.Start();
        }

        public void Stop()
        {
            Game.WatchdogProcess.Unregister(WatchdogToken);
            State.SignalTerminate();
        }

        public void WaitTillFinished()
        {
            State.WaitTillFinished();
        }

        [ThreadName("Updater")]
        void UpdaterThread()
        {
            Profiler.SetThread();

            while (true)
            {
                // Wait for a new Update() command
                State.WaitTillStarted();
                if (State.Terminated)
                    break;
                try
                {
                    if (!DoUpdate())
                        return;
                }
                finally
                {
                    // Signal finished so RenderProcess can start drawing
                    State.SignalFinish();
                }
            }
        }

        RenderFrame CurrentFrame;
        TimeSpan rtc;

        [CallOnThread("Render")]
        internal void StartUpdate(RenderFrame frame, TimeSpan rtc)
        {
            Debug.Assert(State.Finished);
            CurrentFrame = frame;
            this.rtc = rtc;
            State.SignalStart();
        }

        [ThreadName("Updater")]
        bool DoUpdate()
        {
            if (Debugger.IsAttached)
            {
                Update();
            }
            else
            {
                try
                {
                    Update();
                }
                catch (Exception error)
                {
                    // Unblock anyone waiting for us, report error and die.
                    State.SignalTerminate();
                    Game.ProcessReportError(error);
                    return false;
                }
            }
            return true;
        }

        [CallOnThread("Updater")]
        public void Update()
        {
            //En este procedimiento la GPU pinta sobre la superficie
            //llamada "renderFrame"
            Profiler.Start(); //Iniciamos el contador de eficiencia
            try
            {
                WatchdogToken.Ping(); //Estamos vivos. Se lo decimos al perro guardián.
                CurrentFrame.Clear(); //Borramos el dibujo anterior.
                if (Game.State != null) //Game state es el dibujador que está en la cima de estados.
                {
                    Game.State.Update(CurrentFrame, DateTime.Now); //Aquí es donde el gameState pinta en los diferentes canales del render frame.
                    CurrentFrame.Sort(); //Hemos pintado en desorden... algunos procesos son más rápidos que otros. Hay que ordenar las capas para representarlo todo bien.
                }
            }
            finally
            {
                Profiler.Stop(); //Paramos el cronómetro para el medidor de performance.
            }
        }
    }
}
