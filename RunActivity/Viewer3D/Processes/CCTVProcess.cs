using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tourmaline.Processes;
using TOURMALINE.Common;
using TourmalineNetSDK;

namespace Tourmaline.Viewer3D.Processes
{
    public class CCTVProcess
    {
        readonly ProcessState State = new ProcessState("CCTV");
        public readonly Profiler profiler = new Profiler("CCTV");
        readonly Game game;
        readonly Thread thread;
        readonly WatchdogToken watchdogToken;
        CCTV controller;

        public CCTVProcess(Game game)
        {
            this.game = game;
            //this.thread = new Thread(CCTVThread);
            controller = new CCTV();
        }


        public void Start()
        {

        }
        public void Stop()
        {

        }

        [ThreadName("CCTV")]
        async Task CCTVThread()
        {
            profiler.SetThread();
            while (true)
            {
                State.WaitTillStarted();
                if (State.Terminated) break;
                try
                {
                    //bool popo = await controller.Init(null,1);
                }
                finally
                {
                    State.SignalFinish();
                }
            }
        }

        [ThreadName("CCTV")]
        bool DoRefresh()
        {

            return false;
        }
    }
}
