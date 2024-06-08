using System.Threading;

namespace Tourmaline.Processes
{
    public class ProcessState
    {
        public bool Finished { get; private set; }
        public bool Terminated { get; private set; }
        readonly ManualResetEvent StartEvent = new ManualResetEvent(false);
        readonly ManualResetEvent FinishEvent = new ManualResetEvent(true);
        readonly ManualResetEvent TerminateEvent = new ManualResetEvent(false);
        readonly WaitHandle[] StartEvents;
        readonly WaitHandle[] FinishEvents;
#if DEBUG_THREAD_PERFORMANCE
        StreamWriter DebugFileStream;
#endif

        public ProcessState(string name)
        {
            Finished = true;
            StartEvents = new[] { StartEvent, TerminateEvent };
            FinishEvents = new[] { FinishEvent, TerminateEvent };
#if DEBUG_THREAD_PERFORMANCE
            DebugFileStream = new StreamWriter(File.OpenWrite("debug_thread_" + name.ToLowerInvariant() + "_state.csv"));
            DebugFileStream.Write("Time,Event\n");
#endif
        }

        public void SignalStart()
        {
#if DEBUG_THREAD_PERFORMANCE
            DebugFileStream.Write("{0},SS\n", DateTime.Now.Ticks);
#endif
            Finished = false;
            FinishEvent.Reset();
            StartEvent.Set();
        }

        public void SignalFinish()
        {
#if DEBUG_THREAD_PERFORMANCE
            DebugFileStream.Write("{0},SF\n", DateTime.Now.Ticks);
#endif
            Finished = true;
            StartEvent.Reset();
            FinishEvent.Set();
        }

        public void SignalTerminate()
        {
#if DEBUG_THREAD_PERFORMANCE
            DebugFileStream.Write("{0},ST\n", DateTime.Now.Ticks);
#endif
            Terminated = true;
            TerminateEvent.Set();
        }

        public void WaitTillStarted()
        {
#if DEBUG_THREAD_PERFORMANCE
            DebugFileStream.Write("{0},WTS+\n", DateTime.Now.Ticks);
#endif
            WaitHandle.WaitAny(StartEvents);
#if DEBUG_THREAD_PERFORMANCE
            DebugFileStream.Write("{0},WTS-\n", DateTime.Now.Ticks);
#endif
        }

        public void WaitTillFinished()
        {
#if DEBUG_THREAD_PERFORMANCE
            DebugFileStream.Write("{0},WTF+\n", DateTime.Now.Ticks);
#endif
            WaitHandle.WaitAny(FinishEvents);
#if DEBUG_THREAD_PERFORMANCE
            DebugFileStream.Write("{0},WTF-\n", DateTime.Now.Ticks);
#endif
        }
    }
}

