using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tourmaline.Viewer3D.Processes;
using Tourmaline.Viewer3D.TvForms;

namespace Tourmaline.Viewer3D.Materials
{
    internal enum RenderPrimitiveSequence
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
    internal enum RenderPrimitiveGroup
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

    internal abstract class RenderPrimitive
    {
        /// <summary>
        /// Mapeado de <see cref="RenderPrimitiveGroup"/> hacia <see cref="RenderPrimitiveSequence"/> para materiales combinados.
        /// El número de elementos del array debe ser igual al número de valores en el <see cref="RenderPrimitiveGroup"/>.       
        /// </summary>
        internal static readonly RenderPrimitiveSequence[] SequenceForBlended = new[] {
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
        internal static readonly RenderPrimitiveSequence[] SequenceForOpaque = new[] {
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
        internal float ZBias;

        /// <summary>
        /// Parámetro para ajustar el orden de aparición de las primitivas en la misma ubicación.
        /// Las primitivas con un mayor valor se procesan después de otras.
        /// Este parámetro no tiene efecto en las primitivas non-blended.
        /// </summary>
        internal float SortIndex;

        /// <summary>
        /// Se llama cuando el objeto se renderiza a sí mismo en la pantalla.
        /// No se hace referencia a ningún dato volátil.
        /// Se ejecuta en el hilo "RenderProcess"
        /// </summary>
        /// <param name="graphicsDevice"></param>
        internal abstract void Draw(GraphicsDevice graphicsDevice);

        // Necesitamos suministrar toda la información necesaria para el código del shader.
        // Para evitar dividir en versiones instanciadas y no instanciadas tenemos este buffer de vértices dummy en lugar del buffer de instancias donde lo necesitemos.
        static VertexBuffer DummyVertexBuffer;
        static internal VertexBuffer GetDummyVertexBuffer(GraphicsDevice graphicsDevice)
        {
            if (DummyVertexBuffer == null)
            {
                var vertexBuffer = new VertexBuffer(graphicsDevice, new VertexDeclaration(ShapeInstanceData.SizeInBytes, ShapeInstanceData.VertexElements), 1, BufferUsage.WriteOnly);
                vertexBuffer.SetData(new Microsoft.Xna.Framework.Matrix[] { Microsoft.Xna.Framework.Matrix.Identity });
                DummyVertexBuffer = vertexBuffer;
            }
            return DummyVertexBuffer;
        }


    }

    [DebuggerDisplay("{Material} {RenderPrimitive} {Flags}")]
    internal struct RenderItem
    {
        internal Material Material;
        internal RenderPrimitive RenderPrimitive;
        internal Microsoft.Xna.Framework.Matrix XNAMatrix;
        internal ShapeFlags Flags;
        internal object ItemData;
        internal RenderItem(Material material, RenderPrimitive renderPrimitive, ref Microsoft.Xna.Framework.Matrix xnaMatrix, ShapeFlags flags, object itemData = null)
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

    internal class RenderItemCollection : IList<RenderItem>, IEnumerator<RenderItem>
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

    internal class RenderFrame
    {
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
        _3dBaseCamera Camera;
        Vector3 CameraLocation;
        Vector3 XNACameraLocation;
        Matrix XNACameraView;
        Matrix XNACameraProjection;

        readonly GraphicsDeviceControl mvarControl;

        internal RenderFrame(GraphicsDeviceControl myControl)
        {
            mvarControl = myControl;
            DummyBlendedMaterial = new EmptyMaterial(null);
            for (int i = 0; i < RenderItems.Length; i++)
                RenderItems[i] = new Dictionary<Material, RenderItemCollection>();
            XNACameraView = Matrix.Identity;
            XNACameraProjection = Matrix.CreateOrthographic(mvarControl.DisplayRectangle.Width, mvarControl.DisplayRectangle.Height, 1, 100);
            ScreenChanged();
        }

        void ScreenChanged()
        {
            RenderSurface = new RenderTarget2D(
                mvarControl.GraphicsDevice,
                mvarControl.GraphicsDevice.PresentationParameters.BackBufferWidth,                
                mvarControl.GraphicsDevice.PresentationParameters.BackBufferHeight,
                false,
                mvarControl.GraphicsDevice.PresentationParameters.BackBufferFormat,
                mvarControl.GraphicsDevice.PresentationParameters.DepthStencilFormat,
                mvarControl.GraphicsDevice.PresentationParameters.MultiSampleCount,
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
        }

        internal void PrepareFrame(_3dTrainViewer viewer)
        {
            if (RenderSurfaceMaterial == null)
                RenderSurfaceMaterial = new SpriteBatchMaterial(viewer, BlendState.Opaque);

            //if (ShadowMapMaterial == null)
            //    ShadowMapMaterial = (ShadowMapMaterial)viewer.MaterialManager.Load("ShadowMap");
            if (SceneryShader == null)
                SceneryShader = viewer.MaterialManager.SceneryShader;
        }

        internal void SetCamera(_3dBaseCamera camera)
        {
            Camera = camera;
            XNACameraLocation = CameraLocation = Camera.Location;
            XNACameraLocation.Z *= -1;
            XNACameraView = Camera.XnaView;
            XNACameraProjection = Camera.XnaProjection;
        }

        static bool LockShadows;

        internal void PrepareFrame(long elapsedTime){}

        internal void AddAutoPrimitive(Vector3 mstsLocation, float objectRadius, float objectViewingDistance, Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix, ShapeFlags flags)
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

        internal void AddPrimitive(Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix)
        {
            AddPrimitive(material, primitive, group, ref xnaMatrix, ShapeFlags.None, null);
        }

        static readonly bool[] PrimitiveBlendedScenery = new bool[] { true, false }; // Busca píxeles opacos en primitivas mezcladas con canal alpha mientras mantiene el DepthBuffer en orden correcto
        static readonly bool[] PrimitiveBlended = new bool[] { true };
        static readonly bool[] PrimitiveNotBlended = new bool[] { false };

        public void AddPrimitive(Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix, ShapeFlags flags)
        {
            AddPrimitive(material, primitive, group, ref xnaMatrix, flags, null);
        }

        internal void AddPrimitive(Material material, RenderPrimitive primitive, RenderPrimitiveGroup group, ref Matrix xnaMatrix, ShapeFlags flags, object itemData)
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

        internal void Sort()
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

        static RenderPrimitiveSequence GetRenderSequence(RenderPrimitiveGroup group, bool blended)
        {
            if (blended)
                return RenderPrimitive.SequenceForBlended[(int)group];
            return RenderPrimitive.SequenceForOpaque[(int)group];
        }

        public void Draw(GraphicsDeviceControl destination)
        {
            if (RenderSurface.Width != destination.GraphicsDevice.PresentationParameters.BackBufferWidth || RenderSurface.Height != destination.GraphicsDevice.PresentationParameters.BackBufferHeight)
                ScreenChanged();

            var logging = false;
            if (logging)
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Draw {");
            }

            DrawSimple(destination, logging);


            //for (var i = 0; i < (int)RenderPrimitiveSequence.Sentinel; i++)
            //    Game.RenderProcess.PrimitiveCount[i] = RenderItems[i].Values.Sum(l => l.Count);

            if (logging)
            {
                Console.WriteLine("}");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Ejecución en el hilo RenderProcess. Dibujo simple.
        /// </summary>
        /// <param name="graphicsDevice"></param>
        /// <param name="logging"></param>
        void DrawSimple(GraphicsDeviceControl destination, bool logging)
        {
            if (RenderSurfaceMaterial != null)
            {                
                destination.GraphicsDevice.SetRenderTarget(destination.DefaultRenderTarget);
            }

            if (logging) Console.WriteLine("  DrawSimple {");
            destination.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 1, 0);
            destination.GraphicsDevice.Clear(Color.White);

            DrawSequences(destination.GraphicsDevice, logging); // Creo que aquí es donde pinta el tren.
            if (logging) Console.WriteLine("  }");

            if (RenderSurfaceMaterial != null)  //TODO Esta parte está petando
            {
                //destination.GraphicsDevice.SetRenderTarget(null);
                //destination.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer | ClearOptions.Stencil, Color.Transparent, 1, 0);
                //RenderSurfaceMaterial.SetState(destination.GraphicsDevice, null);
                //RenderSurfaceMaterial.SpriteBatch.Draw(destination.DefaultRenderTarget, Vector2.Zero, Color.White);
                //RenderSurfaceMaterial.ResetState(destination.GraphicsDevice);                
            }
        }

        void DrawSequences(GraphicsDevice graphicsDevice, bool logging)
        {
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
                    }
                    else
                    {
                        // Opacidad: Material simple... representar en un solo paso.
                        sequenceMaterial.Key.SetState(graphicsDevice, null);
                        if (logging) Console.WriteLine("      {0,-5} * {1}", sequenceMaterial.Value.Count, sequenceMaterial.Key);
                        sequenceMaterial.Key.Render(graphicsDevice, sequenceMaterial.Value, ref XNACameraView, ref XNACameraProjection);
                        sequenceMaterial.Key.ResetState(graphicsDevice);
                    }
                }
                if (logging) Console.WriteLine("    }");
            }
        }

    }
}
