using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tourmaline.Processes;
using System.Threading;
using CancellationToken = TOURMALINE.Common.CancellationToken;
using CancellationTokenSource = TOURMALINE.Common.CancellationTokenSource;
using TOURMALINE.Common;
//Proceso de digitalización de cámaras de vídeo para mostrar en el HMI

namespace Tourmaline.Viewer3D.Processes
{
    public class VideoStreamingProcess
    {
        public readonly Profiler Profiler = new Profiler("VideoStreaming");
        readonly ProcessState State = new ProcessState("VideoStreaming");
        readonly Game Game;
        readonly Thread Thread;
        readonly WatchdogToken WatchdogToken;
        readonly CancellationTokenSource cancellationTokenSource;

        public VideoStreamingProcess(Game game)
        {
            this.Game = game;
            this.Thread = new Thread(StreamThread);
            this.WatchdogToken = new WatchdogToken(Thread);
            WatchdogToken.SpecialDispensationFactor = 5;
            cancellationTokenSource = new CancellationTokenSource(WatchdogToken.Ping);
        }

        public void Start()
        {
            Game.WatchdogProcess.Register(WatchdogToken);
            Thread.Start();
        }

        public void Stop()
        {
            Game.WatchdogProcess.Unregister(WatchdogToken);
            cancellationTokenSource.Cancel();
            State.SignalTerminate();
        }

        public bool Finished { get => State.Finished; }
        public CancellationToken CancellationToken { get=>CancellationToken;}
        public void WaitTillFinished() { State.WaitTillFinished(); }

        [ThreadName("VideoStreaming")]
        void StreamThread()
        {
            Profiler.SetThread();


        }

    }
}
