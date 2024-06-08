// #define DEBUG_RENDER_STATE
#define RENDER_BLEND_SORTING
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tourmaline.Common;
using Tourmaline.Viewer3D.Processes;
using TOURMALINE.Common;
using TOURMALINE.Common.Input;
using Game = Tourmaline.Viewer3D.Processes.Game;


namespace Tourmaline.Viewer3D
{
    public enum RenderPrimitiveSequence
    {
        CabOpaque,
        Sky,
        WorldOpaque,
        WorldBlended,
        Lights, // TODO: May not be needed once alpha sorting works.
        Precipitation, // TODO: May not be needed once alpha sorting works.
        Particles,
        InteriorOpaque,
        InteriorBlended,
        Labels,
        CabBlended,
        OverlayOpaque,
        OverlayBlended,
        // This value must be last.
        Sentinel
    }
    public enum RenderPrimitiveGroup
    {
        Cab,
        Sky,
        World,
        Lights, // TODO: May not be needed once alpha sorting works.
        Precipitation, // TODO: May not be needed once alpha sorting works.
        Particles,
        Interior,
        Labels,
        Overlay
    }

    public abstract class RenderPrimitive
    {
        /// <summary>
        /// Mapeado de <see cref="RenderPrimitiveGroup"/> hacia <see cref="RenderPrimitiveSequence"/> para materiales combinados.
        /// El número de elementos del array debe ser igual al número de valores en el <see cref="RenderPrimitiveGroup"/>.       
        /// </summary>
        public static readonly RenderPrimitiveSequence[] SequenceForBlended = new[] {
            RenderPrimitiveSequence.CabBlended,
            RenderPrimitiveSequence.Sky,
            RenderPrimitiveSequence.WorldBlended,
            RenderPrimitiveSequence.Lights,
            RenderPrimitiveSequence.Precipitation,
            RenderPrimitiveSequence.Particles,
            RenderPrimitiveSequence.InteriorBlended,
            RenderPrimitiveSequence.Labels,
            RenderPrimitiveSequence.OverlayBlended,
        };

        /// <summary> <see cref="RenderPrimitiveGroup"/> hacia <see cref="RenderPrimitiveSequence"/> para materiales opacos.
        /// El número de elementos del array debe ser igual al número de valores en el <see cref="RenderPrimitiveGroup"/>.
        /// </summary>
        public static readonly RenderPrimitiveSequence[] SequenceForOpaque = new[] {
            RenderPrimitiveSequence.CabOpaque,
            RenderPrimitiveSequence.Sky,
            RenderPrimitiveSequence.WorldOpaque,
            RenderPrimitiveSequence.Lights,
            RenderPrimitiveSequence.Precipitation,
            RenderPrimitiveSequence.Particles,
            RenderPrimitiveSequence.InteriorOpaque,
            RenderPrimitiveSequence.Labels,
            RenderPrimitiveSequence.OverlayOpaque,
        };

        /// <summary>
        /// Parámetro para el cálculo del buffer de profundidad que se puede usar para reducir las texturas a la misma profundidad para sobresalir una sobre otra.
        /// </summary>
        // TODO: ¿Sirve ahora mismo de algo?
        public float ZBias;

        /// <summary>
        /// Parámetro para ajustar el orden de aparición de las primitivas en la misma ubicación.
        /// Las primitivas con un mayor valor se procesan después de otras.
        /// Este parámetro no tiene efecto en las primitivas non-blended.
        /// </summary>
        public float SortIndex;

        /// <summary>
        /// Se llama cuando el objeto se renderiza a sí mismo en la pantalla.
        /// No se hace referencia a ningún dato volátil.
        /// Se ejecuta en el hilo "RenderProcess"
        /// </summary>
        /// <param name="graphicsDevice"></param>
        public abstract void Draw(GraphicsDevice graphicsDevice);

        // Necesitamos suministrar toda la información necesaria para el código del shader.
        // Para evitar dividir en versiones instanciadas y no instanciadas tenemos este buffer de vértices dummy en lugar del buffer de instancias donde lo necesitemos.
        static VertexBuffer DummyVertexBuffer;
        static internal VertexBuffer GetDummyVertexBuffer(GraphicsDevice graphicsDevice)
        {
            if (DummyVertexBuffer == null)
            {
                var vertexBuffer = new VertexBuffer(graphicsDevice, new VertexDeclaration(ShapeInstanceData.SizeInBytes, ShapeInstanceData.VertexElements), 1, BufferUsage.WriteOnly);
                vertexBuffer.SetData(new Matrix[] { Matrix.Identity });
                DummyVertexBuffer = vertexBuffer;
            }
            return DummyVertexBuffer;
        }
    }

    [DebuggerDisplay("{Material} {RenderPrimitive} {Flags}")]
    public struct RenderItem
    {
        public Material Material;
        public RenderPrimitive RenderPrimitive;
        public Matrix XNAMatrix;
        public ShapeFlags Flags;
        public object ItemData;

        public RenderItem(Material material, RenderPrimitive renderPrimitive, ref Matrix xnaMatrix, ShapeFlags flags, object itemData = null)
        {
            Material = material;
            RenderPrimitive = renderPrimitive;
            XNAMatrix = xnaMatrix;
            Flags = flags;
            ItemData = itemData;
        }

        public class Comparer : IComparer<RenderItem>
        {
            readonly Vector3 XNAViewerPos;

            public Comparer(Vector3 viewerPos)
            {
                XNAViewerPos = viewerPos;
                XNAViewerPos.Z *= -1;
            }

            #region IComparer<RenderItem> Members

            public int Compare(RenderItem x, RenderItem y)
            {
                // Por razones desconocidas esto peta con un ArgumentException (como si Compare(x,x) != 0)
                // El cálculo de igualación se hace restando los dos valores, pero a veces no da cero por temas de coma flotante.
                var xd = (x.XNAMatrix.Translation - XNAViewerPos).Length();
                var yd = (y.XNAMatrix.Translation - XNAViewerPos).Length();

                // Si la diferencia absoluta es >= 1mm la usaremos.
                // En otro caso diremos que es el mismo sitio.
                if (Math.Abs(yd - xd) >= 0.001)
                    return Math.Sign(yd - xd);
                return Math.Sign(x.RenderPrimitive.SortIndex - y.RenderPrimitive.SortIndex);
            }

            #endregion
        }
    }

    public class RenderItemCollection : IList<RenderItem>, IEnumerator<RenderItem>
    {
        RenderItem[] Items = new RenderItem[4];
        int ItemCount;
        int EnumeratorIndex;

        public RenderItemCollection()
        {
        }

        public int Capacity { get => Items.Length; }
        public int Count { get => ItemCount; }

        public void Sort(IComparer<RenderItem> comparer)
        {
            Array.Sort(Items, 0, ItemCount, comparer);
        }

        #region IList<RenderItem> Members

        public int IndexOf(RenderItem item)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, RenderItem item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public RenderItem this[int index]
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        #endregion

        #region ICollection<RenderItem> Members

        public void Add(RenderItem item)
        {
            if (ItemCount == Items.Length)
            {
                var items = new RenderItem[Items.Length * 2];
                Array.Copy(Items, 0, items, 0, Items.Length);
                Items = items;
            }
            Items[ItemCount] = item;
            ItemCount++;
        }

        public void Clear()
        {
            Array.Clear(Items, 0, ItemCount);
            ItemCount = 0;
        }

        public bool Contains(RenderItem item)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(RenderItem[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        int ICollection<RenderItem>.Count
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public bool IsReadOnly
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public bool Remove(RenderItem item)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region IEnumerable<RenderItem> Members

        public IEnumerator<RenderItem> GetEnumerator()
        {
            Reset();
            return this;
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IEnumerator<RenderItem> Members

        public RenderItem Current
        {
            get
            {
                return Items[EnumeratorIndex];
            }
        }

        #endregion

        #region IEnumerator Members

        object System.Collections.IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public bool MoveNext()
        {
            EnumeratorIndex++;
            return EnumeratorIndex < ItemCount;
        }

        public void Reset()
        {
            EnumeratorIndex = -1;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            // No op.
        }

        #endregion
    }

    public class RenderFrame
    {
        readonly Game Game;

        // Mapa de datos de sombras compartidas.
        static RenderTarget2D[] ShadowMap;
        static RenderTarget2D[] ShadowMapRenderTarget;
        static Vector3 SteppedSolarDirection = Vector3.UnitX;

        // Mapa de datos de sombras locales.
        Matrix[] ShadowMapLightView;
        Matrix[] ShadowMapLightProj;
        Matrix[] ShadowMapLightViewProjShadowProj;
        Vector3 ShadowMapX;
        Vector3 ShadowMapY;
        Vector3[] ShadowMapCenter;

        internal RenderTarget2D RenderSurface;
        SpriteBatchMaterial RenderSurfaceMaterial;

        readonly Material DummyBlendedMaterial;
        readonly Dictionary<Material, RenderItemCollection>[] RenderItems = new Dictionary<Material, RenderItemCollection>[(int)RenderPrimitiveSequence.Sentinel];
        readonly RenderItemCollection[] RenderShadowSceneryItems;
        readonly RenderItemCollection[] RenderShadowForestItems;
        readonly RenderItemCollection[] RenderShadowTerrainItems;
        readonly RenderItemCollection RenderItemsSequence = new RenderItemCollection();

        public bool IsScreenChanged { get; internal set; }
        ShadowMapMaterial ShadowMapMaterial;
        SceneryShader SceneryShader;
        Vector3 SolarDirection;
        Camera Camera;
        Vector3 CameraLocation;
        Vector3 XNACameraLocation;
        Matrix XNACameraView;
        Matrix XNACameraProjection;

        public RenderFrame(Game game)
        {
            Game = game;
            DummyBlendedMaterial = new EmptyMaterial(null);

            for (int i = 0; i < RenderItems.Length; i++)
                RenderItems[i] = new Dictionary<Material, RenderItemCollection>();

            //if (Game.Settings.DynamicShadows)
            //{
            //    if (ShadowMap == null)
            //    {
            //        var shadowMapSize = Game.Settings.ShadowMapResolution;
            //        ShadowMap = new RenderTarget2D[RenderProcess.ShadowMapCount];
            //        ShadowMapRenderTarget = new RenderTarget2D[RenderProcess.ShadowMapCount];
            //        for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
            //        {
            //            ShadowMapRenderTarget[shadowMapIndex] = new RenderTarget2D(Game.RenderProcess.GraphicsDevice, shadowMapSize, shadowMapSize, false, SurfaceFormat.Rg32, DepthFormat.Depth16, 0, RenderTargetUsage.PreserveContents);
            //            ShadowMap[shadowMapIndex] = new RenderTarget2D(Game.RenderProcess.GraphicsDevice, shadowMapSize, shadowMapSize, false, SurfaceFormat.Rg32, DepthFormat.Depth16, 0, RenderTargetUsage.PreserveContents);
            //        }
            //    }

            //    ShadowMapLightView = new Matrix[RenderProcess.ShadowMapCount];
            //    ShadowMapLightProj = new Matrix[RenderProcess.ShadowMapCount];
            //    ShadowMapLightViewProjShadowProj = new Matrix[RenderProcess.ShadowMapCount];
            //    ShadowMapCenter = new Vector3[RenderProcess.ShadowMapCount];

            //    RenderShadowSceneryItems = new RenderItemCollection[RenderProcess.ShadowMapCount];
            //    RenderShadowForestItems = new RenderItemCollection[RenderProcess.ShadowMapCount];
            //    RenderShadowTerrainItems = new RenderItemCollection[RenderProcess.ShadowMapCount];
            //    for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
            //    {
            //        RenderShadowSceneryItems[shadowMapIndex] = new RenderItemCollection();
            //        RenderShadowForestItems[shadowMapIndex] = new RenderItemCollection();
            //        RenderShadowTerrainItems[shadowMapIndex] = new RenderItemCollection();
            //    }
            //}
            //De momento desactivo las sombras

            XNACameraView = Matrix.Identity;
            XNACameraProjection = Matrix.CreateOrthographic(game.RenderProcess.DisplaySize.X, game.RenderProcess.DisplaySize.Y, 1, 100);

            ScreenChanged();
        }

        void ScreenChanged()
        {
            RenderSurface = new RenderTarget2D(
                Game.RenderProcess.GraphicsDevice,
                Game.RenderProcess.GraphicsDevice.PresentationParameters.BackBufferWidth,
                Game.RenderProcess.GraphicsDevice.PresentationParameters.BackBufferHeight,
                false,
                Game.RenderProcess.GraphicsDevice.PresentationParameters.BackBufferFormat,
                Game.RenderProcess.GraphicsDevice.PresentationParameters.DepthStencilFormat,
                Game.RenderProcess.GraphicsDevice.PresentationParameters.MultiSampleCount,
                RenderTargetUsage.PreserveContents
            );
        }

        public void Clear()
        {
            // Intento de eliminar materiales sin usar después de un tiempo (máximo 1 por RenderPrimitiveSequence).
            for (var i = 0; i < RenderItems.Length; i++)
            {
                foreach (var mat in RenderItems[i].Keys)
                {
                    if (RenderItems[i][mat].Count == 0)
                    {
                        RenderItems[i].Remove(mat);
                        break;
                    }
                }
            }

            // Reinicio de todas las listas de RenderItem
            for (var i = 0; i < RenderItems.Length; i++)
                foreach (var mat in RenderItems[i].Keys)
                    RenderItems[i][mat].Clear();

            // Reinicio de todas las listas de RenderItem mapeadas.
            //if (Game.Settings.DynamicShadows)
            //{
            //    for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
            //    {
            //        RenderShadowSceneryItems[shadowMapIndex].Clear();
            //        RenderShadowForestItems[shadowMapIndex].Clear();
            //        RenderShadowTerrainItems[shadowMapIndex].Clear();
            //    }
            //}
        }

        public void PrepareFrame(Viewer viewer)
        {
            if (RenderSurfaceMaterial == null)
                RenderSurfaceMaterial = new SpriteBatchMaterial(viewer, BlendState.Opaque);

            if (ShadowMapMaterial == null)
                ShadowMapMaterial = (ShadowMapMaterial)viewer.MaterialManager.Load("ShadowMap");
            if (SceneryShader == null)
                SceneryShader = viewer.MaterialManager.SceneryShader;
        }

        public void SetCamera(Camera camera)
        {
            Camera = camera;
            XNACameraLocation = CameraLocation = Camera.Location;
            XNACameraLocation.Z *= -1;
            XNACameraView = Camera.XnaView;
            XNACameraProjection = Camera.XnaProjection;
        }

        static bool LockShadows;
        [CallOnThread("Updater")]
        public void PrepareFrame(long elapsedTime)
        {
            //if (Game.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0) && !LockShadows)
            //{
            //    var solarDirection = SolarDirection;
            //    solarDirection.Normalize();
            //    if (Vector3.Dot(SteppedSolarDirection, solarDirection) < 0.99999)
            //        SteppedSolarDirection = solarDirection;

            //    var cameraDirection = new Vector3(-XNACameraView.M13, -XNACameraView.M23, -XNACameraView.M33);
            //    cameraDirection.Normalize();

            //    var shadowMapAlignAxisX = Vector3.Cross(SteppedSolarDirection, Vector3.UnitY);
            //    var shadowMapAlignAxisY = Vector3.Cross(shadowMapAlignAxisX, SteppedSolarDirection);
            //    shadowMapAlignAxisX.Normalize();
            //    shadowMapAlignAxisY.Normalize();
            //    ShadowMapX = shadowMapAlignAxisX;
            //    ShadowMapY = shadowMapAlignAxisY;

            //    for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
            //    {
            //        var viewingDistance = Game.Settings.ViewingDistance;
            //        var shadowMapDiameter = RenderProcess.ShadowMapDiameter[shadowMapIndex];
            //        var shadowMapLocation = XNACameraLocation + RenderProcess.ShadowMapDistance[shadowMapIndex] * cameraDirection;

            //        // Alinea la posición del mapa de sombras al grid para que no se menee mucho.
            //        // básicamente significa que alineará las sombras a lo largo de un grid basado en el texel (ShadowMapSize /ShadowMapSize) de
            //        // las sombras a lo largo del eje de la dirección del sol en sentido ascendente y hacia la izquierda.
            //        var shadowMapAlignmentGrid = (float)shadowMapDiameter / Game.Settings.ShadowMapResolution;
            //        var shadowMapSize = Game.Settings.ShadowMapResolution;
            //        var adjustX = (float)Math.IEEERemainder(Vector3.Dot(shadowMapAlignAxisX, shadowMapLocation), shadowMapAlignmentGrid);
            //        var adjustY = (float)Math.IEEERemainder(Vector3.Dot(shadowMapAlignAxisY, shadowMapLocation), shadowMapAlignmentGrid);
            //        shadowMapLocation.X -= shadowMapAlignAxisX.X * adjustX;
            //        shadowMapLocation.Y -= shadowMapAlignAxisX.Y * adjustX;
            //        shadowMapLocation.Z -= shadowMapAlignAxisX.Z * adjustX;
            //        shadowMapLocation.X -= shadowMapAlignAxisY.X * adjustY;
            //        shadowMapLocation.Y -= shadowMapAlignAxisY.Y * adjustY;
            //        shadowMapLocation.Z -= shadowMapAlignAxisY.Z * adjustY;

            //        ShadowMapLightView[shadowMapIndex] = Matrix.CreateLookAt(shadowMapLocation + viewingDistance * SteppedSolarDirection, shadowMapLocation, Vector3.Up);
            //        ShadowMapLightProj[shadowMapIndex] = Matrix.CreateOrthographic(shadowMapDiameter, shadowMapDiameter, 0, viewingDistance + shadowMapDiameter / 2);
            //        ShadowMapLightViewProjShadowProj[shadowMapIndex] = ShadowMapLightView[shadowMapIndex] * ShadowMapLightProj[shadowMapIndex] * new Matrix(0.5f, 0, 0, 0, 0, -0.5f, 0, 0, 0, 0, 1, 0, 0.5f + 0.5f / shadowMapSize, 0.5f + 0.5f / shadowMapSize, 0, 1);
            //        ShadowMapCenter[shadowMapIndex] = shadowMapLocation;
            //    }
            //}
        }

        /// <summary>
        /// Añade o desecha automáticamente una <see cref="RenderPrimitive"/> basándose en la localización, radio y máxima distancia de visionado.
        /// </summary>
        /// <param name="mstsLocation">Ubicación del centro de la <see cref="RenderPrimitive"/> en coordenadas MSTS.</param>
        /// <param name="objectRadius">Radio de la esfera que contiene la <see cref="RenderPrimitive"/> completa, centrada en la ubicación <paramref name="mstsLocation"/>.</param>
        /// <param name="objectViewingDistance">Distancia máxima desde donde la <see cref="RenderPrimitive"/> debería ser visible.</param>
        /// <param name="material"></param>
        /// <param name="primitive"></param>
        /// <param name="group"></param>
        /// <param name="xnaMatrix"></param>
        /// <param name="flags"></param>
        [CallOnThread("Updater")]
        public void AddAutoPrimitive(Vector3 mstsLocation, float objectRadius, float objectViewingDistance, Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix, ShapeFlags flags)
        {
            if (float.IsPositiveInfinity(objectViewingDistance) || (Camera != null && Camera.InRange(mstsLocation, objectRadius, objectViewingDistance)))
            {
                if (Camera != null && Camera.InFov(mstsLocation, objectRadius))
                    AddPrimitive(material, primitive, group, ref xnaMatrix, flags);
            }

            //if (Game.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0) && ((flags & ShapeFlags.ShadowCaster) != 0))
            //    for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
            //        if (IsInShadowMap(shadowMapIndex, mstsLocation, objectRadius, objectViewingDistance))
            //            AddShadowPrimitive(shadowMapIndex, material, primitive, ref xnaMatrix, flags);
        }

        [CallOnThread("Updater")]
        public void AddPrimitive(Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix)
        {
            AddPrimitive(material, primitive, group, ref xnaMatrix, ShapeFlags.None, null);
        }

        static readonly bool[] PrimitiveBlendedScenery = new bool[] { true, false }; // Busca píxeles opacos en primitivas mezcladas con canal alpha mientras mantiene el DepthBuffer en orden correcto
        static readonly bool[] PrimitiveBlended = new bool[] { true };
        static readonly bool[] PrimitiveNotBlended = new bool[] { false };

        [CallOnThread("Updater")]
        public void AddPrimitive(Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix, ShapeFlags flags)
        {
            AddPrimitive(material, primitive, group, ref xnaMatrix, flags, null);
        }

        [CallOnThread("Updater")]
        public void AddPrimitive(Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix, ShapeFlags flags, object itemData)
        {
            var getBlending = material.GetBlending();
            var blending = getBlending && material is SceneryMaterial ? PrimitiveBlendedScenery : getBlending ? PrimitiveBlended : PrimitiveNotBlended;

            RenderItemCollection items;
            foreach (var blended in blending)
            {
                var sortingMaterial = blended ? DummyBlendedMaterial : material;
                var sequence = RenderItems[(int)GetRenderSequence(group, blended)];

                if (!sequence.TryGetValue(sortingMaterial, out items))
                {
                    items = new RenderItemCollection();
                    sequence.Add(sortingMaterial, items);
                }
                items.Add(new RenderItem(material, primitive, ref xnaMatrix, flags, itemData));
            }
            if (((flags & ShapeFlags.AutoZBias) != 0) && (primitive.ZBias == 0))
                primitive.ZBias = 1;
        }

        [CallOnThread("Updater")]
        void AddShadowPrimitive(int shadowMapIndex, Material material, RenderPrimitive primitive, ref Matrix xnaMatrix, ShapeFlags flags)
        {
            if (material is SceneryMaterial)
                RenderShadowSceneryItems[shadowMapIndex].Add(new RenderItem(material, primitive, ref xnaMatrix, flags));
            //else if (material is ForestMaterial)
            //    RenderShadowForestItems[shadowMapIndex].Add(new RenderItem(material, primitive, ref xnaMatrix, flags));
            //else if (material is TerrainMaterial)
            //    RenderShadowTerrainItems[shadowMapIndex].Add(new RenderItem(material, primitive, ref xnaMatrix, flags));
            else
                Debug.Fail("Only scenery, forest and terrain materials allowed in shadow map.");
        }

        [CallOnThread("Updater")]
        public void Sort()
        {
            var renderItemComparer = new RenderItem.Comparer(CameraLocation);
            foreach (var sequence in RenderItems)
            {
                foreach (var sequenceMaterial in sequence.Where(kvp => kvp.Value.Count > 0))
                {
                    if (sequenceMaterial.Key != DummyBlendedMaterial)
                        continue;
                    sequenceMaterial.Value.Sort(renderItemComparer);
                }
            }
        }

        bool IsInShadowMap(int shadowMapIndex, Vector3 mstsLocation, float objectRadius, float objectViewingDistance)
        {
            if (ShadowMapRenderTarget == null)
                return false;

            mstsLocation.Z *= -1;
            mstsLocation.X -= ShadowMapCenter[shadowMapIndex].X;
            mstsLocation.Y -= ShadowMapCenter[shadowMapIndex].Y;
            mstsLocation.Z -= ShadowMapCenter[shadowMapIndex].Z;
            objectRadius += RenderProcess.ShadowMapDiameter[shadowMapIndex] / 2;

            // Comprueba si el objeto está dentro de la esfera.
            var length = mstsLocation.LengthSquared();
            if (length <= objectRadius * objectRadius)
                return true;

            // Comprueba si el objeto está dentro del cilindro.
            var dotX = Math.Abs(Vector3.Dot(mstsLocation, ShadowMapX));
            if (dotX > objectRadius)
                return false;

            var dotY = Math.Abs(Vector3.Dot(mstsLocation, ShadowMapY));
            if (dotY > objectRadius)
                return false;

            // Comprueba si el objeto está en el lado correcto del centro.
            var dotZ = Vector3.Dot(mstsLocation, SteppedSolarDirection);
            if (dotZ < 0)
                return false;

            return true;
        }

        static RenderPrimitiveSequence GetRenderSequence(RenderPrimitiveGroup group, bool blended)
        {
            if (blended)
                return RenderPrimitive.SequenceForBlended[(int)group];
            return RenderPrimitive.SequenceForOpaque[(int)group];
        }

        [CallOnThread("Render")]
        public void Draw(GraphicsDevice graphicsDevice)
        {
            if (RenderSurface.Width != graphicsDevice.PresentationParameters.BackBufferWidth || RenderSurface.Height != graphicsDevice.PresentationParameters.BackBufferHeight)
                ScreenChanged();

#if DEBUG_RENDER_STATE
            DebugRenderState(graphicsDevice, "RenderFrame.Draw");
#endif
            //var logging = UserInput.IsPressed(UserCommand.DebugLogRenderFrame);
            var logging = false;
            if (logging)
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Draw {");
            }

            //if (Game.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0) && ShadowMapMaterial != null)
            //    DrawShadows(graphicsDevice, logging);

            DrawSimple(graphicsDevice, logging);

            for (var i = 0; i < (int)RenderPrimitiveSequence.Sentinel; i++)
                Game.RenderProcess.PrimitiveCount[i] = RenderItems[i].Values.Sum(l => l.Count);

            if (logging)
            {
                Console.WriteLine("}");
                Console.WriteLine();
            }
        }

        void DrawShadows(GraphicsDevice graphicsDevice, bool logging)
        {
            if (logging) Console.WriteLine("  DrawShadows {");
            for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                DrawShadows(graphicsDevice, logging, shadowMapIndex);
            for (var shadowMapIndex = 0; shadowMapIndex < RenderProcess.ShadowMapCount; shadowMapIndex++)
                Game.RenderProcess.ShadowPrimitiveCount[shadowMapIndex] = RenderShadowSceneryItems[shadowMapIndex].Count + RenderShadowForestItems[shadowMapIndex].Count + RenderShadowTerrainItems[shadowMapIndex].Count;
            if (logging) Console.WriteLine("  }");
        }

        void DrawShadows(GraphicsDevice graphicsDevice, bool logging, int shadowMapIndex)
        {
            if (logging) Console.WriteLine("    {0} {{", shadowMapIndex);

            // Prepara el renderizador para dibujar el mapa de sombras.
            graphicsDevice.SetRenderTarget(ShadowMapRenderTarget[shadowMapIndex]);
            graphicsDevice.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, Color.White, 1, 0);

            // Prepara el renderizado normal de la escena (el no bloqueante).
            ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.Normal);

            // Primeo renderiza los elementos de sombra que no sean del terreno ni de los bosques.
            if (logging) Console.WriteLine("      {0,-5} * SceneryMaterial (normal)", RenderShadowSceneryItems[shadowMapIndex].Count);
            ShadowMapMaterial.Render(graphicsDevice, RenderShadowSceneryItems[shadowMapIndex], ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            // Prepara el renderizado normal (no bloqueante) de los bosques.
            ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.Forest);

            // Renderiza los elementos de sombra del bosque a continuación.
            if (logging) Console.WriteLine("      {0,-5} * ForestMaterial (forest)", RenderShadowForestItems[shadowMapIndex].Count);
            ShadowMapMaterial.Render(graphicsDevice, RenderShadowForestItems[shadowMapIndex], ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            // Prepara el renderizado normal (no bloqueante) del terreno.
            ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.Normal);

            // Representa las sombras de los elementos del terreno ahora, con su magia.
            if (logging) Console.WriteLine("      {0,-5} * TerrainMaterial (normal)", RenderShadowTerrainItems[shadowMapIndex].Count);
            //graphicsDevice.Indices = TerrainPrimitive.SharedPatchIndexBuffer;
            ShadowMapMaterial.Render(graphicsDevice, RenderShadowTerrainItems[shadowMapIndex], ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            // Prepara para la representación bloqueante del terreno.
            ShadowMapMaterial.SetState(graphicsDevice, ShadowMapMaterial.Mode.Blocker);

            // Representa los elementos de sombra del terreno en modo bloqueante.
            if (logging) Console.WriteLine("      {0,-5} * TerrainMaterial (blocker)", RenderShadowTerrainItems[shadowMapIndex].Count);
            ShadowMapMaterial.Render(graphicsDevice, RenderShadowTerrainItems[shadowMapIndex], ref ShadowMapLightView[shadowMapIndex], ref ShadowMapLightProj[shadowMapIndex]);

            // Todo está hecho.
            ShadowMapMaterial.ResetState(graphicsDevice);
#if DEBUG_RENDER_STATE
            DebugRenderState(graphicsDevice, ShadowMapMaterial.ToString());
#endif
            graphicsDevice.SetRenderTarget(null);

            // Difumina el mapa de sombras.
//            if (Game.Settings.ShadowMapBlur)
//            {
//                ShadowMap[shadowMapIndex] = ShadowMapMaterial.ApplyBlur(graphicsDevice, ShadowMap[shadowMapIndex], ShadowMapRenderTarget[shadowMapIndex]);
//#if DEBUG_RENDER_STATE
//                DebugRenderState(graphicsDevice, ShadowMapMaterial.ToString() + " ApplyBlur()");
//#endif
//            }
//            else
//                ShadowMap[shadowMapIndex] = ShadowMapRenderTarget[shadowMapIndex];

            if (logging) Console.WriteLine("    }");
        }

        /// <summary>
        /// Ejecución en el hilo RenderProcess. Dibujo simple.
        /// </summary>
        /// <param name="graphicsDevice"></param>
        /// <param name="logging"></param>
        void DrawSimple(GraphicsDevice graphicsDevice, bool logging)
        {
            if (RenderSurfaceMaterial != null)
            {
                graphicsDevice.SetRenderTarget(RenderSurface);
            }

            if (logging) Console.WriteLine("  DrawSimple {");
            graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 1, 0);
            graphicsDevice.Clear(Color.CornflowerBlue);
            
            DrawSequences(graphicsDevice, logging);
            if (logging) Console.WriteLine("  }");

            if (RenderSurfaceMaterial != null)
            {
                graphicsDevice.SetRenderTarget(null);
                graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 1, 0);
                graphicsDevice.Clear(Color.Green);
                RenderSurfaceMaterial.SetState(graphicsDevice, null);
                RenderSurfaceMaterial.SpriteBatch.Draw(RenderSurface, Vector2.Zero, Color.White);
                RenderSurfaceMaterial.ResetState(graphicsDevice);
            }
        }

        void DrawSequences(GraphicsDevice graphicsDevice, bool logging)
        {
            //if (Game.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0) && SceneryShader != null)
            //    SceneryShader.SetShadowMap(ShadowMapLightViewProjShadowProj, ShadowMap, RenderProcess.ShadowMapLimit);

            var renderItems = RenderItemsSequence;
            renderItems.Clear();
            for (var i = 0; i < (int)RenderPrimitiveSequence.Sentinel; i++)
            {
                if (logging) Console.WriteLine("    {0} {{", (RenderPrimitiveSequence)i);
                var sequence = RenderItems[i];
                foreach (var sequenceMaterial in sequence)
                {
                    if (sequenceMaterial.Value.Count == 0)
                        continue;
                    if (sequenceMaterial.Key == DummyBlendedMaterial)
                    {
                        // Blended: Materiales múltiples agrupados por material todo lo posible sin destruir el orden.
                        Material lastMaterial = null;
                        foreach (var renderItem in sequenceMaterial.Value)
                        {
                            if (lastMaterial != renderItem.Material)
                            {
                                if (renderItems.Count > 0)
                                {
                                    if (logging) Console.WriteLine("      {0,-5} * {1}", renderItems.Count, lastMaterial);
                                    lastMaterial.Render(graphicsDevice, renderItems, ref XNACameraView, ref XNACameraProjection);
                                    renderItems.Clear();
                                }
                                if (lastMaterial != null)
                                    lastMaterial.ResetState(graphicsDevice);
#if DEBUG_RENDER_STATE
                                if (lastMaterial != null)
                                    DebugRenderState(graphicsDevice, lastMaterial.ToString());
#endif
                                renderItem.Material.SetState(graphicsDevice, lastMaterial);
                                lastMaterial = renderItem.Material;
                            }
                            renderItems.Add(renderItem);
                        }
                        if (renderItems.Count > 0)
                        {
                            if (logging) Console.WriteLine("      {0,-5} * {1}", renderItems.Count, lastMaterial);
                            lastMaterial.Render(graphicsDevice, renderItems, ref XNACameraView, ref XNACameraProjection);
                            renderItems.Clear();
                        }
                        if (lastMaterial != null)
                            lastMaterial.ResetState(graphicsDevice);
#if DEBUG_RENDER_STATE
                        if (lastMaterial != null)
                            DebugRenderState(graphicsDevice, lastMaterial.ToString());
#endif
                    }
                    else
                    {
                        //if (Game.Settings.DistantMountains && (sequenceMaterial.Key is TerrainSharedDistantMountain || sequenceMaterial.Key is SkyMaterial
                        //    || sequenceMaterial.Key is MSTSSkyMaterial))
                        //    continue;
                        // Opacidad: Material simple... representar en un solo paso.
                        sequenceMaterial.Key.SetState(graphicsDevice, null);
                        if (logging) Console.WriteLine("      {0,-5} * {1}", sequenceMaterial.Value.Count, sequenceMaterial.Key);
                        sequenceMaterial.Key.Render(graphicsDevice, sequenceMaterial.Value, ref XNACameraView, ref XNACameraProjection);
                        sequenceMaterial.Key.ResetState(graphicsDevice);
#if DEBUG_RENDER_STATE
                        DebugRenderState(graphicsDevice, sequenceMaterial.Key.ToString());
#endif
                    }
                }
                if (logging) Console.WriteLine("    }");
            }

            //if (Game.Settings.DynamicShadows && (RenderProcess.ShadowMapCount > 0) && SceneryShader != null)
            //    SceneryShader.ClearShadowMap();
        }

        void DrawSequencesDistantMountains(GraphicsDevice graphicsDevice, bool logging)
        {
            for (var i = 0; i < (int)RenderPrimitiveSequence.Sentinel; i++)
            {
                if (logging) Console.WriteLine("    {0} {{", (RenderPrimitiveSequence)i);
                var sequence = RenderItems[i];
                foreach (var sequenceMaterial in sequence)
                {
                    if (sequenceMaterial.Value.Count == 0)
                        continue;
                    //if (sequenceMaterial.Key is TerrainSharedDistantMountain || sequenceMaterial.Key is SkyMaterial || sequenceMaterial.Key is MSTSSkyMaterial)
                    if (sequenceMaterial.Key is SkyMaterial)
                    {
                        // Opacidad: Material simple... representar en un solo paso.
                        sequenceMaterial.Key.SetState(graphicsDevice, null);
                        if (logging) Console.WriteLine("      {0,-5} * {1}", sequenceMaterial.Value.Count, sequenceMaterial.Key);
                        sequenceMaterial.Key.Render(graphicsDevice, sequenceMaterial.Value, ref XNACameraView, ref Camera.XnaDistantMountainProjection);
                        sequenceMaterial.Key.ResetState(graphicsDevice);
#if DEBUG_RENDER_STATE
                        DebugRenderState(graphicsDevice, sequenceMaterial.Key.ToString());
#endif
                    }
                }
                if (logging) Console.WriteLine("    }");
            }
        }

#if DEBUG_RENDER_STATE
        static void DebugRenderState(GraphicsDevice graphicsDevice, string location)
        {
            if (graphicsDevice.BlendState != BlendState.Opaque) throw new InvalidOperationException($"BlendState is {graphicsDevice.BlendState}; expected {BlendState.Opaque} at {location}.");
            if (graphicsDevice.DepthStencilState != DepthStencilState.Default) throw new InvalidOperationException($"DepthStencilState is {graphicsDevice.DepthStencilState}; expected {DepthStencilState.Default} at {location}.");
            if (graphicsDevice.RasterizerState != RasterizerState.CullCounterClockwise) throw new InvalidOperationException($"RasterizerState is {graphicsDevice.RasterizerState}; expected {RasterizerState.CullCounterClockwise} at {location}.");
            // TODO: Check graphicsDevice.ScissorRectangle? Tricky because we struggle to know what the default Width/Height should be (different for shadows vs normal)
        }
#endif
    }
}
