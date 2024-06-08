using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Input;
using Tourmaline.Simulation;

namespace Tourmaline.Common
{
    /// <summary>
    /// User may specify an automatic pause in the replay at a time measured from the end of the replay.
    /// </summary>
    public enum ReplayPauseState
    {
        Before,
        Due,        // Set by CommandLog.Replay(), tested by Viewer.Update()
        During,
        Done
    };

    public class CommandLog
    {

        public List<ICommand> CommandList = new List<ICommand>();
        public MicroSim Simulator { get; set; }
        public bool ReplayComplete { get; set; }
        public DateTime ReplayEndsAt { get; set; }
        public ReplayPauseState PauseState { get; set; }
        public bool CameraReplaySuspended { get; set; }

        private TimeSpan completeTime;
        private DateTime? resumeTime;
        private const double completeDelayS = 2;

        /// <summary>
        /// Preferred constructor.
        /// </summary>
        public CommandLog(MicroSim sim)
        {
            Simulator = sim;
        }

        /// <summary>
        /// When a command is created, it adds itself to the log.
        /// </summary>
        /// <param name="Command"></param>
        public void CommandAdd(ICommand command)
        {
            command.Time = Simulator.gameTime.ElapsedGameTime.Ticks/10000; // Note time that command was issued
            CommandList.Add(command);
        }

        /// <summary>
        /// Replays any commands that have become due.
        /// Issues commands from the replayCommandList at the same time that they were originally issued.
        /// <para>
        /// Assumes replayCommandList is already sorted by time.
        /// </para>
        /// </summary>
        public void Update(List<ICommand> replayCommandList)
        {
            TimeSpan elapsedTime = Simulator.gameTime.ElapsedGameTime;

            //if (PauseState == ReplayPauseState.Before)
            //{
            //    if (elapsedTime.Ticks > ReplayEndsAt.Subtract(new TimeSpan(0, 0, Simulator.Settings.ReplayPauseBeforeEndS)).Ticks) ;
            //    {
            //        PauseState = ReplayPauseState.Due;  // For Viewer.Update() to detect and pause.
            //    }
            //}

            if (replayCommandList.Count > 0)
            {
                var c = replayCommandList[0];
                if (c is PausedCommand)
                {
                    // Wait for the right duration and then action the command.
                    // ActivityCommands need dedicated code as the clock is no longer advancing.
                    if (resumeTime == null)
                    {
                        var resumeCommand = (PausedCommand)c;
                        resumeTime = DateTime.Now.AddSeconds(resumeCommand.PauseDurationS);
                    }
                    else
                    {
                        if (DateTime.Now >= resumeTime)
                        {
                            resumeTime = null;  // cancel trigger
                            ReplayCommand(elapsedTime, replayCommandList, c);
                        }
                    }
                }
                else
                {
                    // When the player uses a camera command during replay, replay continues but any camera commands in the 
                    // replayCommandList are skipped until the player pauses and exit from the Quit Menu.
                    // This allows some editing of the camera during a replay.
                    if (!(c is CameraCommand && CameraReplaySuspended))
                    {
                        ReplayCommand(elapsedTime, replayCommandList, c);
                    }
                    completeTime = elapsedTime.Add (new TimeSpan(0,0,0,(int)completeDelayS));  // Postpone the time for "Replay complete" message
                }
            }
            else
            {
                if (completeTime.Ticks != 0 && elapsedTime > completeTime)
                {
                    completeTime = new TimeSpan(0);       // Reset trigger so this only happens once
                    ReplayComplete = true;  // Flag seen by Viewer3D which announces "Replay complete".
                }
            }
        }

        private void ReplayCommand(TimeSpan elapsedTime, List<ICommand> replayCommandList, ICommand c)
        {
            c.Redo();                           // Action the command
            CommandList.Add(c);               // Add to the log of commands
            replayCommandList.RemoveAt(0);    // Remove it from the head of the replay list
        }

        /// <summary>
        /// Copies the command objects from the log into the file specified, first creating the file.
        /// </summary>
        /// <param name="filePath"></param>
        public void SaveLog(string filePath)
        {
            Stream stream = null;
            try
            {
                stream = new FileStream(filePath, FileMode.Create);
                BinaryFormatter formatter = new BinaryFormatter();
                // Re-sort based on time as tests show that some commands are deferred.
                CommandList.Sort((x, y) => x.Time.CompareTo(y.Time));
                formatter.Serialize(stream, CommandList);
            }
            catch (IOException)
            {
                // Do nothing but warn, ignoring errors.
                Trace.TraceWarning("SaveLog error writing command log " + filePath);
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                    Trace.WriteLine("\nList of commands to replay saved");
                }
            }
        }

        /// <summary>
        /// Copies the command objects from the file specified into the log, replacing the log's contents.
        /// </summary>
        /// <param name="fullFilePath"></param>
        public void LoadLog(string filePath)
        {
            Stream stream = null;
            try
            {
                stream = new FileStream(filePath, FileMode.Open);
                BinaryFormatter formatter = new BinaryFormatter();
                CommandList = (List<ICommand>)formatter.Deserialize(stream);
            }
            catch (IOException)
            {
                // Do nothing but warn, ignoring errors.
                Trace.TraceWarning("LoadLog error reading command log " + filePath);
            }
            finally
            {
                if (stream != null) { stream.Close(); }
            }
        }

        public static void ReportReplayCommands(List<ICommand> list)
        {
            Trace.WriteLine("\nList of commands to replay:");
            foreach (var c in list) { c.Report(); }
        }
    }
}
