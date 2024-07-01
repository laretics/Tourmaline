using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tourmaline.Viewer3D.TvForms
{
    internal class TvForm:System.Windows.Forms.Form
    {
        private System.Windows.Forms.Button CbTest;
        private System.Windows.Forms.Label LbDebug;
        public _3dTrainControl mvarFondo;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            //this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.CbTest = new System.Windows.Forms.Button();
            this.LbDebug = new System.Windows.Forms.Label();
            this.mvarFondo = new Tourmaline.Viewer3D.TvForms._3dTrainControl();
            this.SuspendLayout();
            // 
            // CbTest
            // 
            this.CbTest.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CbTest.Location = new System.Drawing.Point(511, 295);
            this.CbTest.Name = "CbTest";
            this.CbTest.Size = new System.Drawing.Size(183, 91);
            this.CbTest.TabIndex = 1;
            this.CbTest.Text = "Prueba";
            this.CbTest.UseVisualStyleBackColor = true;
            // 
            // LbDebug
            // 
            this.LbDebug.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.LbDebug.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.LbDebug.Location = new System.Drawing.Point(12, 295);
            this.LbDebug.Name = "LbDebug";
            this.LbDebug.Size = new System.Drawing.Size(445, 111);
            this.LbDebug.TabIndex = 2;
            // 
            // mvarFondo
            // 
            this.mvarFondo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mvarFondo.Location = new System.Drawing.Point(0, 0);
            this.mvarFondo.Name = "mvarFondo";
            this.mvarFondo.Size = new System.Drawing.Size(706, 430);
            this.mvarFondo.TabIndex = 0;
            this.mvarFondo.Text = "Fondo";
            // 
            // TvForm
            // 
            this.ClientSize = new System.Drawing.Size(706, 430);
            this.Controls.Add(this.LbDebug);
            this.Controls.Add(this.CbTest);
            this.Controls.Add(this.mvarFondo);
            this.Name = "TvForm";
            this.Text = "Tourmaline";
            this.Load += new System.EventHandler(this.TvForm_Load);
            this.ResumeLayout(false);

        }

        private void TvForm_Load(object sender, EventArgs e)
        {

        }
    }
}
