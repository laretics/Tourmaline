using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tourmaline.Formats.Msts;

namespace Tourmaline.Viewer3D.Materials
{
    /// <summary>
    /// Soporte para animación de algunas sub-piezas de un coche o una locomotora.
    /// Sirve tanto para las animaciones on/off como para las que funcionan de forma continua.
    /// </summary>
    internal class _3dAnimatedPart
    {
        //Pieza que animamos
        readonly PoseableShape mvarPoseableShape;
        //Numero de fotogramas. El valor se saca de la dimensión de las matrices.
        public int FrameCount;

        //Fotograma actual de la animación
        float mvarAnimationKey;

        //Lista de matrices que se animan para esta parte.
        public List<int> MatrixIndexes = new List<int>();

        // Constructor con un link a la figura que contiene las partes animadas
        internal _3dAnimatedPart(PoseableShape poseableShape)
        {
            mvarPoseableShape = poseableShape;
        }

        // El constructor de MSTSWagon añade todas las matrices asociadas con esta parte en la inicalización.
        public void AddMatrix(int matrix)
        {
            if (matrix < 0) return;
            MatrixIndexes.Add(matrix);
            auxUdateFrameCount(matrix);
        }

        void auxUdateFrameCount(int matrix)
        {
            if (null != mvarPoseableShape.SharedShape.Animations
                && mvarPoseableShape.SharedShape.Animations.Count > 0
                && mvarPoseableShape.SharedShape.Animations[0].anim_nodes.Count > matrix
                && mvarPoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers.Count > 0
                && mvarPoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers[0].Count > 0)
            {
                FrameCount = Math.Max(FrameCount, mvarPoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers[0].ToArray().Cast<KeyPosition>().Last().Frame);
                // A veces hay más frames en el segundo controlador que en el primero.
                if (mvarPoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers.Count > 1
                    && mvarPoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers[1].Count > 0)
                    FrameCount = Math.Max(FrameCount, mvarPoseableShape.SharedShape.Animations[0].anim_nodes[matrix].controllers[1].ToArray().Cast<KeyPosition>().Last().Frame);
            }
            for (int i = 0; i < mvarPoseableShape.Hierarchy.Length; i++)
                if (matrix == mvarPoseableShape.Hierarchy[i])
                    auxUdateFrameCount(i);

        }

        //Nos aseguramos que las partes contenidas de este tipo en el archivo shape y sus sub-partes tienen una sección de animación.
        public bool Empty()
        {
            return 0 == MatrixIndexes.Count;
        }

        void auxSetFrame(float frame)
        {
            mvarAnimationKey = frame;
            foreach (int matrix in MatrixIndexes)
                mvarPoseableShape.AnimateMatrix(matrix, mvarAnimationKey);
        }

        //Asigna la animación a un frame en particular mientras lo mantiene dentro del rango de cuenta de frames
        public void setFrameClamp(float frame)
        {
            if (frame > FrameCount) frame = FrameCount;
            if (frame < 0) frame = 0;
            auxSetFrame(frame);
        }

        // Asigna la animación a un frame en particular mientras el ciclo inverso hacia el comienzo como entrada pasa del último frame
        public void setFrameCycle(float frame)
        {
            setFrameClamp(FrameCount - Math.Abs(frame - FrameCount));
        }

        //Asigna la animación a un frame en particular mientras lo envuelve alrededor del rango de cuenta de frames
        public void setFrameWrap(float frame)
        {
            while (FrameCount > 0 && frame < 0) frame += FrameCount;
            if (frame < 0) frame = 0;
            frame %= FrameCount;
            auxSetFrame(frame);
        }

        //Se salta la transición lenta y pasa a la parte inmediata a este nuevo estado
        public void setState(bool state)
        {
            auxSetFrame(state ? FrameCount : 0);
        }

        //Actualiza una parte animada que puede ser uno de dos estados (pantógrafo, puertas, espejos...)
        //ElapsedTime: milisegundos
        public void updateState(bool state, long elapsedTime)
        {
            setFrameClamp(mvarAnimationKey + (state ? 1 : -1) * (float)elapsedTime / 1000);
        }

        //Actualiza una parte animada en movimiento constante (por ejemplo las bielas de una vaporosa), cambiando la cantidad dada.
        public void updateLoop(float change)
        {
            if (null != mvarPoseableShape.SharedShape.Animations
                || 0 == mvarPoseableShape.SharedShape.Animations.Count
                || 0 == FrameCount) return;

            //La velocidad de rotación es un conjunto de 8 frames de animación por rotación a 30FPS
            float frameRate = mvarPoseableShape.SharedShape.Animations[0].FrameRate * 8 / 30f;
            setFrameWrap(mvarAnimationKey + change * frameRate);
        }

        //Actualiza una parte animada que sólo se mueve cuando está activada (por ejemplo los limpiaparabrisas)
        public void updateLoop(bool running, long elapsedTime, float frameRateMultiplier = 1.5f)
        {
            if (null != mvarPoseableShape.SharedShape.Animations
                || 0 == mvarPoseableShape.SharedShape.Animations.Count
                || 0 == FrameCount) return;
            // La velocidad del ciclo es 1.5 frames de animación por segundo a 30FPS por defecto.
            float frameRate = mvarPoseableShape.SharedShape.Animations[0].FrameRate * frameRateMultiplier / 30f;
            if (running || (mvarAnimationKey > 0 && mvarAnimationKey + elapsedTime * frameRate < FrameCount))
                setFrameWrap(mvarAnimationKey + elapsedTime * frameRate);
            else
                auxSetFrame(0);
        }

        //Intercambio de valores
        public static void swap(ref AnimatedPart a, ref AnimatedPart b)
        {
            AnimatedPart temp = a;
            a = b;
            b = temp;
        }

    }
}
