using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tourmaline.Viewer3D.Popups
{
    public class CCTVWindow:Window
    {        

        Label Title;

        public CCTVWindow(WindowManager owner)
            : base(owner, 640, Window.DecorationSize.Y + 480, "CCTV")
        {

        }


    }
    public class CameraControl : Control
    {
        static Texture2D CanvasTexture;

        public CameraControl(int width, int height)
            : base(0,0,width, height)
            {

            }

        internal override void Draw(SpriteBatch spriteBatch, Point offset)
        {
            //Primer acercamiento... vamos a intentar cargar una imagen fija
            if(null ==CanvasTexture)
            {

            }

        }


    }
}
