using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using AXISMEDIACONTROLLib;

namespace Video
{

	enum MediaType
	{
		mjpeg,
		h264,
		h265,
		mpeg4
	}

	/// <summary>
	/// View, record and playback using AXIS Media Control example
	/// </summary>
	public class VideoForm : System.Windows.Forms.Form
	{
		private AxAXISMEDIACONTROLLib.AxAxisMediaControl amc;
		private System.Windows.Forms.Button myPlayButton;
		private System.Windows.Forms.Button myPlayFileButton;
		private System.Windows.Forms.TextBox myUrlBox;
		private System.Windows.Forms.ComboBox myTypeBox;
		private System.Windows.Forms.Button myStopButton;
		private System.Windows.Forms.Button myRecordButton;
		private System.Windows.Forms.TextBox myFileBox;
		private System.Windows.Forms.Button myFileDialogButton;
		private System.Windows.Forms.TextBox myPassBox;
		private System.Windows.Forms.TextBox myUserBox;
		private System.Windows.Forms.Label userLabel;
		private System.Windows.Forms.Label passLabel;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public VideoForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			// We set AMC properties here for clarity
			amc.StretchToFit = true;
			amc.MaintainAspectRatio = true;
			amc.ShowStatusBar = true;
			amc.BackgroundColor = 0; // black
			amc.VideoRenderer = (int)AMC_VIDEO_RENDERER.AMC_VIDEO_RENDERER_EVR;
			amc.EnableOverlays = true;

			// Configure context menu
			amc.EnableContextMenu = true;
			amc.ToolbarConfiguration = "+play,+fullscreen,-settings"; //"-pixcount" to remove pixel counter

			// AMC messaging setting
			amc.Popups = 0;
			amc.Popups |= (int)AMC_POPUPS.AMC_POPUPS_LOGIN_DIALOG; // Allow login dialog
			amc.Popups |= (int)AMC_POPUPS.AMC_POPUPS_NO_VIDEO; // "No Video" message when stopped
			//amc.Popups |= (int)AMC_POPUPS.AMC_POPUPS_MESSAGES; // Yellow-balloon notification

			amc.UIMode = "digital-zoom";

			this.myTypeBox.Items.AddRange(new object[] 
				{ MediaType.h264, MediaType.h265, MediaType.mpeg4, MediaType.mjpeg });
			this.myTypeBox.SelectedIndex = 0;
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
			Application.Run(new VideoForm());
		}

		private void myPlayButton_Click(object sender, System.EventArgs e)
		{
			//Stop possible streams
			amc.Stop();

			// Set properties, deciding what url completion to use by MediaType.
			amc.MediaUsername = myUserBox.Text;
			amc.MediaPassword = myPassBox.Text;
			amc.MediaURL = CompleteURL(myUrlBox.Text, (MediaType)myTypeBox.SelectedItem);

			// Start the streaming
			amc.Play();

			// check for stream errors in OnError event
		}

		private void myPlayFileButton_Click(object sender, System.EventArgs e)
		{
			// Set the MediaFile property to the selected file.
			FileDialog aDialog = new OpenFileDialog();
			if (aDialog.ShowDialog(this) == DialogResult.OK)
			{
				//Stop possible streams
				amc.Stop();

				amc.MediaFile = aDialog.FileName;
				amc.Play();
			}
		}

		private void myStopButton_Click(object sender, System.EventArgs e)
		{
			// Stop the stream (will also stop any recording in progress).
			amc.Stop();
		}

		private void myRecordButton_Click(object sender, System.EventArgs e)
		{
			if ((amc.Status & (int)AMC_STATUS.AMC_STATUS_RECORDING) > 0)
			{
				amc.StopRecordMedia();
			}
			else
			{
				// Start the recording (video and audio)
				int recordingFlag = (int)AMC_RECORD_FLAG.AMC_RECORD_FLAG_AUDIO_VIDEO;
				if (MediaType.mjpeg == (MediaType)myTypeBox.SelectedItem)
				{
					// Audio recording is not supported for Motion JPEG over HTTP
					recordingFlag = (int)AMC_RECORD_FLAG.AMC_RECORD_FLAG_VIDEO;
				}

				amc.StartRecordMedia(myFileBox.Text, recordingFlag, "");
			}
		}

		private void myFileDialogButton_Click(object sender, System.EventArgs e)
		{
			FileDialog aDialog = new SaveFileDialog();
			if (aDialog.ShowDialog(this) == DialogResult.OK)
			{
				myFileBox.Text = aDialog.FileName;
			}
		}

		private string CompleteURL(string theMediaURL, MediaType theMediaType)
		{
			string anURL = theMediaURL;
			if (!anURL.EndsWith("/")) anURL += "/";

			switch (theMediaType)
			{
				case MediaType.mjpeg:
					anURL += "axis-cgi/mjpg/video.cgi";
					break;
				case MediaType.mpeg4:
					anURL += "mpeg4/media.amp";
					break;
				case MediaType.h264:
					anURL += "axis-media/media.amp?videocodec=h264";
					break;
				case MediaType.h265:
					anURL += "axis-media/media.amp?videocodec=h265";
					break;
			}

			anURL = CompleteProtocol(anURL, theMediaType);
			return anURL;
		}

		private string CompleteProtocol(string theMediaURL, MediaType theMediaType)
		{
			if (theMediaURL.IndexOf("://") >= 0) return theMediaURL;

			string anURL = theMediaURL;

			switch (theMediaType)
			{
				case MediaType.mjpeg:
					// This example streams Motion JPEG over HTTP multipart (only video)
					// See docs on how to receive unsynchronized audio with Motion JPEG
					anURL = "http://" + anURL;
					break;
				case MediaType.mpeg4:
				case MediaType.h264:
				case MediaType.h265:
					// Use RTP over RTSP over HTTP (for other transport mechanisms see docs)
					anURL = "axrtsphttp://" + anURL;
					break;
			}

			return anURL;
		}

		void amc_OnNewVideoSize(object sender, AxAXISMEDIACONTROLLib._IAxisMediaControlEvents_OnNewVideoSizeEvent e)
		{
			if (e.theWidth >= 320 && e.theHeight >= 240)
			{
				// Adapt window size to video size
				Width += e.theWidth - amc.Width;
				Height += e.theHeight - amc.Height;

				if (amc.ShowStatusBar)
				{
					Height += 22;
				}

				if (amc.ShowToolbar)
				{
					Height += 32;
				}
			}
		}

		void amc_OnStatusChange(object sender, AxAXISMEDIACONTROLLib._IAxisMediaControlEvents_OnStatusChangeEvent e)
		{
			if ((e.theOldStatus & (int)AMC_STATUS.AMC_STATUS_RECORDING) == 0 && // was not recording
					(e.theNewStatus & (int)AMC_STATUS.AMC_STATUS_RECORDING) > 0) // is recording
			{
				myRecordButton.Text = "Stop Recording";
			}
			else
			{
				myRecordButton.Text = "Record";
			}
		}

		private void amc_OnError(object sender, AxAXISMEDIACONTROLLib._IAxisMediaControlEvents_OnErrorEvent e)
		{
			System.Windows.Forms.MessageBox.Show(this, e.theErrorInfo, "Error code " + e.theErrorCode.ToString("X8"));
		}

		private void amc_OnMouseMove(object sender, AxAXISMEDIACONTROLLib._IAxisMediaControlEvents_OnMouseMoveEvent e)
		{
			if (amc.UIMode == "digital-zoom")
			{
				if ((amc.Status & (int)AMC_STATUS.AMC_STATUS_PLAYING) > 0)
				{
					// set focus to AMC in order to zoom using mouse wheel
					amc.Focus();
				}
			}
		}


		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VideoForm));
			this.amc = new AxAXISMEDIACONTROLLib.AxAxisMediaControl();
			this.myPlayButton = new System.Windows.Forms.Button();
			this.myPlayFileButton = new System.Windows.Forms.Button();
			this.myUrlBox = new System.Windows.Forms.TextBox();
			this.myFileBox = new System.Windows.Forms.TextBox();
			this.myTypeBox = new System.Windows.Forms.ComboBox();
			this.myStopButton = new System.Windows.Forms.Button();
			this.myRecordButton = new System.Windows.Forms.Button();
			this.myFileDialogButton = new System.Windows.Forms.Button();
			this.myPassBox = new System.Windows.Forms.TextBox();
			this.myUserBox = new System.Windows.Forms.TextBox();
			this.userLabel = new System.Windows.Forms.Label();
			this.passLabel = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.amc)).BeginInit();
			this.SuspendLayout();
			// 
			// amc
			// 
			this.amc.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.amc.Enabled = true;
			this.amc.Location = new System.Drawing.Point(8, 8);
			this.amc.Name = "amc";
			this.amc.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("amc.OcxState")));
			this.amc.Size = new System.Drawing.Size(368, 312);
			this.amc.TabIndex = 0;
			this.amc.TabStop = false;
			this.amc.OnError += new AxAXISMEDIACONTROLLib._IAxisMediaControlEvents_OnErrorEventHandler(this.amc_OnError);
			this.amc.OnMouseMove += new AxAXISMEDIACONTROLLib._IAxisMediaControlEvents_OnMouseMoveEventHandler(this.amc_OnMouseMove);
			this.amc.OnStatusChange += new AxAXISMEDIACONTROLLib._IAxisMediaControlEvents_OnStatusChangeEventHandler(this.amc_OnStatusChange);
			this.amc.OnNewVideoSize += new AxAXISMEDIACONTROLLib._IAxisMediaControlEvents_OnNewVideoSizeEventHandler(this.amc_OnNewVideoSize);
			// 
			// myPlayButton
			// 
			this.myPlayButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.myPlayButton.Location = new System.Drawing.Point(384, 128);
			this.myPlayButton.Name = "myPlayButton";
			this.myPlayButton.Size = new System.Drawing.Size(80, 23);
			this.myPlayButton.TabIndex = 5;
			this.myPlayButton.Text = "Play Live";
			this.myPlayButton.Click += new System.EventHandler(this.myPlayButton_Click);
			// 
			// myPlayFileButton
			// 
			this.myPlayFileButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.myPlayFileButton.Location = new System.Drawing.Point(472, 128);
			this.myPlayFileButton.Name = "myPlayFileButton";
			this.myPlayFileButton.Size = new System.Drawing.Size(80, 23);
			this.myPlayFileButton.TabIndex = 6;
			this.myPlayFileButton.Text = "Play File";
			this.myPlayFileButton.Click += new System.EventHandler(this.myPlayFileButton_Click);
			// 
			// myUrlBox
			// 
			this.myUrlBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.myUrlBox.Location = new System.Drawing.Point(384, 8);
			this.myUrlBox.Name = "myUrlBox";
			this.myUrlBox.Size = new System.Drawing.Size(168, 20);
			this.myUrlBox.TabIndex = 1;
			this.myUrlBox.Text = "0.0.0.0";
			// 
			// myFileBox
			// 
			this.myFileBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.myFileBox.Location = new System.Drawing.Point(384, 240);
			this.myFileBox.Name = "myFileBox";
			this.myFileBox.Size = new System.Drawing.Size(138, 20);
			this.myFileBox.TabIndex = 9;
			this.myFileBox.Text = "C:\\AMC_Recording.asf";
			// 
			// myTypeBox
			// 
			this.myTypeBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.myTypeBox.Location = new System.Drawing.Point(384, 80);
			this.myTypeBox.Name = "myTypeBox";
			this.myTypeBox.Size = new System.Drawing.Size(168, 21);
			this.myTypeBox.TabIndex = 4;
			// 
			// myStopButton
			// 
			this.myStopButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.myStopButton.Location = new System.Drawing.Point(384, 160);
			this.myStopButton.Name = "myStopButton";
			this.myStopButton.Size = new System.Drawing.Size(168, 23);
			this.myStopButton.TabIndex = 7;
			this.myStopButton.Text = "Stop";
			this.myStopButton.Click += new System.EventHandler(this.myStopButton_Click);
			// 
			// myRecordButton
			// 
			this.myRecordButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.myRecordButton.Location = new System.Drawing.Point(384, 208);
			this.myRecordButton.Name = "myRecordButton";
			this.myRecordButton.Size = new System.Drawing.Size(168, 23);
			this.myRecordButton.TabIndex = 8;
			this.myRecordButton.Text = "Record";
			this.myRecordButton.Click += new System.EventHandler(this.myRecordButton_Click);
			// 
			// myFileDialogButton
			// 
			this.myFileDialogButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.myFileDialogButton.Location = new System.Drawing.Point(528, 240);
			this.myFileDialogButton.Name = "myFileDialogButton";
			this.myFileDialogButton.Size = new System.Drawing.Size(22, 24);
			this.myFileDialogButton.TabIndex = 10;
			this.myFileDialogButton.Text = "...";
			this.myFileDialogButton.Click += new System.EventHandler(this.myFileDialogButton_Click);
			// 
			// myPassBox
			// 
			this.myPassBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.myPassBox.Location = new System.Drawing.Point(472, 56);
			this.myPassBox.Name = "myPassBox";
			this.myPassBox.PasswordChar = '*';
			this.myPassBox.Size = new System.Drawing.Size(80, 20);
			this.myPassBox.TabIndex = 3;
			// 
			// myUserBox
			// 
			this.myUserBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.myUserBox.Location = new System.Drawing.Point(472, 32);
			this.myUserBox.Name = "myUserBox";
			this.myUserBox.Size = new System.Drawing.Size(80, 20);
			this.myUserBox.TabIndex = 2;
			// 
			// userLabel
			// 
			this.userLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.userLabel.Location = new System.Drawing.Point(384, 32);
			this.userLabel.Name = "userLabel";
			this.userLabel.Size = new System.Drawing.Size(80, 20);
			this.userLabel.TabIndex = 8;
			this.userLabel.Text = "Username:";
			this.userLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// passLabel
			// 
			this.passLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.passLabel.Location = new System.Drawing.Point(384, 56);
			this.passLabel.Name = "passLabel";
			this.passLabel.Size = new System.Drawing.Size(80, 20);
			this.passLabel.TabIndex = 8;
			this.passLabel.Text = "Password:";
			this.passLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// VideoForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(560, 326);
			this.Controls.Add(this.userLabel);
			this.Controls.Add(this.myFileDialogButton);
			this.Controls.Add(this.myRecordButton);
			this.Controls.Add(this.myStopButton);
			this.Controls.Add(this.myTypeBox);
			this.Controls.Add(this.myFileBox);
			this.Controls.Add(this.myUrlBox);
			this.Controls.Add(this.myPassBox);
			this.Controls.Add(this.myUserBox);
			this.Controls.Add(this.myPlayButton);
			this.Controls.Add(this.amc);
			this.Controls.Add(this.myPlayFileButton);
			this.Controls.Add(this.passLabel);
			this.Name = "VideoForm";
			this.Text = "Video Sample";
			((System.ComponentModel.ISupportInitialize)(this.amc)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

	}
}
