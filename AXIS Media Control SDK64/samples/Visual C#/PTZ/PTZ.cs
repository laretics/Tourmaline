using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using AXISMEDIACONTROLLib;

namespace PTZ
{
	/// <summary>
	/// Move the camera by clicking example
	/// </summary>
	public class PTZ : System.Windows.Forms.Form
	{
		private AxAXISMEDIACONTROLLib.AxAxisMediaControl AMC;
		internal System.Windows.Forms.TextBox ipText;
		internal System.Windows.Forms.Button btnConnect;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public PTZ()
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
			Application.Run(new PTZ());
		}

		private void btnConnect_Click(object sender, EventArgs e)
		{
			try
			{
				//Stops possible streams
				AMC.Stop();

				// Set the PTZ properties
				AMC.PTZControlURL = "http://" + ipText.Text + "/axis-cgi/com/ptz.cgi";
				AMC.UIMode = "ptz-absolute";

				// Enable PTZ-position presets from AMC context menu
				AMC.PTZPresetURL = "http://" + ipText.Text +
					"/axis-cgi/param.cgi?usergroup=anonymous&action=list&group=PTZ.Preset.P0";
				// Firmware version 4
				//AMC.PTZPresetURL = "http://" + ipText.Text + 
				//	"/axis-cgi/view/param.cgi?action=list&group=PTZ.Preset.P0";

				// Enable joystick support
				AMC.EnableJoystick = true;

				// Enable area zoom
				AMC.EnableAreaZoom = true;

				// Enable one-click-zoom
				//AMC.OneClickZoom = true;

				// Set overlay settings
				AMC.EnableOverlays = true;
				AMC.ClientOverlay = (int)AMC_OVERLAY.AMC_OVERLAY_CROSSHAIR |
														(int)AMC_OVERLAY.AMC_OVERLAY_VECTOR |
														(int)AMC_OVERLAY.AMC_OVERLAY_ZOOM;


				// Show the status bar and the tool bar in the AXIS Media Control
				AMC.ShowStatusBar = true;
				AMC.ShowToolbar = true;
				AMC.StretchToFit = true;
				AMC.EnableContextMenu = true;
				AMC.ToolbarConfiguration = "default,-mute,-volume,+ptz";

				// Set the media URL
				AMC.MediaURL = "http://" + ipText.Text + "/axis-cgi/mjpg/video.cgi";

				// Start the download of the mjpeg stream from the Axis camera/video server
				AMC.Play();
			}
			catch (ArgumentException ArgEx)
			{
				MessageBox.Show(ArgEx.Message, "Error");
			}
			catch (Exception Ex)
			{
				MessageBox.Show(Ex.Message, "Error");
			}
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PTZ));
			this.AMC = new AxAXISMEDIACONTROLLib.AxAxisMediaControl();
			this.ipText = new System.Windows.Forms.TextBox();
			this.btnConnect = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.AMC)).BeginInit();
			this.SuspendLayout();
			// 
			// AMC
			// 
			this.AMC.Enabled = true;
			this.AMC.Location = new System.Drawing.Point(8, 8);
			this.AMC.Name = "AMC";
			this.AMC.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("AMC.OcxState")));
			this.AMC.Size = new System.Drawing.Size(552, 424);
			this.AMC.TabIndex = 0;
			// 
			// ipText
			// 
			this.ipText.Location = new System.Drawing.Point(9, 444);
			this.ipText.Name = "ipText";
			this.ipText.Size = new System.Drawing.Size(131, 20);
			this.ipText.TabIndex = 1;
			this.ipText.Text = "0.0.0.0";
			// 
			// btnConnect
			// 
			this.btnConnect.Location = new System.Drawing.Point(146, 444);
			this.btnConnect.Name = "btnConnect";
			this.btnConnect.Size = new System.Drawing.Size(64, 20);
			this.btnConnect.TabIndex = 2;
			this.btnConnect.Text = "Connect";
			this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
			// 
			// PTZ
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(568, 476);
			this.Controls.Add(this.btnConnect);
			this.Controls.Add(this.ipText);
			this.Controls.Add(this.AMC);
			this.Name = "PTZ";
			this.Text = "PTZ";
			((System.ComponentModel.ISupportInitialize)(this.AMC)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
