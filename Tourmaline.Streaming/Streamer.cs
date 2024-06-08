using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tourmaline.Streaming
{
    //Este objeto obtiene una transmisión de imágenes desde un dispositivo
    //de tipo cámara y permite extraer una textura 2D de ella para representar
    //en una superficie de Tourmaline.
    public abstract class Streamer
    {
        Texture2D mvarTexture; //Textura en la que guardaré el streaming.
        public void init(GraphicsDevice gd,int width, int height)
        {
            mvarTexture = new Texture2D(gd, width, height);

        }

        internal abstract bool capture(); //Rutina que captura un frame en la textura
        public Texture2D texture { get => mvarTexture; }
    }
}
