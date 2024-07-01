using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tourmaline.Viewer3D.TvForms
{
    //Objeto que contiene la escena del tren 3D a visualizar...
    //Basicamente contiene el tren y una cámara que iremos moviendo.
    internal class _3dTrainViewer
    {
        internal GraphicsDevice GraphicsDevice { get; private set; }
        internal Materials.SharedTextureManager TextureManager { get; private set; }
        internal SharedMaterialManager MaterialManager { get; private set; }
        internal SharedShapeManager ShapeManager { get; private set; }

        internal _3dCamera Camera { get; set; }
        internal static Viewport DefaultViewport;

        internal Vector3 NearPoint { get; private set; }
        internal Vector3 FarPoint { get;private set; }

        internal _3dTrainViewer(GraphicsDevice gd)
        {
            Camera = new _3dCamera(this);
            this.GraphicsDevice = gd;
            Initialize();
        }



        private void Initialize()
        {
            DefaultViewport = GraphicsDevice.Viewport;
            TextureManager = new Materials.SharedTextureManager(GraphicsDevice);



        }

    }
}
