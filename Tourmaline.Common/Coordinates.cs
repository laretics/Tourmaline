/*
 * Sistema de coordenadas:
 * XNA usa un sistema de cordenadas diferente del de MSTS. En XNA, 
 * el eje Z aumenta hacia la cámara, mientras que en MSTS es al revés.
 * Como resultado el signo de todas las coordenadas Z está negado y se
 * han ajustado las matrices tal como se cargan en XNA. Además, el 
 * orden de recorrido de los triángulos está al revés en XNA.
 * Normalmente, las coordenadas, vectores, quaternions y los ángulos
 * se expresan en coordenadas MSTS a menos que se les ponga
 * el prefijo XNA. Se construllen las matrices usando coordenadas XNA
 * para que se puedan usar directamente en las rutinas de dibujo XNA.
 * Por eso, la mayoría de matrices tienen el prefijo XNA en el nombre.
 * 
 * Coordenadas mundiales:
 * X incrementa hacia el este.
 * Y incrementa hacia arriba.
 * Z incrementa hacia el norte.
 * AX incrementa girando hacia abajo.
 * AY incrementa girando hacia la derecha.
 * 
 * LEXICO
 * Location: Es el punto x,y,z donde está ubicado el centro del objeto. (Normalmente un vector3)
 * Pose: La orientación de un objeto 3D, como por ejemplo el balanceo o rotación. Normalmente es una XNAMatrix.
 * Position: Combina pose y location
 * WorldLocation: Es un Location con coordenadas de un tile.
 * WorldPosition: Es un Position con coordenadas de un tile.
 */
using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace TOURMALINE.Common
{
    /// <summary>   
    /// Representa la posición y la orientación de un objeto en un tile en coordenadas XNA.
    /// </summary>
    public class WorldPosition
    {
        /// <summary>Posición local en el tile (relativa al centro del tile)</summary>
        public Matrix XNAMatrix = Matrix.Identity;

        /// <summary>
        /// Constructor vacío por defecto
        /// </summary>
        public WorldPosition()
        {
        }

        /// <summary>
        /// Constructor copia
        /// </summary>
        public WorldPosition(WorldPosition copy)
        {    
            XNAMatrix = copy.XNAMatrix;
        }

        public WorldPosition(Vector3 copy)
        {
            Location = copy;
        }


        /// <summary>
        /// Describe la localización como un vector 3D en coordenadas MSTS en el tile
        /// </summary>
        public Vector3 Location
        {
            get
            {
                Vector3 location = XNAMatrix.Translation;
                location.Z *= -1; // conversión a coordenadas MSTS
                return location;
            }
            set
            {
                value.Z *= -1;
                XNAMatrix.Translation = value;
            }
        }

        /// <summary>
        /// Genera una representación bonita en un string de la posición en el mundo
        /// </summary>
        public override string ToString()
        {
            //return WorldLocation.ToString();
            return XNAMatrix.ToString();
        }
    }
}
