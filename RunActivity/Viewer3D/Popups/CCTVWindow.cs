using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Tourmaline.Viewer3D.Popups
{
    public class CCTVWindow:Window
    {        

        Label Title;
        CameraControl mvarControl;

        public CCTVWindow(WindowManager owner)
            : base(owner, 640, Window.DecorationSize.Y + 480, "CCTV")
        {
            mvarControl = new CameraControl(630, 480);


        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            mvarControl.Draw(spriteBatch, new Point(0, 0));
        }


    }
    public class CameraControl : Control
    {
        static Texture2D CanvasTexture;

        public CameraControl(int width, int height)
            : base(0,0,width, height)
            {


        }

        internal override void Draw(SpriteBatch canvas, Point offset)
        {
            //Primer acercamiento... vamos a intentar cargar una imagen fija
            if (null == CanvasTexture)
            {
                //Cargamos la textura por primera vez.
                string resourcesFile = Tourmaline.Viewer3D.Processes.Game.Instance.ResourcesPath;
                string cartaFile = Path.Combine(resourcesFile, "SetView.png");
                if (File.Exists(cartaFile))
                {
                    CanvasTexture = new Texture2D(canvas.GraphicsDevice, 640, 480, false, SurfaceFormat.Color);
                    CanvasTexture = SharedTextureManager.Get(canvas.GraphicsDevice, cartaFile);
                }
            }
            Debug.Assert(null != CanvasTexture);
            canvas.Draw(CanvasTexture,new Rectangle(4,56,632,472),Color.White);
        }


    }
}
