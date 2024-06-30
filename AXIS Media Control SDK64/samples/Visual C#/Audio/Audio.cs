using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using AXISMEDIACONTROLLib;

namespace Audio
{
	/// <summary>
	/// Receive, transmit, record and playback audio example
	/// </summary>
	public class Audio : System.Windows.Forms.Form
	{
		private System.Windows.Forms.CheckBox chkbReceived;
		private System.Windows.Forms.CheckBox chkbTransmitted;
		private System.Windows.Forms.Button btnStartRecordMedia;
		private System.Windows.Forms.Button btnStartTransmitMedia;
		private System.Windows.Forms.RadioButton rdbReceiveOn;
		private System.Windows.Forms.RadioButton rdbReceiveOff;
		private System.Windows.Forms.RadioButton rdbTransmitOff;
		private System.Windows.Forms.RadioButton rdbTransmitOn;
		private System.Windows.Forms.GroupBox grpbTransmitFile;
		private System.Windows.Forms.GroupBox grpbReceive;
		private System.Windows.Forms.GroupBox grpbTransmit;
		private AxAXISMEDIACONTROLLib.AxAxisMediaControl AMC;
		private System.Windows.Forms.Panel panelLower;
		private System.Windows.Forms.Panel panelUpper;
		private System.Windows.Forms.GroupBox grpbRecord;
		private System.Windows.Forms.TextBox ipText;
		private System.Windows.Forms.Button connectButton;
		private System.Windows.Forms.GroupBox grpbPlay;
		private System.Windows.Forms.Button btnPlay;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public Audio()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Audio());
		}

		private void rdbReceiveOff_CheckedChanged(object sender, System.EventArgs e)
		{

		}

		private void btnStartRecordMedia_Click(object sender, System.EventArgs e)
		{
			if (btnStartRecordMedia.Text.Equals("Start"))
			{
				int theFlags = (int)AMC_RECORD_FLAG.AMC_RECORD_FLAG_NONE;

				if(chkbReceived.Checked)
				{
					// Recording is only supported in 8 kHz
					MessageBox.Show("Make sure the Axis device is configured to send audio in 8 kHz.");
					theFlags |= (int)AMC_RECORD_FLAG.AMC_RECORD_FLAG_RECEIVED_AUDIO;
				}
				
				if(chkbTransmitted.Checked)
				{
					theFlags |= (int)AMC_RECORD_FLAG.AMC_RECORD_FLAG_TRANSMITTED_AUDIO;
				}

				// Present a dialog to select where to save the recording.
				SaveFileDialog mySaveDlg = new SaveFileDialog();
				mySaveDlg.Filter = "wav files (*.wav)|*.wav|All files (*.*)|*.*";

				if (mySaveDlg.ShowDialog() == DialogResult.OK)
				{
					btnStartRecordMedia.Text = "Stop";
					panelLower.Enabled = false;
					panelUpper.Enabled = false;

					try
					{
						// Stores one or more media streams to a file.
						AMC.StartRecordMedia(mySaveDlg.FileName, theFlags, "");
					}
					catch (ArgumentException ArgEx)
					{
						MessageBox.Show(ArgEx.Message, "Error");
					}
				}
			}
			else
			{
				panelLower.Enabled = true;
				panelUpper.Enabled = true;
				btnStartRecordMedia.Text = "Start";

				// Ends the ongoing recording and closes the file used for storing the media streams.
				AMC.StopRecordMedia();
			}
		}

		private void chkbTransmitted_CheckedChanged(object sender, System.EventArgs e)
		{
			btnStartRecordMedia.Enabled = chkbReceived.Checked | chkbTransmitted.Checked;
		}

		private void rdbTransmitOn_CheckedChanged(object sender, System.EventArgs e)
		{
			// Start/Stop transmitted media
			if (rdbTransmitOn.Checked == true)
			{
				// URL for transmitting audio to the server. 
				AMC.AudioTransmitURL = "http://" + ipText.Text + "/axis-cgi/audio/transmit.cgi";

				// Start stream
				AMC.AudioTransmitStart();
			}
			else
			{
				// Stop stream
				AMC.AudioTransmitStop();
			}

			chkbTransmitted.Enabled = rdbTransmitOn.Checked;
			chkbTransmitted.Checked = rdbTransmitOn.Checked;
		}

		private void chkbReceivedTransmitted_CheckedChanged(object sender, System.EventArgs e)
		{
			btnStartRecordMedia.Enabled = chkbReceived.Checked | chkbTransmitted.Checked;
		}

		private void rdbReceiveOn_CheckedChanged(object sender, System.EventArgs e)
		{
			// Start/Stop received media
			if (rdbReceiveOn.Checked == true)
			{
				// The complete URL to an audio stream from an Axis video product. 
				AMC.AudioReceiveURL = "http://" + ipText.Text + "/axis-cgi/audio/receive.cgi";

				// Start stream
				AMC.AudioReceiveStart();
			}
			else
			{
				// Stop stream
				AMC.AudioReceiveStop();
			}

			chkbReceived.Enabled = rdbReceiveOn.Checked;
			chkbReceived.Checked = rdbReceiveOn.Checked;
		}

		private void btnStartTransmitMedia_Click(object sender, System.EventArgs e)
		{
			if (btnStartTransmitMedia.Text.Equals("Start"))
			{
				// Present a dialog to select the file to transmit.
				OpenFileDialog myOpenDlg = new OpenFileDialog();
				myOpenDlg.Title = "Select 8 kHz audio file"; 
				myOpenDlg.Filter = "wav files (*.wav)|*.wav|All files (*.*)|*.*";

				if (myOpenDlg.ShowDialog() == DialogResult.OK)
				{
					btnStartTransmitMedia.Text = "Stop";
					grpbReceive.Enabled = false;
					grpbTransmit.Enabled = false;
					grpbRecord.Enabled = false;

					try
					{
						// URL for transmitting audio to the server.
						AMC.AudioTransmitURL = "http://" + ipText.Text + "/axis-cgi/audio/transmit.cgi";

						// Starts to transmit the file to the server.
						AMC.StartTransmitMedia(myOpenDlg.FileName, 0);
					}
					catch (ArgumentException ArgEx)
					{
						MessageBox.Show(ArgEx.Message, "Error");
					}
				}
			}
			else
			{
				btnStartTransmitMedia.Text = "Start";
				grpbReceive.Enabled = true;
				grpbTransmit.Enabled = true;
				grpbRecord.Enabled = true;

				// Ends the ongoing file-transmission.
				AMC.StopTransmitMedia();
			}
		}

		private void connectButton_Click(object sender, System.EventArgs e)
		{
			// This URL is used to retrieve audio configuration from an Axis device with audio capability.
			AMC.AudioConfigURL = "http://" + ipText.Text + "/axis-cgi/view/param.cgi" + 
				"?usergroup=anonymous&action=list&group=Audio,AudioSource";


			// To transmit audio in 16 kHz use this configuration URL instead (recording will not work)
			//AMC.AudioConfigURL = "http://" + ipText.Text + "/axis-cgi/view/param.cgi" +
			//  "?usergroup=anonymous&action=list&group=Audio,AudioSource,Properties.Audio";

			// Mute/unmute received and transmitted media
			if (rdbReceiveOn.Checked == true)
				AMC.AudioReceiveStart();
			else
				AMC.AudioReceiveStop();
			if (rdbTransmitOn.Checked == true)
				AMC.AudioTransmitStart();
			else
				AMC.AudioTransmitStop();

			grpbReceive.Enabled = true;
			grpbRecord.Enabled = true;
			grpbTransmit.Enabled = true;
			grpbTransmitFile.Enabled = true;

			// Handle errors in AMC_OnError event.
		}

		private void AMC_OnError(object sender, AxAXISMEDIACONTROLLib._IAxisMediaControlEvents_OnErrorEvent e)
		{
				MessageBox.Show(e.theErrorInfo, "Error");
		}

		private void btnPlay_Click(object sender, System.EventArgs e)
		{
			if (btnPlay.Text.Equals("Start"))
			{
				// Present a dialog to select the file to play.
				OpenFileDialog myOpenDlg = new OpenFileDialog();
				myOpenDlg.Filter = "wav files (*.wav)|*.wav|All files (*.*)|*.*";

				if (myOpenDlg.ShowDialog() == DialogResult.OK)
				{
					btnPlay.Text = "Stop";

					AMC.AudioReceiveStart();
					AMC.AudioTransmitStop();

					// Sets MediaFile and starts to play the file.
					AMC.MediaFile = myOpenDlg.FileName;
					AMC.Play();
				}
			}
			else
			{
				// Stops playing the mediafile.
				AMC.Stop();
				btnPlay.Text = "Start";

				if (rdbReceiveOn.Checked == true)
					AMC.AudioReceiveStart();

				if (rdbTransmitOn.Checked == true)
					AMC.AudioTransmitStart();
			}
		}

		private void AMC_OnStatusChange(object sender, AxAXISMEDIACONTROLLib._IAxisMediaControlEvents_OnStatusChangeEvent e)
		{
			if ((e.theOldStatus & (int)AMC_STATUS.AMC_STATUS_TRANSMIT_AUDIO_FILE) > 0 &&
				 (e.theNewStatus & (int)AMC_STATUS.AMC_STATUS_TRANSMIT_AUDIO_FILE) == 0)
			{
				// audio file transmit ended
				btnStartTransmitMedia.Text = "Start";
				grpbReceive.Enabled = true;
				grpbTransmit.Enabled = true;
				grpbRecord.Enabled = true;
			}
			if ((e.theOldStatus & (int)AMC_STATUS.AMC_STATUS_PLAYING) > 0 &&
				 (e.theNewStatus & (int)AMC_STATUS.AMC_STATUS_PLAYING) == 0)
			{
				// audio file playback ended
				btnPlay.Text = "Start";

				if (rdbReceiveOn.Checked == true)
					AMC.AudioReceiveStart();

				if (rdbTransmitOn.Checked == true)
					AMC.AudioTransmitStart();
			}
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Audio));
			this.grpbRecord = new System.Windows.Forms.GroupBox();
			this.btnStartRecordMedia = new System.Windows.Forms.Button();
			this.panelLower = new System.Windows.Forms.Panel();
			this.chkbTransmitted = new System.Windows.Forms.CheckBox();
			this.chkbReceived = new System.Windows.Forms.CheckBox();
			this.grpbTransmitFile = new System.Windows.Forms.GroupBox();
			this.btnStartTransmitMedia = new System.Windows.Forms.Button();
			this.grpbReceive = new System.Windows.Forms.GroupBox();
			this.rdbReceiveOn = new System.Windows.Forms.RadioButton();
			this.rdbReceiveOff = new System.Windows.Forms.RadioButton();
			this.grpbTransmit = new System.Windows.Forms.GroupBox();
			this.rdbTransmitOff = new System.Windows.Forms.RadioButton();
			this.rdbTransmitOn = new System.Windows.Forms.RadioButton();
			this.panelUpper = new System.Windows.Forms.Panel();
			this.AMC = new AxAXISMEDIACONTROLLib.AxAxisMediaControl();
			this.ipText = new System.Windows.Forms.TextBox();
			this.connectButton = new System.Windows.Forms.Button();
			this.grpbPlay = new System.Windows.Forms.GroupBox();
			this.btnPlay = new System.Windows.Forms.Button();
			this.grpbRecord.SuspendLayout();
			this.panelLower.SuspendLayout();
			this.grpbTransmitFile.SuspendLayout();
			this.grpbReceive.SuspendLayout();
			this.grpbTransmit.SuspendLayout();
			this.panelUpper.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.AMC)).BeginInit();
			this.grpbPlay.SuspendLayout();
			this.SuspendLayout();
			// 
			// grpbRecord
			// 
			this.grpbRecord.Controls.Add(this.btnStartRecordMedia);
			this.grpbRecord.Controls.Add(this.panelLower);
			this.grpbRecord.Enabled = false;
			this.grpbRecord.Location = new System.Drawing.Point(88, 104);
			this.grpbRecord.Name = "grpbRecord";
			this.grpbRecord.Size = new System.Drawing.Size(160, 64);
			this.grpbRecord.TabIndex = 23;
			this.grpbRecord.TabStop = false;
			this.grpbRecord.Text = "Record";
			// 
			// btnStartRecordMedia
			// 
			this.btnStartRecordMedia.Enabled = false;
			this.btnStartRecordMedia.Location = new System.Drawing.Point(104, 24);
			this.btnStartRecordMedia.Name = "btnStartRecordMedia";
			this.btnStartRecordMedia.Size = new System.Drawing.Size(48, 24);
			this.btnStartRecordMedia.TabIndex = 3;
			this.btnStartRecordMedia.Text = "Start";
			this.btnStartRecordMedia.Click += new System.EventHandler(this.btnStartRecordMedia_Click);
			// 
			// panelLower
			// 
			this.panelLower.Controls.Add(this.chkbTransmitted);
			this.panelLower.Controls.Add(this.chkbReceived);
			this.panelLower.Location = new System.Drawing.Point(8, 16);
			this.panelLower.Name = "panelLower";
			this.panelLower.Size = new System.Drawing.Size(88, 40);
			this.panelLower.TabIndex = 26;
			// 
			// chkbTransmitted
			// 
			this.chkbTransmitted.Enabled = false;
			this.chkbTransmitted.Location = new System.Drawing.Point(0, 24);
			this.chkbTransmitted.Name = "chkbTransmitted";
			this.chkbTransmitted.Size = new System.Drawing.Size(88, 16);
			this.chkbTransmitted.TabIndex = 14;
			this.chkbTransmitted.Text = "Transmitted";
			this.chkbTransmitted.CheckedChanged += new System.EventHandler(this.chkbReceivedTransmitted_CheckedChanged);
			// 
			// chkbReceived
			// 
			this.chkbReceived.Enabled = false;
			this.chkbReceived.Location = new System.Drawing.Point(0, 0);
			this.chkbReceived.Name = "chkbReceived";
			this.chkbReceived.Size = new System.Drawing.Size(88, 24);
			this.chkbReceived.TabIndex = 13;
			this.chkbReceived.Text = "Received";
			this.chkbReceived.CheckedChanged += new System.EventHandler(this.chkbReceivedTransmitted_CheckedChanged);
			// 
			// grpbTransmitFile
			// 
			this.grpbTransmitFile.Controls.Add(this.btnStartTransmitMedia);
			this.grpbTransmitFile.Enabled = false;
			this.grpbTransmitFile.Location = new System.Drawing.Point(152, 0);
			this.grpbTransmitFile.Name = "grpbTransmitFile";
			this.grpbTransmitFile.Size = new System.Drawing.Size(88, 56);
			this.grpbTransmitFile.TabIndex = 22;
			this.grpbTransmitFile.TabStop = false;
			this.grpbTransmitFile.Text = "Transmit file";
			// 
			// btnStartTransmitMedia
			// 
			this.btnStartTransmitMedia.Location = new System.Drawing.Point(16, 24);
			this.btnStartTransmitMedia.Name = "btnStartTransmitMedia";
			this.btnStartTransmitMedia.Size = new System.Drawing.Size(56, 24);
			this.btnStartTransmitMedia.TabIndex = 2;
			this.btnStartTransmitMedia.Text = "Start";
			this.btnStartTransmitMedia.Click += new System.EventHandler(this.btnStartTransmitMedia_Click);
			// 
			// grpbReceive
			// 
			this.grpbReceive.Controls.Add(this.rdbReceiveOn);
			this.grpbReceive.Controls.Add(this.rdbReceiveOff);
			this.grpbReceive.Enabled = false;
			this.grpbReceive.Location = new System.Drawing.Point(8, 0);
			this.grpbReceive.Name = "grpbReceive";
			this.grpbReceive.Size = new System.Drawing.Size(64, 56);
			this.grpbReceive.TabIndex = 21;
			this.grpbReceive.TabStop = false;
			this.grpbReceive.Text = "Receive";
			// 
			// rdbReceiveOn
			// 
			this.rdbReceiveOn.Location = new System.Drawing.Point(16, 16);
			this.rdbReceiveOn.Name = "rdbReceiveOn";
			this.rdbReceiveOn.Size = new System.Drawing.Size(40, 16);
			this.rdbReceiveOn.TabIndex = 18;
			this.rdbReceiveOn.Text = "On";
			this.rdbReceiveOn.CheckedChanged += new System.EventHandler(this.rdbReceiveOn_CheckedChanged);
			// 
			// rdbReceiveOff
			// 
			this.rdbReceiveOff.Checked = true;
			this.rdbReceiveOff.Location = new System.Drawing.Point(16, 32);
			this.rdbReceiveOff.Name = "rdbReceiveOff";
			this.rdbReceiveOff.Size = new System.Drawing.Size(40, 16);
			this.rdbReceiveOff.TabIndex = 19;
			this.rdbReceiveOff.TabStop = true;
			this.rdbReceiveOff.Text = "Off";
			this.rdbReceiveOff.CheckedChanged += new System.EventHandler(this.rdbReceiveOff_CheckedChanged);
			// 
			// grpbTransmit
			// 
			this.grpbTransmit.Controls.Add(this.rdbTransmitOff);
			this.grpbTransmit.Controls.Add(this.rdbTransmitOn);
			this.grpbTransmit.Enabled = false;
			this.grpbTransmit.Location = new System.Drawing.Point(80, 0);
			this.grpbTransmit.Name = "grpbTransmit";
			this.grpbTransmit.Size = new System.Drawing.Size(64, 56);
			this.grpbTransmit.TabIndex = 24;
			this.grpbTransmit.TabStop = false;
			this.grpbTransmit.Text = "Transmit";
			// 
			// rdbTransmitOff
			// 
			this.rdbTransmitOff.Checked = true;
			this.rdbTransmitOff.Location = new System.Drawing.Point(8, 32);
			this.rdbTransmitOff.Name = "rdbTransmitOff";
			this.rdbTransmitOff.Size = new System.Drawing.Size(40, 16);
			this.rdbTransmitOff.TabIndex = 21;
			this.rdbTransmitOff.TabStop = true;
			this.rdbTransmitOff.Text = "Off";
			// 
			// rdbTransmitOn
			// 
			this.rdbTransmitOn.Location = new System.Drawing.Point(8, 16);
			this.rdbTransmitOn.Name = "rdbTransmitOn";
			this.rdbTransmitOn.Size = new System.Drawing.Size(40, 16);
			this.rdbTransmitOn.TabIndex = 20;
			this.rdbTransmitOn.Text = "On";
			this.rdbTransmitOn.CheckedChanged += new System.EventHandler(this.rdbTransmitOn_CheckedChanged);
			// 
			// panelUpper
			// 
			this.panelUpper.Controls.Add(this.grpbTransmitFile);
			this.panelUpper.Controls.Add(this.grpbReceive);
			this.panelUpper.Controls.Add(this.grpbTransmit);
			this.panelUpper.Location = new System.Drawing.Point(8, 40);
			this.panelUpper.Name = "panelUpper";
			this.panelUpper.Size = new System.Drawing.Size(248, 56);
			this.panelUpper.TabIndex = 25;
			// 
			// AMC
			// 
			this.AMC.Enabled = true;
			this.AMC.Location = new System.Drawing.Point(256, 96);
			this.AMC.Name = "AMC";
			this.AMC.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("AMC.OcxState")));
			this.AMC.Size = new System.Drawing.Size(40, 48);
			this.AMC.TabIndex = 26;
			this.AMC.Visible = false;
			this.AMC.OnError += new AxAXISMEDIACONTROLLib._IAxisMediaControlEvents_OnErrorEventHandler(this.AMC_OnError);
			this.AMC.OnStatusChange += new AxAXISMEDIACONTROLLib._IAxisMediaControlEvents_OnStatusChangeEventHandler(this.AMC_OnStatusChange);
			// 
			// ipText
			// 
			this.ipText.Location = new System.Drawing.Point(16, 8);
			this.ipText.Name = "ipText";
			this.ipText.Size = new System.Drawing.Size(144, 20);
			this.ipText.TabIndex = 27;
			this.ipText.Text = "0.0.0.0";
			// 
			// connectButton
			// 
			this.connectButton.Location = new System.Drawing.Point(176, 8);
			this.connectButton.Name = "connectButton";
			this.connectButton.Size = new System.Drawing.Size(72, 23);
			this.connectButton.TabIndex = 28;
			this.connectButton.Text = "Connect";
			this.connectButton.Click += new System.EventHandler(this.connectButton_Click);
			// 
			// grpbPlay
			// 
			this.grpbPlay.Controls.Add(this.btnPlay);
			this.grpbPlay.Location = new System.Drawing.Point(16, 104);
			this.grpbPlay.Name = "grpbPlay";
			this.grpbPlay.Size = new System.Drawing.Size(64, 64);
			this.grpbPlay.TabIndex = 29;
			this.grpbPlay.TabStop = false;
			this.grpbPlay.Text = "Play file";
			// 
			// btnPlay
			// 
			this.btnPlay.Location = new System.Drawing.Point(8, 24);
			this.btnPlay.Name = "btnPlay";
			this.btnPlay.Size = new System.Drawing.Size(48, 24);
			this.btnPlay.TabIndex = 2;
			this.btnPlay.Text = "Start";
			this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
			// 
			// Form1
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(264, 174);
			this.Controls.Add(this.grpbPlay);
			this.Controls.Add(this.connectButton);
			this.Controls.Add(this.ipText);
			this.Controls.Add(this.AMC);
			this.Controls.Add(this.panelUpper);
			this.Controls.Add(this.grpbRecord);
			this.Name = "Form1";
			this.Text = "Audio";
			this.grpbRecord.ResumeLayout(false);
			this.panelLower.ResumeLayout(false);
			this.grpbTransmitFile.ResumeLayout(false);
			this.grpbReceive.ResumeLayout(false);
			this.grpbTransmit.ResumeLayout(false);
			this.panelUpper.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.AMC)).EndInit();
			this.grpbPlay.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

	}
}
