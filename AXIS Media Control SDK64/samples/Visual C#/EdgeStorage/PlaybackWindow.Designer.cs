namespace EdgeStorage
{
	partial class PlaybackWindow
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PlaybackWindow));
			this.amc = new AxAXISMEDIACONTROLLib.AxAxisMediaControl();
			this.progressBar1 = new System.Windows.Forms.ProgressBar();
			this.trackBar1 = new System.Windows.Forms.TrackBar();
			this.playButton = new System.Windows.Forms.Button();
			this.pauseButton = new System.Windows.Forms.Button();
			this.stopButton = new System.Windows.Forms.Button();
			this.currentTimeTextBox = new System.Windows.Forms.TextBox();
			this.backStepButton = new System.Windows.Forms.Button();
			this.fwdStepButton = new System.Windows.Forms.Button();
			this.playbackRateLabel = new System.Windows.Forms.Label();
			this.playbackRateControl = new System.Windows.Forms.NumericUpDown();
			((System.ComponentModel.ISupportInitialize)(this.amc)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.playbackRateControl)).BeginInit();
			this.SuspendLayout();
			// 
			// amc
			// 
			this.amc.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.amc.Enabled = true;
			this.amc.Location = new System.Drawing.Point(0, 0);
			this.amc.Name = "amc";
			this.amc.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("amc.OcxState")));
			this.amc.Size = new System.Drawing.Size(800, 450);
			this.amc.TabIndex = 0;
			this.amc.OnNewImage += new System.EventHandler(this.amc_OnNewImage);
			this.amc.OnStatusChange += new AxAXISMEDIACONTROLLib._IAxisMediaControlEvents_OnStatusChangeEventHandler(this.amc_OnStatusChange);
			// 
			// progressBar1
			// 
			this.progressBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.progressBar1.Location = new System.Drawing.Point(12, 454);
			this.progressBar1.Maximum = 1000;
			this.progressBar1.Name = "progressBar1";
			this.progressBar1.Size = new System.Drawing.Size(774, 11);
			this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
			this.progressBar1.TabIndex = 1;
			// 
			// trackBar1
			// 
			this.trackBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.trackBar1.LargeChange = 100;
			this.trackBar1.Location = new System.Drawing.Point(0, 467);
			this.trackBar1.Maximum = 1000;
			this.trackBar1.Name = "trackBar1";
			this.trackBar1.Size = new System.Drawing.Size(800, 45);
			this.trackBar1.TabIndex = 2;
			this.trackBar1.TickFrequency = 0;
			this.trackBar1.ValueChanged += new System.EventHandler(this.trackBar1_ValueChanged);
			this.trackBar1.KeyUp += new System.Windows.Forms.KeyEventHandler(this.trackBar1_KeyUp);
			this.trackBar1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.trackBar1_MouseUp);
			// 
			// playButton
			// 
			this.playButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.playButton.Location = new System.Drawing.Point(322, 515);
			this.playButton.Name = "playButton";
			this.playButton.Size = new System.Drawing.Size(75, 23);
			this.playButton.TabIndex = 3;
			this.playButton.Text = "Play";
			this.playButton.UseVisualStyleBackColor = true;
			this.playButton.Click += new System.EventHandler(this.playButton_Click);
			// 
			// pauseButton
			// 
			this.pauseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.pauseButton.Location = new System.Drawing.Point(363, 544);
			this.pauseButton.Name = "pauseButton";
			this.pauseButton.Size = new System.Drawing.Size(75, 23);
			this.pauseButton.TabIndex = 4;
			this.pauseButton.Text = "Pause";
			this.pauseButton.UseVisualStyleBackColor = true;
			this.pauseButton.Click += new System.EventHandler(this.pauseButton_Click);
			// 
			// stopButton
			// 
			this.stopButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.stopButton.Enabled = false;
			this.stopButton.Location = new System.Drawing.Point(404, 515);
			this.stopButton.Name = "stopButton";
			this.stopButton.Size = new System.Drawing.Size(75, 23);
			this.stopButton.TabIndex = 5;
			this.stopButton.Text = "Stop";
			this.stopButton.UseVisualStyleBackColor = true;
			this.stopButton.Click += new System.EventHandler(this.stopButton_Click);
			// 
			// currentTimeTextBox
			// 
			this.currentTimeTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.currentTimeTextBox.Location = new System.Drawing.Point(637, 517);
			this.currentTimeTextBox.Name = "currentTimeTextBox";
			this.currentTimeTextBox.ReadOnly = true;
			this.currentTimeTextBox.Size = new System.Drawing.Size(151, 20);
			this.currentTimeTextBox.TabIndex = 7;
			this.currentTimeTextBox.Text = "__:__:__ / __:__:__";
			this.currentTimeTextBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// backStepButton
			// 
			this.backStepButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.backStepButton.Location = new System.Drawing.Point(282, 544);
			this.backStepButton.Name = "backStepButton";
			this.backStepButton.Size = new System.Drawing.Size(75, 23);
			this.backStepButton.TabIndex = 8;
			this.backStepButton.Text = "<- Step";
			this.backStepButton.UseVisualStyleBackColor = true;
			this.backStepButton.Click += new System.EventHandler(this.backStepButton_Click);
			// 
			// fwdStepButton
			// 
			this.fwdStepButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.fwdStepButton.Location = new System.Drawing.Point(444, 544);
			this.fwdStepButton.Name = "fwdStepButton";
			this.fwdStepButton.Size = new System.Drawing.Size(75, 23);
			this.fwdStepButton.TabIndex = 9;
			this.fwdStepButton.Text = "Step ->";
			this.fwdStepButton.UseVisualStyleBackColor = true;
			this.fwdStepButton.Click += new System.EventHandler(this.fwdStepButton_Click);
			// 
			// playbackRateLabel
			// 
			this.playbackRateLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.playbackRateLabel.AutoSize = true;
			this.playbackRateLabel.Location = new System.Drawing.Point(665, 549);
			this.playbackRateLabel.Name = "playbackRateLabel";
			this.playbackRateLabel.Size = new System.Drawing.Size(75, 13);
			this.playbackRateLabel.TabIndex = 21;
			this.playbackRateLabel.Text = "Playback rate:";
			// 
			// playbackRateControl
			// 
			this.playbackRateControl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.playbackRateControl.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
			this.playbackRateControl.Location = new System.Drawing.Point(743, 547);
			this.playbackRateControl.Maximum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
			this.playbackRateControl.Minimum = new decimal(new int[] {
            15625,
            0,
            0,
            393216});
			this.playbackRateControl.Name = "playbackRateControl";
			this.playbackRateControl.Size = new System.Drawing.Size(45, 20);
			this.playbackRateControl.TabIndex = 20;
			this.playbackRateControl.ThousandsSeparator = true;
			this.playbackRateControl.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.playbackRateControl.ValueChanged += new System.EventHandler(this.playbackRateControl_ValueChanged);
			// 
			// PlaybackWindow
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 577);
			this.Controls.Add(this.playbackRateLabel);
			this.Controls.Add(this.playbackRateControl);
			this.Controls.Add(this.fwdStepButton);
			this.Controls.Add(this.backStepButton);
			this.Controls.Add(this.currentTimeTextBox);
			this.Controls.Add(this.stopButton);
			this.Controls.Add(this.pauseButton);
			this.Controls.Add(this.playButton);
			this.Controls.Add(this.trackBar1);
			this.Controls.Add(this.progressBar1);
			this.Controls.Add(this.amc);
			this.Name = "PlaybackWindow";
			this.Text = "Playback Window";
			((System.ComponentModel.ISupportInitialize)(this.amc)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.playbackRateControl)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private AxAXISMEDIACONTROLLib.AxAxisMediaControl amc;
		private System.Windows.Forms.ProgressBar progressBar1;
		private System.Windows.Forms.TrackBar trackBar1;
		private System.Windows.Forms.Button playButton;
		private System.Windows.Forms.Button pauseButton;
		private System.Windows.Forms.Button stopButton;
		private System.Windows.Forms.TextBox currentTimeTextBox;
		private System.Windows.Forms.Button backStepButton;
		private System.Windows.Forms.Button fwdStepButton;
		private System.Windows.Forms.Label playbackRateLabel;
		private System.Windows.Forms.NumericUpDown playbackRateControl;
	}
}