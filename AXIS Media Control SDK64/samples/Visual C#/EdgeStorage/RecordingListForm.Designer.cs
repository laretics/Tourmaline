namespace EdgeStorage
{
	partial class RecordingListForm
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
			this.dataGridView1 = new System.Windows.Forms.DataGridView();
			this.userLabel = new System.Windows.Forms.Label();
			this.ipBox = new System.Windows.Forms.TextBox();
			this.passwordBox = new System.Windows.Forms.TextBox();
			this.userNameBox = new System.Windows.Forms.TextBox();
			this.passLabel = new System.Windows.Forms.Label();
			this.connectButton = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.playButton = new System.Windows.Forms.Button();
			this.firmwareLabel = new System.Windows.Forms.Label();
			this.exportButton = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// dataGridView1
			// 
			this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridView1.Location = new System.Drawing.Point(0, 0);
			this.dataGridView1.MultiSelect = false;
			this.dataGridView1.Name = "dataGridView1";
			this.dataGridView1.ReadOnly = true;
			this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.dataGridView1.Size = new System.Drawing.Size(758, 346);
			this.dataGridView1.TabIndex = 1;
			this.dataGridView1.CellMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView1_CellMouseDoubleClick);
			// 
			// userLabel
			// 
			this.userLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.userLabel.Location = new System.Drawing.Point(10, 47);
			this.userLabel.Name = "userLabel";
			this.userLabel.Size = new System.Drawing.Size(80, 20);
			this.userLabel.TabIndex = 13;
			this.userLabel.Text = "User Name:";
			this.userLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// ipBox
			// 
			this.ipBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.ipBox.Location = new System.Drawing.Point(10, 23);
			this.ipBox.Name = "ipBox";
			this.ipBox.Size = new System.Drawing.Size(168, 20);
			this.ipBox.TabIndex = 0;
			this.ipBox.Text = "0.0.0.0";
			// 
			// passwordBox
			// 
			this.passwordBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.passwordBox.Location = new System.Drawing.Point(98, 71);
			this.passwordBox.Name = "passwordBox";
			this.passwordBox.PasswordChar = '*';
			this.passwordBox.Size = new System.Drawing.Size(80, 20);
			this.passwordBox.TabIndex = 2;
			this.passwordBox.Text = "";
			this.passwordBox.UseSystemPasswordChar = true;
			// 
			// userNameBox
			// 
			this.userNameBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.userNameBox.Location = new System.Drawing.Point(98, 47);
			this.userNameBox.Name = "userNameBox";
			this.userNameBox.Size = new System.Drawing.Size(80, 20);
			this.userNameBox.TabIndex = 1;
			this.userNameBox.Text = "root";
			// 
			// passLabel
			// 
			this.passLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.passLabel.Location = new System.Drawing.Point(10, 71);
			this.passLabel.Name = "passLabel";
			this.passLabel.Size = new System.Drawing.Size(80, 20);
			this.passLabel.TabIndex = 12;
			this.passLabel.Text = "Password:";
			this.passLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// connectButton
			// 
			this.connectButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.connectButton.Location = new System.Drawing.Point(103, 99);
			this.connectButton.Name = "connectButton";
			this.connectButton.Size = new System.Drawing.Size(75, 23);
			this.connectButton.TabIndex = 3;
			this.connectButton.Text = "Connect";
			this.connectButton.UseVisualStyleBackColor = true;
			this.connectButton.Click += new System.EventHandler(this.connectButton_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.groupBox1.Controls.Add(this.ipBox);
			this.groupBox1.Controls.Add(this.connectButton);
			this.groupBox1.Controls.Add(this.passLabel);
			this.groupBox1.Controls.Add(this.userLabel);
			this.groupBox1.Controls.Add(this.userNameBox);
			this.groupBox1.Controls.Add(this.passwordBox);
			this.groupBox1.Location = new System.Drawing.Point(12, 352);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(188, 128);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Download Recording List";
			// 
			// playButton
			// 
			this.playButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.playButton.Enabled = false;
			this.playButton.Location = new System.Drawing.Point(671, 428);
			this.playButton.Name = "playButton";
			this.playButton.Size = new System.Drawing.Size(75, 23);
			this.playButton.TabIndex = 2;
			this.playButton.Text = "Play";
			this.playButton.UseVisualStyleBackColor = true;
			this.playButton.Click += new System.EventHandler(this.playButton_Click);
			// 
			// firmwareLabel
			// 
			this.firmwareLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.firmwareLabel.AutoSize = true;
			this.firmwareLabel.Location = new System.Drawing.Point(207, 365);
			this.firmwareLabel.Name = "firmwareLabel";
			this.firmwareLabel.Size = new System.Drawing.Size(112, 13);
			this.firmwareLabel.TabIndex = 18;
			this.firmwareLabel.Text = "Firmware version: N/A";
			// 
			// exportButton
			// 
			this.exportButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.exportButton.Enabled = false;
			this.exportButton.Location = new System.Drawing.Point(671, 457);
			this.exportButton.Name = "exportButton";
			this.exportButton.Size = new System.Drawing.Size(75, 23);
			this.exportButton.TabIndex = 3;
			this.exportButton.Text = "Export";
			this.exportButton.UseVisualStyleBackColor = true;
			this.exportButton.Click += new System.EventHandler(this.exportButton_Click);
			// 
			// RecordingListForm
			// 
			this.AcceptButton = this.playButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(758, 492);
			this.Controls.Add(this.exportButton);
			this.Controls.Add(this.firmwareLabel);
			this.Controls.Add(this.playButton);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.dataGridView1);
			this.Name = "RecordingListForm";
			this.Text = "Recording List";
			((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.DataGridView dataGridView1;
		private System.Windows.Forms.Label userLabel;
		private System.Windows.Forms.TextBox ipBox;
		private System.Windows.Forms.TextBox passwordBox;
		private System.Windows.Forms.TextBox userNameBox;
		private System.Windows.Forms.Label passLabel;
		private System.Windows.Forms.Button connectButton;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button playButton;
		private System.Windows.Forms.Label firmwareLabel;
		private System.Windows.Forms.Button exportButton;
	}
}