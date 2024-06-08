using Tourmaline.Processes;
using TOURMALINE.Common;
using System;
using System.Diagnostics;
using System.Threading;
using CancellationToken = TOURMALINE.Common.CancellationToken;
using CancellationTokenSource = TOURMALINE.Common.CancellationTokenSource;
using Tourmaline.Common;



namespace Tourmaline.Viewer3D.Processes
{
    public class LoaderProcess
    {
        public readonly Profiler Profiler = new Profiler("Loader");
        readonly ProcessState State = new ProcessState("Loader");
        readonly Game Game;
        readonly Thread Thread;
        readonly WatchdogToken WatchdogToken;
        readonly TOURMALINE.Common.CancellationTokenSource CancellationTokenSource;

        public LoaderProcess(Game game)
        {
            Game = game;
            Thread = new Thread(LoaderThread);
            WatchdogToken = new WatchdogToken(Thread);
            WatchdogToken.SpecialDispensationFactor = 6;
            CancellationTokenSource = new TOURMALINE.Common.CancellationTokenSource(WatchdogToken.Ping);
        }

        public void Start()
        {
            Game.WatchdogProcess.Register(WatchdogToken);
            Thread.Start();
        }

        public void Stop()
        {
            Game.WatchdogProcess.Unregister(WatchdogToken);
            CancellationTokenSource.Cancel();
            State.SignalTerminate();
        }

        public bool Finished
        {
            get
            {
                return State.Finished;
            }
        }

        /// <summary>
        /// Devuelve un token (objeto copiable) al que se puede consultar para cancelar (terminar) el cargador.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Toda la carga de código debe comprobar periódicamente (entre archivo y archivo) el token y salir en
        /// cuanto vea que está cancelado. (<see cref="CancellationToken.IsCancellationRequested"/>).
        /// </para>
        /// <para>
        /// Al leer <see cref="CancellationToken.IsCancellationRequested"/> envía un ping al <see cref="WatchdogToken"/>
        /// informando al <see cref="WatchdogProcess"/> que el proceso cargador todavía está funcionando.
        /// De cualquier forma, la frecuencia con que se hace <see cref="WatchdogToken.Ping()"/> indicarán cuando debe
        /// o no debe emplearse.
        /// </para>
        /// </remarks>
        public TOURMALINE.Common.CancellationToken CancellationToken
        {
            get
            {
                return CancellationTokenSource.Token;
            }
        }

        public void WaitTillFinished()
        {
            State.WaitTillFinished();
        }

        [ThreadName("Loader")]
        void LoaderThread()
        {
            Profiler.SetThread();
            Game.SetThreadLanguage();

            while (true)
            {
                //Espera hasta un nuevo comando "Update()"
                State.WaitTillStarted();
                if (State.Terminated)
                    break;
                try
                {
                    if (!DoLoad())
                        return;
                }
                finally
                {
                    // Signal finished so RenderProcess can start drawing
                    State.SignalFinish();
                }
            }
        }

        [CallOnThread("Updater")]
        internal void StartLoad()
        {
            Debug.Assert(State.Finished);
            State.SignalStart();
        }

        [ThreadName("Loader")]
        bool DoLoad()
        {
            if (Debugger.IsAttached)
            {
                Load();
            }
            else
            {
                try
                {
                    Load();
                }
                catch (Exception error)
                {
                    // Unblock anyone waiting for us, report error and die.
                    CancellationTokenSource.Cancel();
                    State.SignalTerminate();
                    Game.ProcessReportError(error);
                    return false;
                }
            }
            return true;
        }

        [CallOnThread("Loader")]
        public void Load()
        {
            Profiler.Start();
            try
            {
                WatchdogToken.Ping();
                Game.State.Load();
            }
            finally
            {
                Profiler.Stop();
            }
        }

    }
}
