using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AXISMEDIACONTROLLib;

namespace EdgeStorage
{
	public partial class PlaybackWindow : Form
	{
		enum State
		{
			Stopped,
			Paused,
			Playing
		}

		State state = State.Stopped;

		private bool compatibilityMode;
		private bool isSeeking = false;

		private ulong duration = 0;
		private decimal playbackRate = 1;

		public PlaybackWindow(string recordingUrl,
			string userName,
			string password,
			Version firmwareVersion)
		{
			InitializeComponent();
			Text = "Playback Window - " + recordingUrl;

			// Firmware version 5.50 and above supports native seeking functionality.
			// This sample also provides a work-around to support seeking in AMC for older
			// firmware versions.
			compatibilityMode = firmwareVersion < new Version(5, 50);

			// For best performance this value should match the GOV-length
			BackStepLengthMilliSec = 1000;

			// We set properties here for clarity
			amc.BackgroundColor = 0; // black
			amc.MaintainAspectRatio = true;
			amc.StretchToFit = true;
			amc.Popups &= ~((int)AMC_POPUPS.AMC_POPUPS_NO_VIDEO); // Hide "Video stopped" message
			amc.ToolbarConfiguration = "default -pixcount -settings";

			// VMR9 is the recommended video renderer for playback but VMR7 works better when using the
			// work-around provided to support seeking for older firmware versions (< 5.50).
			amc.VideoRenderer = compatibilityMode ? (int)AMC_VIDEO_RENDERER.AMC_VIDEO_RENDERER_VMR7 : // VMR7
																							(int)AMC_VIDEO_RENDERER.AMC_VIDEO_RENDERER_VMR9;  // VMR9
			amc.EnableOverlays = false;

			// Optimize for playback of recording (not live)
			amc.PlaybackMode = (int)AMC_PLAYBACK_MODE.AMC_PM_RECORDING;

			amc.MediaURL = recordingUrl;
			if (!String.IsNullOrEmpty(userName))
			{
				amc.MediaUsername = userName;
				amc.MediaPassword = password;
			}

			amc.Play();
		}

		public ulong MediaDuration
		{
			get
			{
				if (duration > 0)
				{
					return duration;
				}
				else
				{
					return amc.Duration64;
				}
			}

			set
			{
				duration = value;
			}
		}

		public uint BackStepLengthMilliSec { get; set; }

		private void SetState(State state)
		{
			this.state = state;

			switch (state)
			{
				case State.Stopped:
					playButton.Enabled = true;
					pauseButton.Enabled = true;
					stopButton.Enabled = false;
					trackBar1.Value = 0;
					progressBar1.Value = 0;
					break;
				case State.Paused:
					playButton.Enabled = true;
					pauseButton.Enabled = false;
					stopButton.Enabled = true;
					break;
				case State.Playing:
					playButton.Enabled = false;
					pauseButton.Enabled = true;
					stopButton.Enabled = true;
					break;
			}
		}

		private void UpdateMediaPositionFromSlider(bool forceSeek)
		{
			ulong scale = (ulong)(trackBar1.Maximum - trackBar1.Minimum);
			ulong newPosMilliSec = ((ulong)trackBar1.Value * MediaDuration) / scale;
			SetMediaPosition(newPosMilliSec, forceSeek);
		}

		private void SetMediaPosition(ulong newPosMilliSec, bool forceSeek)
		{
			if (MediaDuration <= 0)
			{
				return;
			}

			if (compatibilityMode)
			{
				// To change the current media position in compatibility mode we first stop AMC,
				// then set the new position and start streaming again by calling the Play method
				// Note that the Stop/Play methods are asynchronous.
				if (!isSeeking || forceSeek)
				{
					if (newPosMilliSec >= MediaDuration && MediaDuration > 0)
					{
						newPosMilliSec = MediaDuration - 1;
					}

					if (state == State.Stopped)
					{
						amc.CurrentPosition64 = newPosMilliSec;
						return;
					}

					isSeeking = true; // Performing an asynchronous seeking operation

					amc.Stop();
					amc.CurrentPosition64 = newPosMilliSec;
					if (state == State.Paused)
					{
						amc.TogglePause();
					}
					else
					{
						amc.Play();
					}

					// The asynchronous seeking operation will be complete when the status
					// of AMC is playing again, see amc_OnStatusChange.
				}
			}
			else
			{
				amc.CurrentPosition64 = newPosMilliSec;
			}
		}

		private void playButton_Click(object sender, EventArgs e)
		{
			amc.Play();
		}

		private void pauseButton_Click(object sender, EventArgs e)
		{
			amc.TogglePause();
		}

		private void stopButton_Click(object sender, EventArgs e)
		{
			isSeeking = false;
			amc.Stop();
		}

		private void amc_OnStatusChange(object sender, AxAXISMEDIACONTROLLib._IAxisMediaControlEvents_OnStatusChangeEvent e)
		{
			if (isSeeking) // compatibility mode only
			{
				// The seeking operation will be complete when the status change from opening to playing.
				if ((e.theNewStatus & (int)AMC_STATUS.AMC_STATUS_PLAYING) > 0 && // is playing
					(e.theNewStatus & (int)AMC_STATUS.AMC_STATUS_OPENING) == 0 && // is not opening
					(e.theOldStatus & (int)AMC_STATUS.AMC_STATUS_OPENING) > 0) // was opening 
				{
					isSeeking = false; // seeking complete
				}
			}
			else
			{
				if ((e.theNewStatus & (int)AMC_STATUS.AMC_STATUS_PAUSED) > 0)
				{
					SetState(State.Paused);
				}
				else if ((e.theNewStatus & (int)AMC_STATUS.AMC_STATUS_PLAYING) > 0)
				{
					SetState(State.Playing);
				}
				else
				{
					SetState(State.Stopped);
					playbackRateControl.Value = 1.0M;
				}
			}
		}

		private void amc_OnNewImage(object sender, EventArgs e)
		{
			// Update progress bar
			if (MediaDuration > 0)
			{
				ulong scale = (ulong)(progressBar1.Maximum - progressBar1.Minimum);
				if (amc.CurrentPosition64 > MediaDuration)
				{
					progressBar1.Value = progressBar1.Maximum;
				}
				else
				{
					progressBar1.Value = (int)(((amc.CurrentPosition64 * scale) / MediaDuration));
				}
			}
			else
			{
				progressBar1.Value = 0;
			}

			// Update time text box
			TimeSpan currentTime = new TimeSpan((long)amc.CurrentPosition64 * 10000); // ms -> 100-nanosecond
			TimeSpan currentDuration = new TimeSpan((long)MediaDuration * 10000); // ms -> 100-nanosecond

			string timeFormat = (currentTime.Days > 0 || currentDuration.Days > 0) ?
				"({6}) {0:D2}:{1:D2}:{2:D2} / ({7}) {3:D2}:{4:D2}:{5:D2}" :
				"{0:D2}:{1:D2}:{2:D2} / {3:D2}:{4:D2}:{5:D2}";

			string timeinfo = String.Format(timeFormat,
				currentTime.Hours, currentTime.Minutes, currentTime.Seconds,
				currentDuration.Hours, currentDuration.Minutes, currentDuration.Seconds,
				currentTime.Days, currentDuration.Days);

			currentTimeTextBox.Text = timeinfo;
		}

		private void trackBar1_ValueChanged(object sender, EventArgs e)
		{
			// remove this line to make seek only when slider nob is released
			UpdateMediaPositionFromSlider(false);
		}

		private void trackBar1_MouseUp(object sender, MouseEventArgs e)
		{
			// only needed in compatibility mode
			if (compatibilityMode)
			{
				UpdateMediaPositionFromSlider(true);
			}
		}

		private void trackBar1_KeyUp(object sender, KeyEventArgs e)
		{
			// only needed in compatibility mode
			if (compatibilityMode)
			{
				UpdateMediaPositionFromSlider(true);
			}
		}

		private void backStepButton_Click(object sender, EventArgs e)
		{
			// Accurate backward frame stepping is not supported but we could step back
			// to the previous sync-point
			if ((amc.Status & (int)AMC_STATUS.AMC_STATUS_PAUSED) == 0) // not paused
			{
				amc.TogglePause();
			}

			ulong newPosition = amc.CurrentPosition64;
			if (newPosition > BackStepLengthMilliSec)
			{
				newPosition -= BackStepLengthMilliSec;
			}
			else
			{
				newPosition = 0;
			}
			SetMediaPosition(newPosition, true);
		}

		private void fwdStepButton_Click(object sender, EventArgs e)
		{
			amc.FrameStep(1);
		}

		private void playbackRateControl_ValueChanged(object sender, EventArgs e)
		{
			// Enforce logarithmic scale
			decimal newRate;
			if (playbackRate < playbackRateControl.Value && playbackRateControl.Value <= 2 * playbackRate)
			{
				newRate = playbackRate * 2;
			}
			else if (playbackRate / 2 <= playbackRateControl.Value && playbackRateControl.Value < playbackRate)
			{
				newRate = playbackRate / 2;
			}
			else if (playbackRate != playbackRateControl.Value)
			{
				// user entered value manual - stick to power-two values
				int test = (int)Math.Log((double)playbackRateControl.Value, 2);
				newRate = (decimal)Math.Pow(2.0, (double)test);
			}
			else
			{
				return;
			}

			if (newRate < playbackRateControl.Minimum)
			{
				newRate = playbackRateControl.Minimum;
			}

			if (newRate > playbackRateControl.Maximum)
			{
				newRate = playbackRateControl.Maximum;
			}

			try
			{
				amc.PlaybackRate = (double)newRate;
			}
			catch	
			{
				// rate is only supported with newer FW
			}

			playbackRate = (decimal)amc.PlaybackRate;
			playbackRateControl.Value = playbackRate;
			playbackRateControl.DecimalPlaces = (playbackRateControl.Value < 1) ? 2 : 0;
		}
	}
}
