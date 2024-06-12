// Código experimental que colapsa las primitivas duplicadas innecesariamente al cargar shapes.
//PELIGRO: Es más lento y no se garantiza que funcione bien.
//#define OPTIMIZE_SHAPES_ON_LOAD

//Imprime un montón de información de diagnóstico sobre la construcción de las shapes con sus respectivos sub-objetos y jerarquías.
//#define DEBUG_SHAPE_HIERARCHY

//Añade flechas verde brillante a todas las shapes indicando la dirección de sus vectores normales.
//#define DEBUG_SHAPE_NORMALS

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tourmaline.Formats.Msts;
using Tourmaline.Simulation;
using Tourmaline.Simulation.RollingStocks;
using Tourmaline.Viewer3D.Common;
using Tourmaline.Viewer3D;
using TOURMALINE.Common;
using System.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using static Tourmaline.Viewer3D.SharedShape;
using System.Xml.Linq;
using Tourmaline.Common;


using Event = Tourmaline.Common.Event;
using Events = Tourmaline.Common.Events;
using static Tourmaline.Viewer3D.Common.Helpers;

namespace Tourmaline.Viewer3D
{
    [CallOnThread("Loader")]
    public class SharedShapeManager
    {
        readonly Viewer Viewer;

        Dictionary<string, SharedShape> Shapes = new Dictionary<string, SharedShape>();
        Dictionary<string, bool> ShapeMarks;
        SharedShape EmptyShape;

        [CallOnThread("Render")]
        internal SharedShapeManager(Viewer viewer)
        {
            Viewer = viewer;
            EmptyShape = new SharedShape(Viewer);
        }

        public SharedShape Get(string path)
        {
            if (Thread.CurrentThread.Name != "Loader Process")
                Trace.TraceError("SharedShapeManager.Get incorrectly called by {0}; must be Loader Process or crashes will occur.", Thread.CurrentThread.Name);

            if (path == null || path == EmptyShape.FilePath)
                return EmptyShape;

            path = path.ToLowerInvariant();
            if (!Shapes.ContainsKey(path))
            {
                try
                {
                    Shapes.Add(path, new SharedShape(Viewer, path));
                }
                catch (Exception error)
                {
                    Trace.WriteLine(new FileLoadException(path, error));
                    Shapes.Add(path, EmptyShape);
                }
            }
            return Shapes[path];
        }

        public void Mark()
        {
            ShapeMarks = new Dictionary<string, bool>(Shapes.Count);
            foreach (var path in Shapes.Keys)
                ShapeMarks.Add(path, false);
        }

        public void Mark(SharedShape shape)
        {
            if (Shapes.ContainsValue(shape))
                ShapeMarks[Shapes.First(kvp => kvp.Value == shape).Key] = true;
        }

        public void Sweep()
        {
            foreach (var path in ShapeMarks.Where(kvp => !kvp.Value).Select(kvp => kvp.Key))
                Shapes.Remove(path);
        }

        [CallOnThread("Updater")]
        public string GetStatus()
        {
            return string.Format("{0:F0} shapes", Shapes.Keys.Count);
        }
    }

    [Flags]
    public enum ShapeFlags
    {
        None = 0,
        // Shape casts a shadow (scenery objects according to RE setting, and all train objects).
        ShadowCaster = 1,
        // Shape needs automatic z-bias to keep it out of trouble.
        AutoZBias = 2,
        // Shape is an interior and must be rendered in a separate group.
        Interior = 4,
        // NOTE: Use powers of 2 for values!
    }

    public class StaticShape
    {
        public readonly Viewer Viewer;
        public readonly WorldPosition Location;
        public readonly ShapeFlags Flags;
        public readonly SharedShape SharedShape;

        /// <summary>
        /// Construye e inicializa la clase
        /// Este constructor es para los objetos descritos por un archivo shape de MSTS
        /// </summary>
        public StaticShape(Viewer viewer, string path, WorldPosition position, ShapeFlags flags)
        {
            Viewer = viewer;
            Location = position;
            Flags = flags;
            SharedShape = Viewer.ShapeManager.Get(path);
        }

        public virtual void PrepareFrame(RenderFrame frame, long elapsedTime)
        {
            SharedShape.PrepareFrame(frame, Location, Flags);
        }

        [CallOnThread("Loader")]
        public virtual void Unload()
        {
        }

        [CallOnThread("Loader")]
        internal virtual void Mark()
        {
            SharedShape.Mark();
        }
    }

    public class SharedStaticShapeInstance : StaticShape
    {
        readonly bool HasNightSubObj;
        readonly float ObjectRadius;
        readonly float ObjectViewingDistance;
        readonly ShapePrimitiveInstances[] Primitives;

        public SharedStaticShapeInstance(Viewer viewer, string path, List<StaticShape> shapes)
            : base(viewer, path, GetCenterLocation(shapes), shapes[0].Flags)
        {
            HasNightSubObj = shapes[0].SharedShape.HasNightSubObj;

            if (shapes[0].SharedShape.LodControls.Length > 0)
            {
                // Se necesitan los dos extremos de niveles de distancia.
                // Se renderiza el primero pero se ve tan lejos como el último.
                var dlHighest = shapes[0].SharedShape.LodControls[0].DistanceLevels.First();
                var dlLowest = shapes[0].SharedShape.LodControls[0].DistanceLevels.Last();

                // El radio del objeto debería extenderse del centro a la última localización de la instancia
                // MAS el radio del objeto actual. 
                ObjectRadius = shapes.Max(s => (Location.Location - s.Location.Location).Length()) + dlHighest.ViewSphereRadius;

                // La distancia de visualización del objeto es fácil porque se basa en el exterior
                // del radio del objeto.
                if (Processes.Game.Instance.LODViewingExtention)
                    ObjectViewingDistance = float.MaxValue;
                else
                    ObjectViewingDistance = dlLowest.ViewingDistance;
            }

            // Genera todas las primitivas para la shape compartida.
            var prims = new List<ShapePrimitiveInstances>();
            foreach (var lod in shapes[0].SharedShape.LodControls)
                for (var subObjectIndex = 0; subObjectIndex < lod.DistanceLevels[0].SubObjects.Length; subObjectIndex++)
                    foreach (var prim in lod.DistanceLevels[0].SubObjects[subObjectIndex].ShapePrimitives)
                        prims.Add(new ShapePrimitiveInstances(viewer.GraphicsDevice, prim, GetMatricies(shapes, prim), subObjectIndex));
            Primitives = prims.ToArray();
        }

        static WorldPosition GetCenterLocation(List<StaticShape> shapes)
        {
            var minX = shapes.Min(s => s.Location.Location.X);
            var maxX = shapes.Max(s => s.Location.Location.X);
            var minY = shapes.Min(s => s.Location.Location.Y);
            var maxY = shapes.Max(s => s.Location.Location.Y);
            var minZ = shapes.Min(s => s.Location.Location.Z);
            var maxZ = shapes.Max(s => s.Location.Location.Z);
            return new WorldPosition() { Location = new Vector3((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2) };
        }

        Matrix[] GetMatricies(List<StaticShape> shapes, ShapePrimitive shapePrimitive)
        {
            var matrix = Matrix.Identity;
            var hi = shapePrimitive.HierarchyIndex;
            while (hi >= 0 && hi < shapePrimitive.Hierarchy.Length && shapePrimitive.Hierarchy[hi] != -1)
            {
                matrix *= SharedShape.Matrices[hi];
                hi = shapePrimitive.Hierarchy[hi];
            }

            var matricies = new Matrix[shapes.Count];
            for (var i = 0; i < shapes.Count; i++)
                matricies[i] = matrix * shapes[i].Location.XNAMatrix * Matrix.CreateTranslation(-Location.Location.X, -Location.Location.Y, Location.Location.Z);

            return matricies;
        }

        public override void PrepareFrame(RenderFrame frame, long elapsedTime)
        {
            var mstsLocation = Location.Location;// + new Vector3(dTileX * 2048, 0, dTileZ * 2048);
            var xnaMatrix = Matrix.CreateTranslation(mstsLocation.X, mstsLocation.Y, -mstsLocation.Z);
            foreach (var primitive in Primitives)
                if (primitive.SubObjectIndex != 1 || !HasNightSubObj || Viewer.MaterialManager.sunDirection.Y < 0)
                    frame.AddAutoPrimitive(mstsLocation, ObjectRadius, ObjectViewingDistance, primitive.Material, primitive, RenderPrimitiveGroup.World, ref xnaMatrix, Flags);
        }
    }

    public class StaticTrackShape : StaticShape
    {
        public StaticTrackShape(Viewer viewer, string path, WorldPosition position)
            : base(viewer, path, position, ShapeFlags.AutoZBias)
        {
        }
    }

    /// <summary>
    /// Esta shape tiene una jerarquía de objetos que se pueden mover a base
    /// de ajustar las matrices XNAMatrices en cada nodo.
    /// </summary>
    public class PoseableShape : StaticShape
    {
        protected static Dictionary<string, bool> SeenShapeAnimationError = new Dictionary<string, bool>();

        public Matrix[] XNAMatrices = new Matrix[0];  // las posiciones de los sub-objetos

        public readonly int[] Hierarchy;

        public PoseableShape(Viewer viewer, string path, WorldPosition initialPosition, ShapeFlags flags)
            : base(viewer, path, initialPosition, flags)
        {
            XNAMatrices = new Matrix[SharedShape.Matrices.Length];
            for (int iMatrix = 0; iMatrix < SharedShape.Matrices.Length; ++iMatrix)
                XNAMatrices[iMatrix] = SharedShape.Matrices[iMatrix];

            if (SharedShape.LodControls.Length > 0 && SharedShape.LodControls[0].DistanceLevels.Length > 0 && SharedShape.LodControls[0].DistanceLevels[0].SubObjects.Length > 0 && SharedShape.LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives.Length > 0)
                Hierarchy = SharedShape.LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives[0].Hierarchy;
            else
                Hierarchy = new int[0];
        }

        public PoseableShape(Viewer viewer, string path, WorldPosition initialPosition)
            : this(viewer, path, initialPosition, ShapeFlags.None)
        {
        }

        public override void PrepareFrame(RenderFrame frame, long elapsedTime)
        {
            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);
        }

        /// <summary>
        /// Ajusta la posición del nodo especificado a la posición del frame que
        /// se especifica por su clave (key)
        /// </summary>
        public void AnimateMatrix(int iMatrix, float key)
        {
            // Anima la matriz dada.
            AnimateOneMatrix(iMatrix, key);

            // Anima todos los nodos hijos en la jerarquía también.
            for (var i = 0; i < Hierarchy.Length; i++)
                if (Hierarchy[i] == iMatrix)
                    AnimateMatrix(i, key);
        }

        protected virtual void AnimateOneMatrix(int iMatrix, float key)
        {
            if (SharedShape.Animations == null || SharedShape.Animations.Count == 0)
            {
                if (!SeenShapeAnimationError.ContainsKey(SharedShape.FilePath))
                    Trace.TraceInformation("Datos de animaciones no encontradas e ignoradas en shape {0}", SharedShape.FilePath);
                SeenShapeAnimationError[SharedShape.FilePath] = true;
                return;  // Falta la animación.
            }

            if (iMatrix < 0 || iMatrix >= SharedShape.Animations[0].anim_nodes.Count || iMatrix >= XNAMatrices.Length)
            {
                if (!SeenShapeAnimationError.ContainsKey(SharedShape.FilePath))
                    Trace.TraceInformation("Ignorado índice fuera de rango en matriz {1} en shape {0}", SharedShape.FilePath, iMatrix);
                SeenShapeAnimationError[SharedShape.FilePath] = true;
                return;  // Matrices con parámetros erróneos
            }

            var anim_node = SharedShape.Animations[0].anim_nodes[iMatrix];
            if (anim_node.controllers.Count == 0)
                return;  // Faltan los controladores

            // Comienza con la posición inicial en el archivo Shape.
            var xnaPose = SharedShape.Matrices[iMatrix];

            foreach (controller controller in anim_node.controllers)
            {
                // Determina el índice de frame del frame actual ('key').
                // Se calcula la interpolación entre dos fotogramas key
                // (que son los items en 'controller') de manera que
                // tendremos que buscar el último MENOR que el fotograma actual
                // e interpolaremos con el siguiente tras él.
                var index = 0;
                for (var i = 0; i < controller.Count; i++)
                    if (controller[i].Frame <= key)
                        index = i;
                    else if (controller[i].Frame > key) // No hace falta optimización para el algoritmo.
                        break;

                var position1 = controller[index];
                var position2 = index + 1 < controller.Count ? controller[index + 1] : controller[index];
                var frame1 = position1.Frame;
                var frame2 = position2.Frame;

                // Hay que limitar entre dos valores la cantidad porque es posible
                // salirse del rango de fotogramas.
                // Además hay que asegurarse de que los valores de frame1 y frame2
                // no coinciden porque si no haremos una división por cero.
                var amount = frame1 < frame2 ? MathHelper.Clamp((key - frame1) / (frame2 - frame1), 0, 1) : 0;

                if (position1.GetType() == typeof(slerp_rot))  // rotate the existing matrix
                {
                    slerp_rot MSTS1 = (slerp_rot)position1;
                    slerp_rot MSTS2 = (slerp_rot)position2;
                    Quaternion XNA1 = new Quaternion(MSTS1.X, MSTS1.Y, -MSTS1.Z, MSTS1.W);
                    Quaternion XNA2 = new Quaternion(MSTS2.X, MSTS2.Y, -MSTS2.Z, MSTS2.W);
                    Quaternion q = Quaternion.Slerp(XNA1, XNA2, amount);
                    Vector3 location = xnaPose.Translation;
                    xnaPose = Matrix.CreateFromQuaternion(q);
                    xnaPose.Translation = location;
                }
                else if (position1.GetType() == typeof(linear_key))  // una clave (key) asigna una posición absoluta, o bien desplazaremos la matriz existente
                {
                    linear_key MSTS1 = (linear_key)position1;
                    linear_key MSTS2 = (linear_key)position2;
                    Vector3 XNA1 = new Vector3(MSTS1.X, MSTS1.Y, -MSTS1.Z);
                    Vector3 XNA2 = new Vector3(MSTS2.X, MSTS2.Y, -MSTS2.Z);
                    Vector3 v = Vector3.Lerp(XNA1, XNA2, amount);
                    xnaPose.Translation = v;
                }
                else if (position1.GetType() == typeof(tcb_key)) // una clave tcb (tcb_key) asigna una rotación absoluta o bien rotaremos la matriz existente.
                {
                    tcb_key MSTS1 = (tcb_key)position1;
                    tcb_key MSTS2 = (tcb_key)position2;
                    Quaternion XNA1 = new Quaternion(MSTS1.X, MSTS1.Y, -MSTS1.Z, MSTS1.W);
                    Quaternion XNA2 = new Quaternion(MSTS2.X, MSTS2.Y, -MSTS2.Z, MSTS2.W);
                    Quaternion q = Quaternion.Slerp(XNA1, XNA2, amount);
                    Vector3 location = xnaPose.Translation;
                    xnaPose = Matrix.CreateFromQuaternion(q);
                    xnaPose.Translation = location;
                }
            }
            XNAMatrices[iMatrix] = xnaPose;  // actualizamos la matriz
        }
    }

    /// <summary>
    /// Una forma animada tiene un movimiento continuo definido
    /// en las animaciones del archivo shape.
    /// </summary>
    public class AnimatedShape : PoseableShape
    {
        protected float AnimationKey;  // avanza con el tiempo
        protected float FrameRateMultiplier = 1; // por ejemplo, en la vista del pasajero MSTS divide por 30 el ratio de fotogramas (frame rate); este valor es el inverso

        /// <summary>
        /// Construye e inicia la clase
        /// </summary>
        public AnimatedShape(Viewer viewer, string path, WorldPosition initialPosition, ShapeFlags flags, float frameRateDivisor = 1.0f)
            : base(viewer, path, initialPosition, flags)
        {
            FrameRateMultiplier = 1 / frameRateDivisor;
        }

        public override void PrepareFrame(RenderFrame frame, long elapsedTime)
        {
            // si la shape tiene animaciones...
            if (SharedShape.Animations?.Count > 0 && SharedShape.Animations[0].FrameCount > 0)
            {
                AnimationKey += SharedShape.Animations[0].FrameRate * (float)elapsedTime/1000 * FrameRateMultiplier;
                while (AnimationKey > SharedShape.Animations[0].FrameCount) AnimationKey -= SharedShape.Animations[0].FrameCount;
                while (AnimationKey < 0) AnimationKey += SharedShape.Animations[0].FrameCount;

                // Actualiza la forma para cada matriz
                for (var matrix = 0; matrix < SharedShape.Matrices.Length; ++matrix)
                    AnimateMatrix(matrix, AnimationKey);
            }
            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);
        }
    }

    //Clase AnalogClockShape para animar los relojes de Open Rails como clase hija de AnimatedShape <- PoseableShape <- StaticShape
    public class AnalogClockShape : AnimatedShape
    {
        public AnalogClockShape(Viewer viewer, string path, WorldPosition initialPosition, ShapeFlags flags, float frameRateDivisor = 1.0f)
            : base(viewer, path, initialPosition, flags)
        {
        }

        protected override void AnimateOneMatrix(int iMatrix, float key)
        {
            if (SharedShape.Animations == null || SharedShape.Animations.Count == 0)
            {
                if (!SeenShapeAnimationError.ContainsKey(SharedShape.FilePath))
                    Trace.TraceInformation("Se ignoran datos de animación que faltan en shape {0}", SharedShape.FilePath);
                SeenShapeAnimationError[SharedShape.FilePath] = true;
                return;  // falta la animación
            }

            if (iMatrix < 0 || iMatrix >= SharedShape.Animations[0].anim_nodes.Count || iMatrix >= XNAMatrices.Length)
            {
                if (!SeenShapeAnimationError.ContainsKey(SharedShape.FilePath))
                    Trace.TraceInformation("Se ignoran índices fuera de límites de la matriz {1} en shape {0}", SharedShape.FilePath, iMatrix);
                SeenShapeAnimationError[SharedShape.FilePath] = true;
                return;  // matrices con límites erróneos
            }

            var anim_node = SharedShape.Animations[0].anim_nodes[iMatrix];
            if (anim_node.controllers.Count == 0)
                return;  //faltan controladores

            // Comienza con la posición inicial en el archivo de forma,
            var xnaPose = SharedShape.Matrices[iMatrix];

            foreach (controller controller in anim_node.controllers)
            {
                // Determina el índice del fotograma del fotograma actual ('key).
                // Interpolaremos entre dos fotogramas clave (los items en 'controller')
                // de forma que será necesario encontrar el último MENOR que el actual
                // e interpolar con el siguiente.
                var index = 0;
                for (var i = 0; i < controller.Count; i++)
                    if (controller[i].Frame <= key)
                        index = i;
                    else if (controller[i].Frame > key) // Optimización, no requerido para el algoritmo.
                        break;

                //Animación de las agujas de los relojes OpenRails
                var animName = anim_node.Name.ToLowerInvariant();
                if (animName.IndexOf("hand_clock") > -1)
                {
                    int gameTimeInSec = Convert.ToInt32(Viewer.microSim.gameTime.TotalGameTime.Ticks / 100000); //Game time as integer in milliseconds
                    int clockHour = gameTimeInSec / 360000 % 24;                          //HOUR of Game time
                    gameTimeInSec %= 360000;                                                //Game time by Modulo 360000 -> resultes minutes as rest
                    int clockMinute = gameTimeInSec / 6000;                                 //MINUTE of Game time
                    gameTimeInSec %= 6000;                                                  //Game time by Modulo 6000 -> resultes seconds as rest
                    int clockSecond = gameTimeInSec / 100;                                  //SECOND of Game time
                    int clockCenti = (gameTimeInSec - clockSecond * 100);                   //CENTI-SECOND of Game time
                    int clockQuadrant = 0;                                                  //Preset: Start with Anim-Control 0 (first quadrant of OR-Clock)
                    bool calculateClockHand = false;                                        //Preset: No drawing of a new matrix by default
                    float quadrantAmount = 1;                                               //Preset: Represents part of the way from position1 to position2 (float Value between 0 and 1)
                    if (animName.StartsWith("orts_chand_clock")) //Shape matrix is a CentiSecond Hand (continuous moved second hand) of an analog OR-clock
                    {
                        clockQuadrant = (int)clockSecond / 15;                              //Quadrant of the clock / Key-Index of anim_node (int Values: 0, 1, 2, 3)
                        quadrantAmount = (float)(clockSecond - (clockQuadrant * 15)) / 15;  //Seconds      Percentage quadrant related (float Value between 0 and 1) 
                        quadrantAmount += ((float)clockCenti / 100 / 15);                   //CentiSeconds Percentage quadrant related (float Value between 0 and 0.0666666)
                        if (controller.Count == 0 || clockQuadrant < 0 || clockQuadrant + 1 > controller.Count - 1)
                            clockQuadrant = 0;  //If controller.Count dosen't match
                        calculateClockHand = true;                                          //Calculate the new Hand position (Quaternion) below
                    }
                    else if (animName.StartsWith("orts_shand_clock")) //Shape matrix is a Second Hand of an analog OR-clock
                    {
                        clockQuadrant = (int)clockSecond / 15;                              //Quadrant of the clock / Key-Index of anim_node (int Values: 0, 1, 2, 3)
                        quadrantAmount = (float)(clockSecond - (clockQuadrant * 15)) / 15;  //Percentage quadrant related (float Value between 0 and 1) 
                        if (controller.Count == 0 || clockQuadrant < 0 || clockQuadrant + 1 > controller.Count - 1)
                            clockQuadrant = 0;  //If controller.Count doesn't match
                        calculateClockHand = true;                                          //Calculate the new Hand position (Quaternion) below
                    }
                    else if (animName.StartsWith("orts_mhand_clock")) //Shape matrix is a Minute Hand of an analog OR-clock
                    {
                        clockQuadrant = (int)clockMinute / 15;                              //Quadrant of the clock / Key-Index of anim_node (Values: 0, 1, 2, 3)
                        quadrantAmount = (float)(clockMinute - (clockQuadrant * 15)) / 15;  //Percentage quadrant related (Value between 0 and 1)
                        if (controller.Count == 0 || clockQuadrant < 0 || clockQuadrant + 1 > controller.Count - 1)
                            clockQuadrant = 0; //If controller.Count dosen't match
                        calculateClockHand = true;                                          //Calculate the new Hand position (Quaternion) below
                    }
                    else if (animName.StartsWith("orts_hhand_clock")) //Shape matrix is an Hour Hand of an analog OR-clock
                    {
                        clockHour %= 12;                                                    //Reduce 24 to 12 format
                        clockQuadrant = (int)clockHour / 3;                                 //Quadrant of the clock / Key-Index of anim_node (Values: 0, 1, 2, 3)
                        quadrantAmount = (float)(clockHour - (clockQuadrant * 3)) / 3;      //Percentage quadrant related (Value between 0 and 1)
                        quadrantAmount += (((float)1 / 3) * ((float)clockMinute / 60));     //add fine minute-percentage for Hour Hand between the full hours
                        if (controller.Count == 0 || clockQuadrant < 0 || clockQuadrant + 1 > controller.Count - 1)
                            clockQuadrant = 0; //If controller.Count doesn't match
                        calculateClockHand = true;                                          //Calculate the new Hand position (Quaternion) below
                    }
                    if (calculateClockHand == true & controller.Count > 0)                  //Calculate new Hand position as usual OR-style (Slerp-animation with Quaternions)
                    {
                        var position1 = controller[clockQuadrant];
                        var position2 = controller[clockQuadrant + 1];
                        if (position1 is slerp_rot sr1 && position2 is slerp_rot sr2)  //OR-Clock anim.node has slerp keys
                        {
                            Quaternion XNA1 = new Quaternion(sr1.X, sr1.Y, -sr1.Z, sr1.W);
                            Quaternion XNA2 = new Quaternion(sr2.X, sr2.Y, -sr2.Z, sr2.W);
                            Quaternion q = Quaternion.Slerp(XNA1, XNA2, quadrantAmount);
                            Vector3 location = xnaPose.Translation;
                            xnaPose = Matrix.CreateFromQuaternion(q);
                            xnaPose.Translation = location;
                        }
                        else if (position1 is linear_key lk1 && position2 is linear_key lk2) //OR-Clock anim.node has tcb keys
                        {
                            Vector3 XNA1 = new Vector3(lk1.X, lk1.Y, -lk1.Z);
                            Vector3 XNA2 = new Vector3(lk2.X, lk2.Y, -lk2.Z);
                            Vector3 v = Vector3.Lerp(XNA1, XNA2, quadrantAmount);
                            xnaPose.Translation = v;
                        }
                        else if (position1 is tcb_key tk1 && position2 is tcb_key tk2) //OR-Clock anim.node has tcb keys
                        {
                            Quaternion XNA1 = new Quaternion(tk1.X, tk1.Y, -tk1.Z, tk1.W);
                            Quaternion XNA2 = new Quaternion(tk2.X, tk2.Y, -tk2.Z, tk2.W);
                            Quaternion q = Quaternion.Slerp(XNA1, XNA2, quadrantAmount);
                            Vector3 location = xnaPose.Translation;
                            xnaPose = Matrix.CreateFromQuaternion(q);
                            xnaPose.Translation = location;
                        }
                    }
                }
            }
            XNAMatrices[iMatrix] = xnaPose;  // actualiza la matriz
        }
    }

    public class SwitchTrackShape : PoseableShape
    {
        protected float AnimationKey;  // define la posición de las agujas en su movimiento a directa o a desviada

        TrJunctionNode TrJunctionNode;  // tiene datoos en la alineación actual para el cambio
        uint MainRoute;                  // 0 or 1 - which route is considered the main route

        public SwitchTrackShape(Viewer viewer, string path, WorldPosition position, TrJunctionNode trj)
            : base(viewer, path, position, ShapeFlags.AutoZBias)
        {
            TrJunctionNode = trj;
            TrackShape TS = viewer.microSim.TSectionDat.TrackShapes.Get(TrJunctionNode.ShapeIndex);
            MainRoute = TS.MainRoute;
        }

        public override void PrepareFrame(RenderFrame frame, long elapsedTime)
        {
            // ie, with 2 frames of animation, the key will advance from 0 to 1
            if (TrJunctionNode.SelectedRoute == MainRoute)
            {
                if (AnimationKey > 0.001) AnimationKey -= 2f * (float)elapsedTime;
                if (AnimationKey < 0.001) AnimationKey = 0;
            }
            else
            {
                if (AnimationKey < 0.999) AnimationKey += 2f * (float)elapsedTime;
                if (AnimationKey > 0.999) AnimationKey = 1.0f;
            }

            // Update the pose
            for (int iMatrix = 0; iMatrix < SharedShape.Matrices.Length; ++iMatrix)
                AnimateMatrix(iMatrix, AnimationKey);

            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);
        }
    }

    public class ShapePrimitive : RenderPrimitive
    {
        public Material Material { get; protected set; }
        public int[] Hierarchy { get; protected set; } // jerarquía de sub-objetos
        public int HierarchyIndex { get; protected set; } // Índice en el array de jerarquía que provee la posición para esta primitiva

        protected internal VertexBuffer VertexBuffer;
        protected VertexDeclaration VertexDeclaration;
        protected int VertexBufferStride;
        protected internal IndexBuffer IndexBuffer;
        protected internal int PrimitiveCount;

        readonly VertexBufferBinding[] VertexBufferBindings;

        public ShapePrimitive()
        {
        }

        public ShapePrimitive(Material material, SharedShape.VertexBufferSet vertexBufferSet, IndexBuffer indexBuffer, int primitiveCount, int[] hierarchy, int hierarchyIndex)
        {
            Material = material;
            VertexBuffer = vertexBufferSet.Buffer;
            IndexBuffer = indexBuffer;
            PrimitiveCount = primitiveCount;
            Hierarchy = hierarchy;
            HierarchyIndex = hierarchyIndex;

            VertexBufferBindings = new[] { new VertexBufferBinding(VertexBuffer), new VertexBufferBinding(GetDummyVertexBuffer(material.Viewer.GraphicsDevice)) };
        }

        public ShapePrimitive(Material material, SharedShape.VertexBufferSet vertexBufferSet, IList<ushort> indexData, GraphicsDevice graphicsDevice, int[] hierarchy, int hierarchyIndex)
            : this(material, vertexBufferSet, null, indexData.Count / 3, hierarchy, hierarchyIndex)
        {
            IndexBuffer = new IndexBuffer(graphicsDevice, typeof(short), indexData.Count, BufferUsage.WriteOnly);
            IndexBuffer.SetData(indexData.ToArray());
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (PrimitiveCount > 0)
            {
                // TODO considerar ordenar por conjunto de vértices para reducir el número de SetSources requeridas.
                graphicsDevice.SetVertexBuffers(VertexBufferBindings);
                graphicsDevice.Indices = IndexBuffer;
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseVertex: 0, startIndex: 0, primitiveCount: PrimitiveCount);
            }
        }

        [CallOnThread("Loader")]
        public virtual void Mark()
        {
            Material.Mark();
        }
    }

    /// Una <c>ShapePrimitive</c> que permite la manipulación de los buffers de vértices e índices para
    /// cambiar la geometría eficientemente.
    /// También permite cambiar el material.
    /// </summary>
    public class MutableShapePrimitive : ShapePrimitive
    {
        /// <remarks>
        /// No se pueden hacer más grandes los buffers, de forma que hay que tener cuidado 
        /// para asignar el valor de <paramref name="maxVertices"/> y <paramref name="maxIndices"/>,
        /// que definen los tamaños máximos de los buffers de vértices e índices respectivamente.
        /// </remarks>
        public MutableShapePrimitive(Material material, int maxVertices, int maxIndices, int[] hierarchy, int hierarchyIndex)
            : base(material: material,
                   vertexBufferSet: new SharedShape.VertexBufferSet(new VertexPositionNormalTexture[maxVertices], material.Viewer.GraphicsDevice),
                   indexData: new ushort[maxIndices],
                   graphicsDevice: material.Viewer.GraphicsDevice,
                   hierarchy: hierarchy,
                   hierarchyIndex: hierarchyIndex)
        { }

        public void SetVertexData(VertexPositionNormalTexture[] data, int minVertexIndex, int numVertices, int primitiveCount)
        {
            VertexBuffer.SetData(data);
            PrimitiveCount = primitiveCount;
        }

        public void SetIndexData(short[] data)
        {
            IndexBuffer.SetData(data);
        }

        public void SetMaterial(Material material)
        {
            Material = material;
        }
    }

    struct ShapeInstanceData
    {
#pragma warning disable 0649
        public Matrix World;
#pragma warning restore 0649

        public static readonly VertexElement[] VertexElements = {
            new VertexElement(sizeof(float) * 0, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
            new VertexElement(sizeof(float) * 4, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2),
            new VertexElement(sizeof(float) * 8, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 3),
            new VertexElement(sizeof(float) * 12, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 4),
        };

        public static int SizeInBytes = sizeof(float) * 16;
    }

    public class ShapePrimitiveInstances : RenderPrimitive
    {
        public Material Material { get; protected set; }
        public int[] Hierarchy { get; protected set; } // jerarquía del sub-objeto
        public int HierarchyIndex { get; protected set; } // índice en el array de la jerarquía que proporciona la forma para esta primitiva
        public int SubObjectIndex { get; protected set; }

        protected VertexBuffer VertexBuffer;
        protected VertexDeclaration VertexDeclaration;
        protected int VertexBufferStride;
        protected IndexBuffer IndexBuffer;
        protected int PrimitiveCount;

        protected VertexBuffer InstanceBuffer;
        protected VertexDeclaration InstanceDeclaration;
        protected int InstanceBufferStride;
        protected int InstanceCount;

        readonly VertexBufferBinding[] VertexBufferBindings;

        internal ShapePrimitiveInstances(GraphicsDevice graphicsDevice, ShapePrimitive shapePrimitive, Matrix[] positions, int subObjectIndex)
        {
            Material = shapePrimitive.Material;
            Hierarchy = shapePrimitive.Hierarchy;
            HierarchyIndex = shapePrimitive.HierarchyIndex;
            SubObjectIndex = subObjectIndex;
            VertexBuffer = shapePrimitive.VertexBuffer;
            VertexDeclaration = shapePrimitive.VertexBuffer.VertexDeclaration;
            IndexBuffer = shapePrimitive.IndexBuffer;
            PrimitiveCount = shapePrimitive.PrimitiveCount;

            InstanceDeclaration = new VertexDeclaration(ShapeInstanceData.SizeInBytes, ShapeInstanceData.VertexElements);
            InstanceBuffer = new VertexBuffer(graphicsDevice, InstanceDeclaration, positions.Length, BufferUsage.WriteOnly);
            InstanceBuffer.SetData(positions);
            InstanceCount = positions.Length;

            VertexBufferBindings = new[] { new VertexBufferBinding(VertexBuffer), new VertexBufferBinding(InstanceBuffer, 0, 1) };
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.Indices = IndexBuffer;
            graphicsDevice.SetVertexBuffers(VertexBufferBindings);
            graphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, baseVertex: 0, startIndex: 0, PrimitiveCount, InstanceCount);
        }
    }

#if DEBUG_SHAPE_NORMALS
    public class ShapeDebugNormalsPrimitive : ShapePrimitive
    {
        public ShapeDebugNormalsPrimitive(Material material, SharedShape.VertexBufferSet vertexBufferSet, List<ushort> indexData, GraphicsDevice graphicsDevice, int[] hierarchy, int hierarchyIndex)
        {
            Material = material;
            VertexBuffer = vertexBufferSet.DebugNormalsBuffer;
            VertexDeclaration = vertexBufferSet.DebugNormalsDeclaration;
            VertexBufferStride = vertexBufferSet.DebugNormalsDeclaration.GetVertexStrideSize(0);
            var debugNormalsIndexBuffer = new List<ushort>(indexData.Count * SharedShape.VertexBufferSet.DebugNormalsVertexPerVertex);
            for (var i = 0; i < indexData.Count; i++)
                for (var j = 0; j < SharedShape.VertexBufferSet.DebugNormalsVertexPerVertex; j++)
                    debugNormalsIndexBuffer.Add((ushort)(indexData[i] * SharedShape.VertexBufferSet.DebugNormalsVertexPerVertex + j));
            IndexBuffer = new IndexBuffer(graphicsDevice, typeof(short), debugNormalsIndexBuffer.Count, BufferUsage.WriteOnly);
            IndexBuffer.SetData(debugNormalsIndexBuffer.ToArray());
            MinVertexIndex = indexData.Min() * SharedShape.VertexBufferSet.DebugNormalsVertexPerVertex;
            NumVerticies = (indexData.Max() - indexData.Min() + 1) * SharedShape.VertexBufferSet.DebugNormalsVertexPerVertex;
            PrimitiveCount = indexData.Count / 3 * SharedShape.VertexBufferSet.DebugNormalsVertexPerVertex;
            Hierarchy = hierarchy;
            HierarchyIndex = hierarchyIndex;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            if (PrimitiveCount > 0)
            {
                graphicsDevice.VertexDeclaration = VertexDeclaration;
                graphicsDevice.Vertices[0].SetSource(VertexBuffer, 0, VertexBufferStride);
                graphicsDevice.Indices = IndexBuffer;
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, MinVertexIndex, NumVerticies, 0, PrimitiveCount);
            }
        }

        [CallOnThread("Loader")]
        public virtual void Mark()
        {
            Material.Mark();
        }
    }
#endif

    public class SharedShape
    {
        static List<string> ShapeWarnings = new List<string>();

        // Esta información es común a todas las instancias de la forma
        public List<string> MatrixNames = new List<string>();
        public Matrix[] Matrices = new Matrix[0];  // la forma natural y original para esta shape - compartida por todas sus instancias
        public animations Animations;
        public LodControl[] LodControls;
        public bool HasNightSubObj;
        public int RootSubObjectIndex = 0;
        //public bool negativeBogie = false;
        public string SoundFileName = "";
        public float BellAnimationFPS = 8;

        readonly Viewer Viewer;
        public readonly string FilePath;
        public readonly string ReferencePath;

        /// <summary>
        /// Crea una shape vacía que se usa cuando no ha podido cargar la buena
        /// </summary>
        /// <param name="viewer"></param>
        public SharedShape(Viewer viewer)
        {
            Viewer = viewer;
            FilePath = "Empty";
            LodControls = new LodControl[0];
        }

        /// <summary>
        /// Shape MSTS desde un archivo
        /// </summary>
        /// <param name="viewer"></param>
        /// <param name="filePath">Ruta del archivo S de la shape</param>
        public SharedShape(Viewer viewer, string filePath)
        {
            Viewer = viewer;
            FilePath = filePath;
            if (filePath.Contains('\0'))
            {
                var parts = filePath.Split('\0');
                FilePath = parts[0];
                ReferencePath = parts[1];
            }
            LoadContent();
        }

        /// <summary>
        /// Solo se carga una copia del modelo independientemente de cuantas copias aparezcan en la escena.
        /// </summary>
        void LoadContent()
        {
            Trace.Write("S");
            var filePath = FilePath;
            // las líneas comentadas permiten permiten leer un bloqe de animación desde un archivo adicional
            // en un subdirectorio Openrails
            //           string dir = Path.GetDirectoryName(filePath);
            //            string file = Path.GetFileName(filePath);
            //            string orFilePath = dir + @"\openrails\" + file;
            //var sFile = new ShapeFile(filePath, Viewer.Settings.SuppressShapeWarnings);
            var sFile = new ShapeFile(filePath, false);
            //            if (file.ToLower().Contains("turntable") && File.Exists(orFilePath))
            //            {
            //                sFile.ReadAnimationBlock(orFilePath);
            //            }

            var textureFlags = Helpers.TextureFlags.None;
            if (File.Exists(FilePath + "d"))
            {
                var sdFile = new ShapeDescriptorFile(FilePath + "d");
                //textureFlags = (Helpers.TextureFlags)sdFile.shape.ESD_Alternative_Texture;
                //if (FilePath != null && FilePath.Contains("\\global\\")) textureFlags |= Helpers.TextureFlags.SnowTrack;//roads and tracks are in global, as MSTS will always use snow texture in snow weather
                HasNightSubObj = sdFile.shape.ESD_SubObj;
                //if ((textureFlags & Helpers.TextureFlags.Night) != 0 && FilePath.Contains("\\trainset\\"))
                //    textureFlags |= Helpers.TextureFlags.Underground;
                //SoundFileName = sdFile.shape.ESD_SoundFileName;
                BellAnimationFPS = sdFile.shape.ESD_BellAnimationFPS;
            }

            var matrixCount = sFile.shape.matrices.Count;
            MatrixNames.Capacity = matrixCount;
            Matrices = new Matrix[matrixCount];
            for (var i = 0; i < matrixCount; ++i)
            {
                MatrixNames.Add(sFile.shape.matrices[i].Name.ToUpper());
                Matrices[i] = XNAMatrixFromMSTS(sFile.shape.matrices[i]);
            }
            Animations = sFile.shape.animations;

#if DEBUG_SHAPE_HIERARCHY
            var debugShapeHierarchy = new StringBuilder();
            debugShapeHierarchy.AppendFormat("Shape {0}:\n", Path.GetFileNameWithoutExtension(FilePath).ToUpper());
            for (var i = 0; i < MatrixNames.Count; ++i)
                debugShapeHierarchy.AppendFormat("  Matrix {0,-2}: {1}\n", i, MatrixNames[i]);
            for (var i = 0; i < sFile.shape.prim_states.Count; ++i)
                debugShapeHierarchy.AppendFormat("  PState {0,-2}: flags={1,-8:X8} shader={2,-15} alpha={3,-2} vstate={4,-2} lstate={5,-2} zbias={6,-5:F3} zbuffer={7,-2} name={8}\n", i, sFile.shape.prim_states[i].flags, sFile.shape.shader_names[sFile.shape.prim_states[i].ishader], sFile.shape.prim_states[i].alphatestmode, sFile.shape.prim_states[i].ivtx_state, sFile.shape.prim_states[i].LightCfgIdx, sFile.shape.prim_states[i].ZBias, sFile.shape.prim_states[i].ZBufMode, sFile.shape.prim_states[i].Name);
            for (var i = 0; i < sFile.shape.vtx_states.Count; ++i)
                debugShapeHierarchy.AppendFormat("  VState {0,-2}: flags={1,-8:X8} lflags={2,-8:X8} lstate={3,-2} material={4,-3} matrix2={5,-2}\n", i, sFile.shape.vtx_states[i].flags, sFile.shape.vtx_states[i].LightFlags, sFile.shape.vtx_states[i].LightCfgIdx, sFile.shape.vtx_states[i].LightMatIdx, sFile.shape.vtx_states[i].Matrix2);
            for (var i = 0; i < sFile.shape.light_model_cfgs.Count; ++i)
            {
                debugShapeHierarchy.AppendFormat("  LState {0,-2}: flags={1,-8:X8} uv_ops={2,-2}\n", i, sFile.shape.light_model_cfgs[i].flags, sFile.shape.light_model_cfgs[i].uv_ops.Count);
                for (var j = 0; j < sFile.shape.light_model_cfgs[i].uv_ops.Count; ++j)
                    debugShapeHierarchy.AppendFormat("    UV OP {0,-2}: texture_address_mode={1,-2}\n", j, sFile.shape.light_model_cfgs[i].uv_ops[j].TexAddrMode);
            }
            Console.Write(debugShapeHierarchy.ToString());
#endif
            LodControls = (from lod_control lod in sFile.shape.lod_controls
                           select new LodControl(lod, sFile, this)).ToArray();
            if (LodControls.Length == 0)
                throw new InvalidDataException("Shape file missing lod_control section");
            else if (LodControls[0].DistanceLevels.Length > 0 && LodControls[0].DistanceLevels[0].SubObjects.Length > 0)
            {
                // Pone a cero el offset de posición de la matriz raíz para compatibilidad con MSTS
                if (LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives.Length > 0 && LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives[0].Hierarchy[0] == -1)
                {
                    Matrices[0].M41 = 0;
                    Matrices[0].M42 = 0;
                    Matrices[0].M43 = 0;
                }
                // Busca el sub-objeto raíz. No es necesariamente el primero. Hay excepciones.
                for (int soIndex = 0; soIndex <= LodControls[0].DistanceLevels[0].SubObjects.Length - 1; soIndex++)
                {
                    sub_object subObject = sFile.shape.lod_controls[0].distance_levels[0].sub_objects[soIndex];
                    if (subObject.sub_object_header.geometry_info.geometry_node_map[0] == 0)
                    {
                        RootSubObjectIndex = soIndex;
                        break;
                    }
                }
            }
        }

        public class LodControl
        {
            public DistanceLevel[] DistanceLevels;

            //public LodControl(lod_control MSTSlod_control, Helpers.TextureFlags textureFlags, ShapeFile sFile, SharedShape sharedShape)
            public LodControl(lod_control MSTSlod_control, ShapeFile sFile, SharedShape sharedShape)
            {
#if DEBUG_SHAPE_HIERARCHY
                Console.WriteLine("  LOD control:");
#endif
                DistanceLevels = (from distance_level level in MSTSlod_control.distance_levels
                                  select new DistanceLevel(level, sFile, sharedShape)).ToArray();
                if (DistanceLevels.Length == 0)
                    throw new InvalidDataException("Shape file missing distance_level");
            }

            [CallOnThread("Loader")]
            internal void Mark()
            {
                foreach (var dl in DistanceLevels)
                    dl.Mark();
            }
        }

        public class DistanceLevel
        {
            public float ViewingDistance;
            public float ViewSphereRadius;
            public SubObject[] SubObjects;

            //public DistanceLevel(distance_level MSTSdistance_level, Helpers.TextureFlags textureFlags, ShapeFile sFile, SharedShape sharedShape)
            public DistanceLevel(distance_level MSTSdistance_level,  ShapeFile sFile, SharedShape sharedShape)
            {
#if DEBUG_SHAPE_HIERARCHY
                Console.WriteLine("    Distance level {0}: hierarchy={1}", MSTSdistance_level.distance_level_header.dlevel_selection, String.Join(" ", MSTSdistance_level.distance_level_header.hierarchy.Select(i => i.ToString()).ToArray()));
#endif
                ViewingDistance = MSTSdistance_level.distance_level_header.dlevel_selection;
                // TODO, work out ViewShereRadius from all sub_object radius and centers.
                if (sFile.shape.volumes.Count > 0)
                    ViewSphereRadius = sFile.shape.volumes[0].Radius;
                else
                    ViewSphereRadius = 100;

                var index = 0;
#if DEBUG_SHAPE_HIERARCHY
                var subObjectIndex = 0;
                SubObjects = (from sub_object obj in MSTSdistance_level.sub_objects
                              select new SubObject(obj, ref index, MSTSdistance_level.distance_level_header.hierarchy, textureFlags, subObjectIndex++, sFile, sharedShape)).ToArray();
#else
                SubObjects = (from sub_object obj in MSTSdistance_level.sub_objects
                                  //select new SubObject(obj, ref index, MSTSdistance_level.distance_level_header.hierarchy, textureFlags, sFile, sharedShape)).ToArray();
                              select new SubObject(obj, ref index, MSTSdistance_level.distance_level_header.hierarchy, sFile, sharedShape)).ToArray();
#endif
                if (SubObjects.Length == 0)
                    throw new InvalidDataException("Shape file missing sub_object");
            }

            [CallOnThread("Loader")]
            internal void Mark()
            {
                foreach (var so in SubObjects)
                    so.Mark();
            }
        }

        public class SubObject
        {
            static readonly SceneryMaterialOptions[] UVTextureAddressModeMap = new[] {
                SceneryMaterialOptions.TextureAddressModeWrap,
                SceneryMaterialOptions.TextureAddressModeMirror,
                SceneryMaterialOptions.TextureAddressModeClamp,
                SceneryMaterialOptions.TextureAddressModeBorder,
            };

            static readonly Dictionary<string, SceneryMaterialOptions> ShaderNames = new Dictionary<string, SceneryMaterialOptions> {
                { "Tex", SceneryMaterialOptions.ShaderFullBright },
                { "TexDiff", SceneryMaterialOptions.Diffuse },
                { "BlendATex", SceneryMaterialOptions.AlphaBlendingBlend | SceneryMaterialOptions.ShaderFullBright},
                { "BlendATexDiff", SceneryMaterialOptions.AlphaBlendingBlend | SceneryMaterialOptions.Diffuse },
                { "AddATex", SceneryMaterialOptions.AlphaBlendingAdd | SceneryMaterialOptions.ShaderFullBright},
                { "AddATexDiff", SceneryMaterialOptions.AlphaBlendingAdd | SceneryMaterialOptions.Diffuse },
            };

            static readonly SceneryMaterialOptions[] VertexLightModeMap = new[] {
                SceneryMaterialOptions.ShaderDarkShade,
                SceneryMaterialOptions.ShaderHalfBright,
                SceneryMaterialOptions.ShaderVegetation, // Not certain this is right.
                SceneryMaterialOptions.ShaderVegetation,
                SceneryMaterialOptions.ShaderFullBright,
                SceneryMaterialOptions.None | SceneryMaterialOptions.Specular750,
                SceneryMaterialOptions.None | SceneryMaterialOptions.Specular25,
                SceneryMaterialOptions.None | SceneryMaterialOptions.None,
            };

            public ShapePrimitive[] ShapePrimitives;

#if DEBUG_SHAPE_HIERARCHY
            public SubObject(sub_object sub_object, ref int totalPrimitiveIndex, int[] hierarchy, Helpers.TextureFlags textureFlags, int subObjectIndex, SFile sFile, SharedShape sharedShape)
#else
            public SubObject(sub_object sub_object, ref int totalPrimitiveIndex, int[] hierarchy,  ShapeFile sFile, SharedShape sharedShape)
#endif
            {
#if DEBUG_SHAPE_HIERARCHY
                var debugShapeHierarchy = new StringBuilder();
                debugShapeHierarchy.AppendFormat("      Sub object {0}:\n", subObjectIndex);
#endif
                var vertexBufferSet = new VertexBufferSet(sub_object, sFile, sharedShape.Viewer.GraphicsDevice);
#if DEBUG_SHAPE_NORMALS
                var debugNormalsMaterial = sharedShape.Viewer.MaterialManager.Load("DebugNormals");
#endif

#if OPTIMIZE_SHAPES_ON_LOAD
                var primitiveMaterials = sub_object.primitives.Cast<primitive>().Select((primitive) =>
#else
                var primitiveIndex = 0;
#if DEBUG_SHAPE_NORMALS
                ShapePrimitives = new ShapePrimitive[sub_object.primitives.Count * 2];
#else
                ShapePrimitives = new ShapePrimitive[sub_object.primitives.Count];
#endif
                foreach (primitive primitive in sub_object.primitives)
#endif
                {
                    var primitiveState = sFile.shape.prim_states[primitive.prim_state_idx];
                    var vertexState = sFile.shape.vtx_states[primitiveState.ivtx_state];
                    var lightModelConfiguration = sFile.shape.light_model_cfgs[vertexState.LightCfgIdx];
                    var options = SceneryMaterialOptions.None;

                    // Validate hierarchy position.
                    var hierarchyIndex = vertexState.imatrix;
                    while (hierarchyIndex != -1)
                    {
                        if (hierarchyIndex < 0 || hierarchyIndex >= hierarchy.Length)
                        {
                            var hierarchyList = new List<int>();
                            hierarchyIndex = vertexState.imatrix;
                            while (hierarchyIndex >= 0 && hierarchyIndex < hierarchy.Length)
                            {
                                hierarchyList.Add(hierarchyIndex);
                                hierarchyIndex = hierarchy[hierarchyIndex];
                            }
                            hierarchyList.Add(hierarchyIndex);
                            Trace.TraceWarning("Ignored invalid primitive hierarchy {1} in shape {0}", sharedShape.FilePath, String.Join(" ", hierarchyList.Select(hi => hi.ToString()).ToArray()));
                            break;
                        }
                        hierarchyIndex = hierarchy[hierarchyIndex];
                    }

                    if (lightModelConfiguration.uv_ops.Count > 0)
                        if (lightModelConfiguration.uv_ops[0].TexAddrMode - 1 >= 0 && lightModelConfiguration.uv_ops[0].TexAddrMode - 1 < UVTextureAddressModeMap.Length)
                            options |= UVTextureAddressModeMap[lightModelConfiguration.uv_ops[0].TexAddrMode - 1];
                        else if (!ShapeWarnings.Contains("texture_addressing_mode:" + lightModelConfiguration.uv_ops[0].TexAddrMode))
                        {
                            Trace.TraceInformation("Skipped unknown texture addressing mode {1} first seen in shape {0}", sharedShape.FilePath, lightModelConfiguration.uv_ops[0].TexAddrMode);
                            ShapeWarnings.Add("texture_addressing_mode:" + lightModelConfiguration.uv_ops[0].TexAddrMode);
                        }

                    if (primitiveState.alphatestmode == 1)
                        options |= SceneryMaterialOptions.AlphaTest;

                    if (ShaderNames.ContainsKey(sFile.shape.shader_names[primitiveState.ishader]))
                        options |= ShaderNames[sFile.shape.shader_names[primitiveState.ishader]];
                    else if (!ShapeWarnings.Contains("shader_name:" + sFile.shape.shader_names[primitiveState.ishader]))
                    {
                        Trace.TraceInformation("Skipped unknown shader name {1} first seen in shape {0}", sharedShape.FilePath, sFile.shape.shader_names[primitiveState.ishader]);
                        ShapeWarnings.Add("shader_name:" + sFile.shape.shader_names[primitiveState.ishader]);
                    }

                    if (12 + vertexState.LightMatIdx >= 0 && 12 + vertexState.LightMatIdx < VertexLightModeMap.Length)
                        options |= VertexLightModeMap[12 + vertexState.LightMatIdx];
                    else if (!ShapeWarnings.Contains("lighting_model:" + vertexState.LightMatIdx))
                    {
                        Trace.TraceInformation("Skipped unknown lighting model index {1} first seen in shape {0}", sharedShape.FilePath, vertexState.LightMatIdx);
                        ShapeWarnings.Add("lighting_model:" + vertexState.LightMatIdx);
                    }

                    //if ((textureFlags & Helpers.TextureFlags.Night) != 0)
                    //    options |= SceneryMaterialOptions.NightTexture;

                    //if ((textureFlags & Helpers.TextureFlags.Underground) != 0)
                    //    options |= SceneryMaterialOptions.UndergroundTexture;

                    Material material;
                    if (primitiveState.tex_idxs.Length != 0)
                    {
                        var texture = sFile.shape.textures[primitiveState.tex_idxs[0]];
                        var imageName = sFile.shape.images[texture.iImage];
                        //if (String.IsNullOrEmpty(sharedShape.ReferencePath))
                        //    material = sharedShape.Viewer.MaterialManager.Load("Scenery", Helpers.GetRouteTextureFile(sharedShape.Viewer.Simulator, textureFlags, imageName), (int)options, texture.MipMapLODBias);
                        //else
                        //    material = sharedShape.Viewer.MaterialManager.Load("Scenery", Helpers.GetTextureFile(sharedShape.Viewer.Simulator, textureFlags, sharedShape.ReferencePath, imageName), (int)options, texture.MipMapLODBias);
                        if (String.IsNullOrEmpty(sharedShape.ReferencePath))
                            material = sharedShape.Viewer.MaterialManager.Load("Scenery", Helpers.GetRouteTextureFile(sharedShape.Viewer.microSim, 0, imageName), (int)options, texture.MipMapLODBias);
                        else
                            material = sharedShape.Viewer.MaterialManager.Load("Scenery", Helpers.GetTextureFile(sharedShape.Viewer.microSim, 0, sharedShape.ReferencePath, imageName), (int)options, texture.MipMapLODBias);

                    }
                    else
                    {
                        material = sharedShape.Viewer.MaterialManager.Load("Scenery", null, (int)options);
                    }

#if DEBUG_SHAPE_HIERARCHY
                    debugShapeHierarchy.AppendFormat("        Primitive {0,-2}: pstate={1,-2} vstate={2,-2} lstate={3,-2} matrix={4,-2}", primitiveIndex, primitive.prim_state_idx, primitiveState.ivtx_state, vertexState.LightCfgIdx, vertexState.imatrix);
                    var debugMatrix = vertexState.imatrix;
                    while (debugMatrix >= 0)
                    {
                        debugShapeHierarchy.AppendFormat(" {0}", sharedShape.MatrixNames[debugMatrix]);
                        debugMatrix = hierarchy[debugMatrix];
                    }
                    debugShapeHierarchy.Append("\n");
#endif

#if OPTIMIZE_SHAPES_ON_LOAD
                    return new { Key = material.ToString() + "/" + vertexState.imatrix.ToString(), Primitive = primitive, Material = material, HierachyIndex = vertexState.imatrix };
                }).ToArray();
#else
                    if (primitive.indexed_trilist.vertex_idxs.Count == 0)
                    {
                        Trace.TraceWarning("Skipped primitive with 0 indices in {0}", sharedShape.FilePath);
                        continue;
                    }

                    var indexData = new List<ushort>(primitive.indexed_trilist.vertex_idxs.Count * 3);
                    foreach (vertex_idx vertex_idx in primitive.indexed_trilist.vertex_idxs)
                        foreach (var index in new[] { vertex_idx.a, vertex_idx.b, vertex_idx.c })
                            indexData.Add((ushort)index);

                    ShapePrimitives[primitiveIndex] = new ShapePrimitive(material, vertexBufferSet, indexData, sharedShape.Viewer.GraphicsDevice, hierarchy, vertexState.imatrix);
                    ShapePrimitives[primitiveIndex].SortIndex = ++totalPrimitiveIndex;
                    ++primitiveIndex;
#if DEBUG_SHAPE_NORMALS
                    ShapePrimitives[primitiveIndex] = new ShapeDebugNormalsPrimitive(debugNormalsMaterial, vertexBufferSet, indexData, sharedShape.Viewer.GraphicsDevice, hierarchy, vertexState.imatrix);
                    ShapePrimitives[primitiveIndex].SortIndex = totalPrimitiveIndex;
                    ++primitiveIndex;
#endif
                }
#endif

#if OPTIMIZE_SHAPES_ON_LOAD
                var indexes = new Dictionary<string, List<short>>(sub_object.primitives.Count);
                foreach (var primitiveMaterial in primitiveMaterials)
                {
                    var baseIndex = 0;
                    var indexData = new List<short>(0);
                    if (indexes.TryGetValue(primitiveMaterial.Key, out indexData))
                    {
                        baseIndex = indexData.Count;
                        indexData.Capacity += primitiveMaterial.Primitive.indexed_trilist.vertex_idxs.Count * 3;
                    }
                    else
                    {
                        indexData = new List<short>(primitiveMaterial.Primitive.indexed_trilist.vertex_idxs.Count * 3);
                        indexes.Add(primitiveMaterial.Key, indexData);
                    }

                    var primitiveState = sFile.shape.prim_states[primitiveMaterial.Primitive.prim_state_idx];
                    foreach (vertex_idx vertex_idx in primitiveMaterial.Primitive.indexed_trilist.vertex_idxs)
                    {
                        indexData.Add((short)vertex_idx.a);
                        indexData.Add((short)vertex_idx.b);
                        indexData.Add((short)vertex_idx.c);
                    }
                }

                ShapePrimitives = new ShapePrimitive[indexes.Count];
                var primitiveIndex = 0;
                foreach (var index in indexes)
                {
                    var indexBuffer = new IndexBuffer(sharedShape.Viewer.GraphicsDevice, typeof(short), index.Value.Count, BufferUsage.WriteOnly);
                    indexBuffer.SetData(index.Value.ToArray());
                    var primitiveMaterial = primitiveMaterials.First(d => d.Key == index.Key);
                    ShapePrimitives[primitiveIndex] = new ShapePrimitive(primitiveMaterial.Material, vertexBufferSet, indexBuffer, index.Value.Min(), index.Value.Max() - index.Value.Min() + 1, index.Value.Count / 3, hierarchy, primitiveMaterial.HierachyIndex);
                    ++primitiveIndex;
                }
                if (sub_object.primitives.Count != indexes.Count)
                    Trace.TraceInformation("{1} -> {2} primitives in {0}", sharedShape.FilePath, sub_object.primitives.Count, indexes.Count);
#else
                if (primitiveIndex < ShapePrimitives.Length)
                    ShapePrimitives = ShapePrimitives.Take(primitiveIndex).ToArray();
#endif

#if DEBUG_SHAPE_HIERARCHY
                Console.Write(debugShapeHierarchy.ToString());
#endif
            }

            [CallOnThread("Loader")]
            internal void Mark()
            {
                foreach (var prim in ShapePrimitives)
                    prim.Mark();
            }
        }

        public class VertexBufferSet
        {
            public VertexBuffer Buffer;

#if DEBUG_SHAPE_NORMALS
            public VertexBuffer DebugNormalsBuffer;
            public VertexDeclaration DebugNormalsDeclaration;
            public int DebugNormalsVertexCount;
            public const int DebugNormalsVertexPerVertex = 3 * 4;
#endif

            public VertexBufferSet(VertexPositionNormalTexture[] vertexData, GraphicsDevice graphicsDevice)
            {
                Buffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture), vertexData.Length, BufferUsage.WriteOnly);
                Buffer.SetData(vertexData);
            }

#if DEBUG_SHAPE_NORMALS
            public VertexBufferSet(VertexPositionNormalTexture[] vertexData, VertexPositionColor[] debugNormalsVertexData, GraphicsDevice graphicsDevice)
                :this(vertexData, graphicsDevice)
            {
                DebugNormalsVertexCount = debugNormalsVertexData.Length;
                DebugNormalsDeclaration = new VertexDeclaration(graphicsDevice, VertexPositionColor.VertexElements);
                DebugNormalsBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor), DebugNormalsVertexCount, BufferUsage.WriteOnly);
                DebugNormalsBuffer.SetData(debugNormalsVertexData);
            }
#endif

            public VertexBufferSet(sub_object sub_object, ShapeFile sFile, GraphicsDevice graphicsDevice)
#if DEBUG_SHAPE_NORMALS
                : this(CreateVertexData(sub_object, sFile.shape), CreateDebugNormalsVertexData(sub_object, sFile.shape), graphicsDevice)
#else
                : this(CreateVertexData(sub_object, sFile.shape), graphicsDevice)
#endif
            {
            }

            static VertexPositionNormalTexture[] CreateVertexData(sub_object sub_object, shape shape)
            {
                // TODO - deal with vertex sets that have various numbers of texture coordinates - ie 0, 1, 2 etc
                return (from vertex vertex in sub_object.vertices
                        select XNAVertexPositionNormalTextureFromMSTS(vertex, shape)).ToArray();
            }

            static VertexPositionNormalTexture XNAVertexPositionNormalTextureFromMSTS(vertex vertex, shape shape)
            {
                var position = shape.points[vertex.ipoint];
                var normal = shape.normals[vertex.inormal];
                // TODO use a simpler vertex description when no UV's in use
                var texcoord = vertex.vertex_uvs.Length > 0 ? shape.uv_points[vertex.vertex_uvs[0]] : new uv_point(0, 0);

                return new VertexPositionNormalTexture()
                {
                    Position = new Vector3(position.X, position.Y, -position.Z),
                    Normal = new Vector3(normal.X, normal.Y, -normal.Z),
                    TextureCoordinate = new Vector2(texcoord.U, texcoord.V),
                };
            }

#if DEBUG_SHAPE_NORMALS
            static VertexPositionColor[] CreateDebugNormalsVertexData(sub_object sub_object, shape shape)
            {
                var vertexData = new List<VertexPositionColor>();
                foreach (vertex vertex in sub_object.vertices)
                {
                    var position = new Vector3(shape.points[vertex.ipoint].X, shape.points[vertex.ipoint].Y, -shape.points[vertex.ipoint].Z);
                    var normal = new Vector3(shape.normals[vertex.inormal].X, shape.normals[vertex.inormal].Y, -shape.normals[vertex.inormal].Z);
                    var right = Vector3.Cross(normal, Math.Abs(normal.Y) > 0.5 ? Vector3.Left : Vector3.Up);
                    var up = Vector3.Cross(normal, right);
                    right /= 50;
                    up /= 50;
                    vertexData.Add(new VertexPositionColor(position + right, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position + normal, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position + up, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position + up, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position + normal, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position - right, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position - right, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position + normal, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position - up, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position - up, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position + normal, Color.LightGreen));
                    vertexData.Add(new VertexPositionColor(position + right, Color.LightGreen));
                }
                return vertexData.ToArray();
            }
#endif
        }

        static Matrix XNAMatrixFromMSTS(matrix MSTSMatrix)
        {
            var XNAMatrix = Matrix.Identity;

            XNAMatrix.M11 = MSTSMatrix.AX;
            XNAMatrix.M12 = MSTSMatrix.AY;
            XNAMatrix.M13 = -MSTSMatrix.AZ;
            XNAMatrix.M21 = MSTSMatrix.BX;
            XNAMatrix.M22 = MSTSMatrix.BY;
            XNAMatrix.M23 = -MSTSMatrix.BZ;
            XNAMatrix.M31 = -MSTSMatrix.CX;
            XNAMatrix.M32 = -MSTSMatrix.CY;
            XNAMatrix.M33 = MSTSMatrix.CZ;
            XNAMatrix.M41 = MSTSMatrix.DX;
            XNAMatrix.M42 = MSTSMatrix.DY;
            XNAMatrix.M43 = -MSTSMatrix.DZ;

            return XNAMatrix;
        }

        public void PrepareFrame(RenderFrame frame, WorldPosition location, ShapeFlags flags)
        {
            PrepareFrame(frame, location, Matrices, null, flags);
        }

        public void PrepareFrame(RenderFrame frame, WorldPosition location, Matrix[] animatedXNAMatrices, ShapeFlags flags)
        {
            PrepareFrame(frame, location, animatedXNAMatrices, null, flags);
        }

        public void PrepareFrame(RenderFrame frame, WorldPosition location, Matrix[] animatedXNAMatrices, bool[] subObjVisible, ShapeFlags flags)
        {
            var lodBias = Viewer.Game.LODBias / 100 + 1;

            // Locate relative to the camera
            //var dTileX = location.TileX - Viewer.Camera.TileX;
            //var dTileZ = location.TileZ - Viewer.Camera.TileZ;
            var mstsLocation = location.Location;
            //mstsLocation.X += dTileX * 2048;
            //mstsLocation.Z += dTileZ * 2048;
            var xnaDTileTranslation = location.XNAMatrix;
            //xnaDTileTranslation.M41 += dTileX * 2048;
            //xnaDTileTranslation.M43 -= dTileZ * 2048;

            foreach (var lodControl in LodControls)
            {
                // Start with the furthest away distance, then look for a nearer one in range of the camera.
                var displayDetailLevel = lodControl.DistanceLevels.Length - 1;

                // If this LOD group is not in the FOV, skip the whole LOD group.
                // TODO: This might imair some shadows.
                if (!Viewer.Camera.InFov(mstsLocation, lodControl.DistanceLevels[displayDetailLevel].ViewSphereRadius))
                    continue;

                // We choose the distance level (LOD) to display first:
                //   - LODBias = 100 means we always use the highest detail.
                //   - LODBias < 100 means we operate as normal (using the highest detail in-range of the camera) but
                //     scaling it by LODBias.
                //
                // However, for the viewing distance (and view sphere), we use a slightly different calculation:
                //   - LODBias = 100 means we always use the *lowest* detail viewing distance.
                //   - LODBias < 100 means we operate as normal (see above).
                //
                // The reason for this disparity is that LODBias = 100 is special, because it means "always use
                // highest detail", but this by itself is not useful unless we keep using the normal (LODBias-scaled)
                // viewing distance - right down to the lowest detail viewing distance. Otherwise, we'll scale the
                // highest detail viewing distance up by 100% and then the object will just disappear!

                if (100 == Viewer.Game.LODBias) //Detalle máximo
                    displayDetailLevel = 0;
                else if (Viewer.Game.LODBias > -100) //No es el nivel mínimo de detalle. Para encontrar el nivel correcto hay que escalar por LODBias.
                    while ((displayDetailLevel > 0) && Viewer.Camera.InRange(mstsLocation, lodControl.DistanceLevels[displayDetailLevel - 1].ViewSphereRadius, lodControl.DistanceLevels[displayDetailLevel - 1].ViewingDistance * lodBias))
                        displayDetailLevel--;

                var displayDetail = lodControl.DistanceLevels[displayDetailLevel];
                var distanceDetail = 100 == Viewer.Game.LODBias
                    ? lodControl.DistanceLevels[lodControl.DistanceLevels.Length - 1]
                    : displayDetail;

                // If set, extend the lowest LOD to the maximum viewing distance.
                if (Viewer.Game.LODViewingExtention && displayDetailLevel == lodControl.DistanceLevels.Length - 1)
                    distanceDetail.ViewingDistance = float.MaxValue;

                if (displayDetailLevel == lodControl.DistanceLevels.Length - 1)
                    distanceDetail.ViewingDistance = float.MaxValue;

                for (var i = 0; i < displayDetail.SubObjects.Length; i++)
                {
                    var subObject = displayDetail.SubObjects[i];

                    // The 1st subobject (note that index 0 is the main object itself) is hidden during the day if HasNightSubObj is true.
                    if ((subObjVisible != null && !subObjVisible[i]) || (i == 1 && HasNightSubObj && Viewer.MaterialManager.sunDirection.Y >= 0))
                        continue;

                    foreach (var shapePrimitive in subObject.ShapePrimitives)
                    {
                        var xnaMatrix = Matrix.Identity;
                        var hi = shapePrimitive.HierarchyIndex;
                        while (hi >= 0 && hi < shapePrimitive.Hierarchy.Length)
                        {
                            Matrix.Multiply(ref xnaMatrix, ref animatedXNAMatrices[hi], out xnaMatrix);
                            hi = shapePrimitive.Hierarchy[hi];
                        }
                        Matrix.Multiply(ref xnaMatrix, ref xnaDTileTranslation, out xnaMatrix);

                        // TODO make shadows depend on shape overrides

                        var interior = (flags & ShapeFlags.Interior) != 0;
                        frame.AddAutoPrimitive(mstsLocation, distanceDetail.ViewSphereRadius, distanceDetail.ViewingDistance * lodBias, shapePrimitive.Material, shapePrimitive, interior ? RenderPrimitiveGroup.Interior : RenderPrimitiveGroup.World, ref xnaMatrix, flags);
                    }
                }
            }
        }

        public Matrix GetMatrixProduct(int iNode)
        {
            int[] h = LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives[0].Hierarchy;
            Matrix matrix = Matrix.Identity;
            while (iNode != -1)
            {
                matrix *= Matrices[iNode];
                iNode = h[iNode];
            }
            return matrix;
        }

        public int GetParentMatrix(int iNode)
        {
            return LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives[0].Hierarchy[iNode];
        }

        [CallOnThread("Loader")]
        internal void Mark()
        {
            Viewer.ShapeManager.Mark(this);
            foreach (var lod in LodControls)
                lod.Mark();
        }
    }

    public class TrItemLabel
    {
        public readonly WorldPosition Location;
        public readonly string ItemName;

        /// <summary>
        /// Construye e inicia la clase.
        /// Este constructor es para las etiquetas de los elementos de vía en los archivos TDB y W como
        /// andenes y vías muertas.
        /// </summary>
        public TrItemLabel(Viewer viewer, WorldPosition position, TrObject trObj)
        {
            Location = position;
            var i = 0;
            while (true)
            {
                var trID = trObj.getTrItemID(i);
                if (trID < 0)
                    break;
                var trItem = viewer.microSim.TDB.TrackDB.TrItemTable[trID];
                if (trItem == null)
                    continue;
                ItemName = trItem.ItemName;
                i++;
            }
        }
    }
}
