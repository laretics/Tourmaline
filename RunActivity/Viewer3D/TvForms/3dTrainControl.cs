using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tourmaline.Simulation.RollingStocks;
using Tourmaline.Viewer3D.Materials;

namespace Tourmaline.Viewer3D.TvForms
{
    public class _3dTrainControl:GraphicsDeviceControl
    {        
        private Materials.RenderFrame mvarFrame;
        private bool mvarAsigned = false;

        internal TourmalineTrain Train
        {
            get => mvarViewer.Train;
            set
            {
                
                mvarViewer.Train = value;
                mvarViewer.Initialize();
                
                mvarFrame.PrepareFrame(mvarViewer);                
                mvarViewer.Update(mvarFrame, 0);
                mvarAsigned = true;
            }
        }

        protected override void Initialize()
        {
            mvarViewer = new _3dTrainViewer(this);
            mvarFrame = new Materials.RenderFrame(this);
        }


        protected override void OnDraw(EventArgs e)
        {
            if(mvarAsigned)
            {                
                mvarFrame.Draw(this);
            }
            else
            {
                GraphicsDevice.Clear(Color.White);
            }         
        }
    }
}
