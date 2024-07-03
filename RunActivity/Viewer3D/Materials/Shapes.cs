using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tourmaline.Formats.Msts;
using Tourmaline.Viewer3D.Common;
using TOURMALINE.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using static Tourmaline.Viewer3D.SharedShape;
using Tourmaline.Viewer3D.TvForms;
using Tourmaline.Viewer3D.Processes;

/// <summary>
/// Formas de malla para representar.
/// </summary>
namespace Tourmaline.Viewer3D.Materials
{
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

    internal class SharedShapeManager
    {
        readonly _3dTrainViewer Viewer;
        Dictionary<string, SharedShape> Shapes = new Dictionary<string, SharedShape>();
        SharedShape EmptyShape;
        internal SharedShapeManager(_3dTrainViewer viewer)
        {
            Viewer = viewer;
            EmptyShape = new SharedShape(Viewer);
        }
        public SharedShape Get(string path)
        {
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

    }
    internal class StaticShape
    {
        public readonly _3dTrainViewer Viewer;
        public readonly WorldPosition Location;
        public readonly Materials.ShapeFlags Flags;
        public readonly Materials.SharedShape SharedShape;

        /// <summary>
        /// Construye e inicializa la clase
        /// Este constructor es para los objetos descritos por un archivo shape de MSTS
        /// </summary>
        public StaticShape(_3dTrainViewer viewer, string path, WorldPosition position, ShapeFlags flags)
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
    }
    /// <summary>
    /// Esta shape tiene una jerarquía de objetos que se pueden mover a base
    /// de ajustar las matrices XNAMatrices en cada nodo.
    /// </summary>
    internal class PoseableShape : StaticShape
    {
        protected static Dictionary<string, bool> SeenShapeAnimationError = new Dictionary<string, bool>();

        public Matrix[] XNAMatrices = new Matrix[0];  // las posiciones de los sub-objetos

        public readonly int[] Hierarchy;

        public PoseableShape(_3dTrainViewer viewer, string path, WorldPosition initialPosition, ShapeFlags flags)
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

        public PoseableShape(_3dTrainViewer viewer, string path, WorldPosition initialPosition)
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
    /// 
    internal class AnimatedShape : PoseableShape
    {
        protected float AnimationKey;  // avanza con el tiempo
        protected float FrameRateMultiplier = 1; // por ejemplo, en la vista del pasajero MSTS divide por 30 el ratio de fotogramas (frame rate); este valor es el inverso

        /// <summary>
        /// Construye e inicia la clase
        /// </summary>
        public AnimatedShape(_3dTrainViewer viewer, string path, WorldPosition initialPosition, ShapeFlags flags, float frameRateDivisor = 1.0f)
            : base(viewer, path, initialPosition, flags)
        {
            FrameRateMultiplier = 1 / frameRateDivisor;
        }

        public override void PrepareFrame(RenderFrame frame, long elapsedTime)
        {
            // si la shape tiene animaciones...
            if (SharedShape.Animations?.Count > 0 && SharedShape.Animations[0].FrameCount > 0)
            {
                AnimationKey += SharedShape.Animations[0].FrameRate * (float)elapsedTime / 1000 * FrameRateMultiplier;
                while (AnimationKey > SharedShape.Animations[0].FrameCount) AnimationKey -= SharedShape.Animations[0].FrameCount;
                while (AnimationKey < 0) AnimationKey += SharedShape.Animations[0].FrameCount;

                // Actualiza la forma para cada matriz
                for (var matrix = 0; matrix < SharedShape.Matrices.Length; ++matrix)
                    AnimateMatrix(matrix, AnimationKey);
            }
            SharedShape.PrepareFrame(frame, Location, XNAMatrices, Flags);
        }
    }

    internal class SharedStaticShapeInstance
    {

    }



    internal class ShapePrimitive:RenderPrimitive
    {
        internal Materials.Material Material { get; set; }
        internal int[] Hierarchy { get; set; } // jerarquía de sub-objetos
        internal int HierarchyIndex { get;  set; } // Índice en el array de jerarquía que provee la posición para esta primitiva

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

            VertexBufferBindings = new[] { new VertexBufferBinding(VertexBuffer), new VertexBufferBinding(GetDummyVertexBuffer(material.Viewer.GControl.GraphicsDevice)) };
        }

        public ShapePrimitive(Material material, SharedShape.VertexBufferSet vertexBufferSet, IList<ushort> indexData, GraphicsDevice graphicsDevice, int[] hierarchy, int hierarchyIndex)
            : this(material, vertexBufferSet, null, indexData.Count / 3, hierarchy, hierarchyIndex)
        {
            IndexBuffer = new IndexBuffer(graphicsDevice, typeof(short), indexData.Count, BufferUsage.WriteOnly);
            IndexBuffer.SetData(indexData.ToArray());
        }

        internal override void Draw(GraphicsDevice graphicsDevice)
        {
            if (PrimitiveCount > 0)
            {
                // TODO considerar ordenar por conjunto de vértices para reducir el número de SetSources requeridas.
                graphicsDevice.SetVertexBuffers(VertexBufferBindings);
                graphicsDevice.Indices = IndexBuffer;
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, baseVertex: 0, startIndex: 0, primitiveCount: PrimitiveCount);
            }
        }
    }

    internal class SharedShape
    {
        static List<string> ShapeWarnings = new List<string>();
        // Esta información es común a todas las instancias de la forma
        public List<string> MatrixNames = new List<string>();
        public Matrix[] Matrices = new Matrix[0];  // la forma natural y original para esta shape - compartida por todas sus instancias
        public animations Animations;
        public LodControl[] LodControls;
        public bool HasNightSubObj;
        public int RootSubObjectIndex = 0;

        readonly _3dTrainViewer Viewer;
        internal readonly string FilePath;
        internal readonly string ReferencePath;

        /// <summary>
        /// Crea una shape vacía que se usa cuando no ha podido cargar la buena
        /// </summary>
        /// <param name="viewer"></param>
        internal SharedShape(_3dTrainViewer viewer)
        {
            this.Viewer = viewer;
            FilePath = "Empty";
            LodControls = new LodControl[0];
        }

        /// <summary>
        /// Shape MSTS desde un archivo
        /// </summary>
        /// <param name="viewer"></param>
        /// <param name="filePath">Ruta del archivo S de la shape</param>
        internal SharedShape(_3dTrainViewer viewer, string filePath)
        {
            this.Viewer = viewer;
            this.FilePath = filePath;
            if(filePath.Contains('\0'))
            {
                string[] parts = filePath.Split('\0');
                this.FilePath = parts[0];
                this.ReferencePath = parts[1];
            }
            loadContent();
        }

        /// <summary>
        /// Solo se carga una copia del modelo independientemente de cuantas copias aparezcan en la escena.
        /// </summary>
        void loadContent()
        {
            Trace.Write("S");
            var filePath = FilePath;
            var sFile = new ShapeFile(filePath, false);
            var textureFlags = Helpers.TextureFlags.None;
            if (File.Exists(FilePath + "d"))
            {
                var sdFile = new ShapeDescriptorFile(FilePath + "d");
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
            FirstLoadProcess config = FirstLoadProcess.Instance;

            var lodBias = config.LODBias / 100 + 1;

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

                if (100 == config.LODBias) //Detalle máximo
                    displayDetailLevel = 0;
                else if (config.LODBias > -100) //No es el nivel mínimo de detalle. Para encontrar el nivel correcto hay que escalar por LODBias.
                    while ((displayDetailLevel > 0) && Viewer.Camera.InRange(mstsLocation, lodControl.DistanceLevels[displayDetailLevel - 1].ViewSphereRadius, lodControl.DistanceLevels[displayDetailLevel - 1].ViewingDistance * lodBias))
                        displayDetailLevel--;

                var displayDetail = lodControl.DistanceLevels[displayDetailLevel];
                var distanceDetail = 100 == config.LODBias
                    ? lodControl.DistanceLevels[lodControl.DistanceLevels.Length - 1]
                    : displayDetail;

                // If set, extend the lowest LOD to the maximum viewing distance.
                if (config.LODViewingExtention && displayDetailLevel == lodControl.DistanceLevels.Length - 1)
                    distanceDetail.ViewingDistance = float.MaxValue;

                if (displayDetailLevel == lodControl.DistanceLevels.Length - 1)
                    distanceDetail.ViewingDistance = float.MaxValue;

                for (var i = 0; i < displayDetail.SubObjects.Length; i++)
                {
                    var subObject = displayDetail.SubObjects[i];

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

        internal Matrix GetMatrixProduct(int iNode)
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

        internal int GetParentMatrix(int iNode)
        {
            return LodControls[0].DistanceLevels[0].SubObjects[0].ShapePrimitives[0].Hierarchy[iNode];
        }

        #region "Elementos contenidos en SharedShape"
        internal class LodControl
        {
            internal DistanceLevel[] DistanceLevels;

            internal LodControl(lod_control MSTSlod_control, ShapeFile sFile, SharedShape sharedShape)
            {
                DistanceLevels = (from distance_level level in MSTSlod_control.distance_levels
                                  select new DistanceLevel(level, sFile, sharedShape)).ToArray();
                if (DistanceLevels.Length == 0)
                    throw new InvalidDataException("Shape file missing distance_level");
            }
        }
        internal class DistanceLevel
        {
            public float ViewingDistance;
            public float ViewSphereRadius;
            public SubObject[] SubObjects;

            public DistanceLevel(distance_level MSTSdistance_level, ShapeFile sFile, SharedShape sharedShape)
            {
                ViewingDistance = MSTSdistance_level.distance_level_header.dlevel_selection;
                // TODO, work out ViewShereRadius from all sub_object radius and centers.
                if (sFile.shape.volumes.Count > 0)
                    ViewSphereRadius = sFile.shape.volumes[0].Radius;
                else
                    ViewSphereRadius = 100;

                var index = 0;

                SubObjects = (from sub_object obj in MSTSdistance_level.sub_objects
                                  //select new SubObject(obj, ref index, MSTSdistance_level.distance_level_header.hierarchy, textureFlags, sFile, sharedShape)).ToArray();
                              select new SubObject(obj, ref index, MSTSdistance_level.distance_level_header.hierarchy, sFile, sharedShape)).ToArray();
                if (SubObjects.Length == 0)
                    throw new InvalidDataException("Shape file missing sub_object");
            }
        }
        internal class SubObject
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
            public SubObject(sub_object sub_object, ref int totalPrimitiveIndex, int[] hierarchy, ShapeFile sFile, SharedShape sharedShape)
            {
                var vertexBufferSet = new VertexBufferSet(sub_object, sFile, sharedShape.Viewer.GControl.GraphicsDevice);
                var primitiveIndex = 0;
                ShapePrimitives = new ShapePrimitive[sub_object.primitives.Count];
                foreach (primitive primitive in sub_object.primitives)
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

                    Materials.Material material;
                    if (primitiveState.tex_idxs.Length != 0)
                    {
                        var texture = sFile.shape.textures[primitiveState.tex_idxs[0]];
                        var imageName = sFile.shape.images[texture.iImage];
                        if (String.IsNullOrEmpty(sharedShape.ReferencePath))
                            material = sharedShape.Viewer.MaterialManager.Load("Scenery", SharedTextureManager.GetRouteTextureFile(0, imageName), (int)options, texture.MipMapLODBias);
                        else
                            material = sharedShape.Viewer.MaterialManager.Load("Scenery", SharedTextureManager.GetTextureFile(0, sharedShape.ReferencePath, imageName), (int)options, texture.MipMapLODBias);

                    }
                    else
                    {
                        material = sharedShape.Viewer.MaterialManager.Load("Scenery", null, (int)options);
                    }

                    if (primitive.indexed_trilist.vertex_idxs.Count == 0)
                    {
                        Trace.TraceWarning("Skipped primitive with 0 indices in {0}", sharedShape.FilePath);
                        continue;
                    }

                    var indexData = new List<ushort>(primitive.indexed_trilist.vertex_idxs.Count * 3);
                    foreach (vertex_idx vertex_idx in primitive.indexed_trilist.vertex_idxs)
                        foreach (var index in new[] { vertex_idx.a, vertex_idx.b, vertex_idx.c })
                            indexData.Add((ushort)index);

                    ShapePrimitives[primitiveIndex] = new ShapePrimitive(material, vertexBufferSet, indexData, sharedShape.Viewer.GControl.GraphicsDevice, hierarchy, vertexState.imatrix);
                    ShapePrimitives[primitiveIndex].SortIndex = ++totalPrimitiveIndex;
                    ++primitiveIndex;
                }

                if (primitiveIndex < ShapePrimitives.Length)
                    ShapePrimitives = ShapePrimitives.Take(primitiveIndex).ToArray();
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
        }
        #endregion
    }
}
