using ACadSharp.Classes;
using ACadSharp.Entities;
using ACadSharp.Header;
using ACadSharp.Objects;
using ACadSharp.Objects.Collections;
using ACadSharp.Tables;
using ACadSharp.Tables.Collections;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace ACadSharp
{
    /// <summary>
    /// Es un dibujo CAD
    /// </summary>
    public class CadDocument : IHandledCadObject
    {
        /// <summary>
        /// El handle del documento es siempre 0. Aquí nos aseguramos de no sobreescribir este valor
        /// </summary>
        public ulong Handle { get { return 0; } }

        /// <summary>
        /// Contiene todas las variables de la cabecera para este documento.
        /// </summary>
        public CadHeader Header { get; internal set; }

        /// <summary>
        /// Acccede a las propiedades de dibujo como el título, asunto, autor y palabras clave
        /// </summary>
        public CadSummaryInfo SummaryInfo { get; set; }

        /// <summary>
        /// Clases Dxf definidas en este documento
        /// </summary>
        public DxfClassCollection Classes { get; set; } = new DxfClassCollection();

        /// <summary>
        /// Colección de todas las aplicaciones registradas en el dibujo
        /// </summary>
        public AppIdsTable AppIds { get; private set; }

        /// <summary>
        /// Colección de todos los bloques del dibujo
        /// </summary>
        public BlockRecordsTable BlockRecords { get; private set; }

        /// <summary>
        /// Colección de todos los estilos de dimensiones del dibujo
        /// </summary>
        public DimensionStylesTable DimensionStyles { get; private set; }

        /// <summary>
        /// Colección de capas del dibujo
        /// </summary>
        public LayersTable Layers { get; private set; }

        /// <summary>
        /// Paleta de todos los estilos de línea
        /// </summary>
        public LineTypesTable LineTypes { get; private set; }

        /// <summary>
        /// Colección de todos los estilos de texto
        /// </summary>
        public TextStylesTable TextStyles { get; private set; }

        /// <summary>
        /// Conjunto de coordenadas de usuario que contiene el dibujo
        /// </summary>
        public UCSTable UCSs { get; private set; }

        /// <summary>
        /// Conjunto de vistas del dibujo
        /// </summary>
        public ViewsTable Views { get; private set; }

        /// <summary>
        /// Colección de puertos de visualización del dibujo
        /// </summary>
        public VPortsTable VPorts { get; private set; }

        /// <summary>        
        /// Colección de configuraciones de visualización del dibujo.
        /// </summary>
        /// <remarks>
        /// Si no existe la entrada <see cref="CadDictionary.AcadLayout"/> en el diccionario raíz, la colección es nula.
        /// </remarks>
        public LayoutCollection Layouts { get; private set; }

        /// <summary>
        /// Colección de agrupamientos de elementos establecidos en el dibujo.
        /// </summary>
        /// <remarks>
        /// La colección es nula si el <see cref="CadDictionary.AcadGroup"/> no existe en el diccionario raíz.
        /// </remarks>
        public ACadSharp.Objects.Collections.GroupCollection Groups { get; private set; }

        /// <summary>
        /// Colección de todas las escalas del dibujo.
        /// </summary>
        /// <remarks>
        /// La colección es nula si la entrada <see cref="CadDictionary.AcadScaleList"/> no figura en el diccionario raíz.
        /// </remarks>
        public ScaleCollection Scales { get; private set; }

        /// <summary>
        /// Colección de estilos multi-línea en el dibujo.
        /// </summary>
        /// <remarks>
        /// La colección es nula si la entrada <see cref="CadDictionary.AcadMLineStyle"/> no está en el diccionario.
        /// </remarks>
        public MLineStyleCollection MLineStyles { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public ImageDefinitionCollection ImageDefinitions { get; private set; }

        /// <summary>
        /// Colección de estilos multi leader del dibujo.
        /// </summary>
        /// <remarks>
        /// La colección será nula si la entrada <see cref="CadDictionary.AcadMLeaderStyle"/> no figura en el diccionario.
        /// </remarks>
        public MLeaderStyleCollection MLeaderStyles { get; private set; }

        /// <summary>
        /// Diccionario raíz del documento
        /// </summary>
        public CadDictionary RootDictionary
        {
            get { return this._rootDictionary; }
            internal set
            {
                this._rootDictionary = value;
                this._rootDictionary.Owner = this;
                this.RegisterCollection(this._rootDictionary);
            }
        }

        /// <summary>
        /// Entidades que se dibujan
        /// </summary>
        public CadObjectCollection<Entity> Entities { get { return this.ModelSpace.Entities; } }

        /// <summary>
        /// Registro de bloque de espacio del modelo que contiene al dibujo
        /// </summary>
        public BlockRecord ModelSpace { get { return this.BlockRecords[BlockRecord.ModelSpaceName]; } }

        /// <summary>
        /// Papel por defecto del modelo
        /// </summary>
        public BlockRecord PaperSpace { get { return this.BlockRecords[BlockRecord.PaperSpaceName]; } }

        private CadDictionary _rootDictionary = null;

        //Diccionario principal que contiene todos los objetos del documento.
        private readonly Dictionary<ulong, IHandledCadObject> _cadObjects = new Dictionary<ulong, IHandledCadObject>();

        internal CadDocument(bool createDefaults)
        {
            this._cadObjects.Add(this.Handle, this);

            if (createDefaults)
            {
                DxfClassCollection.UpdateDxfClasses(this);

                //Header and summary
                this.Header = new CadHeader(this);
                this.SummaryInfo = new CadSummaryInfo();

                //The order of the elements is rellevant for the handles assignation

                //Initialize tables
                this.BlockRecords = new BlockRecordsTable(this);
                this.Layers = new LayersTable(this);
                this.DimensionStyles = new DimensionStylesTable(this);
                this.TextStyles = new TextStylesTable(this);
                this.LineTypes = new LineTypesTable(this);
                this.Views = new ViewsTable(this);
                this.UCSs = new UCSTable(this);
                this.VPorts = new VPortsTable(this);
                this.AppIds = new AppIdsTable(this);

                //Root dictionary
                this.RootDictionary = CadDictionary.CreateRoot();

                //Entries
                Layout modelLayout = Layout.Default;
                Layout paperLayout = new Layout("Layout1");
                (this.RootDictionary[CadDictionary.AcadLayout] as CadDictionary).Add(paperLayout);
                (this.RootDictionary[CadDictionary.AcadLayout] as CadDictionary).Add(modelLayout);

                //Default variables
                this.AppIds.Add(AppId.Default);

                this.LineTypes.Add(LineType.ByLayer);
                this.LineTypes.Add(LineType.ByBlock);
                this.LineTypes.Add(LineType.Continuous);

                this.Layers.Add(Layer.Default);

                this.TextStyles.Add(TextStyle.Default);

                this.DimensionStyles.Add(DimensionStyle.Default);

                this.VPorts.Add(VPort.Default);

                //Blocks
                BlockRecord model = BlockRecord.ModelSpace;
                model.Layout = modelLayout;
                this.BlockRecords.Add(model);

                BlockRecord pspace = BlockRecord.PaperSpace;
                pspace.Layout = paperLayout;
                this.BlockRecords.Add(pspace);

                this.UpdateCollections(false);
            }
        }

        /// <summary>
        /// Genera un documento con los objetos por defecto
        /// </summary>
        /// <remarks>
        /// Default version <see cref="ACadVersion.AC1018"/>
        /// </remarks>
        public CadDocument() : this(ACadVersion.AC1018) { }

        /// <summary>
        /// Crea un documento con los objetos por defecto y una versión específica
        /// </summary>
        /// <param name="version">Version of the document</param>
        public CadDocument(ACadVersion version) : this(true)
        {
            this.Header.Version = version;
        }

        /// <summary>
        /// Obtiene un objeto del documento a partir de su manejador (ulong)
        /// </summary>
        /// <param name="handle"></param>
        /// <returns>the cadObject or null if doesn't exists in the document</returns>
        public CadObject GetCadObject(ulong handle)
        {
            return this.GetCadObject<CadObject>(handle);
        }

        /// <summary>
        /// Obtiene un objeto del documento por su manejador (ulong)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handle"></param>
        /// <returns>the cadObject or null if doesn't exists in the document</returns>
        public T GetCadObject<T>(ulong handle)
            where T : CadObject
        {
            if (this._cadObjects.TryGetValue(handle, out IHandledCadObject obj))
            {
                return obj as T;
            }

            return null;
        }

        /// <summary>
        /// Gets an object in the document by it's handle
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handle"></param>
        /// <param name="cadObject"></param>
        /// <returns></returns>
        public bool TryGetCadObject<T>(ulong handle, out T cadObject)
            where T : CadObject
        {
            cadObject = null;

            if (handle == this.Handle)
                return false;

            if (this._cadObjects.TryGetValue(handle, out IHandledCadObject obj))
            {
                cadObject = obj as T;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the collections in the document and link them to it's dictionary
        /// </summary>
        /// <param name="createDictionaries"></param>
        public void UpdateCollections(bool createDictionaries)
        {
            if (createDictionaries && this.RootDictionary == null)
            {
                this.RootDictionary = CadDictionary.CreateRoot();
            }
            else if (this.RootDictionary == null)
            {
                return;
            }

            if (this.updateCollection(CadDictionary.AcadLayout, createDictionaries, out CadDictionary layout))
            {
                this.Layouts = new LayoutCollection(layout);
            }

            if (this.updateCollection(CadDictionary.AcadGroup, createDictionaries, out CadDictionary groups))
            {
                this.Groups = new GroupCollection(groups);
            }

            if (this.updateCollection(CadDictionary.AcadScaleList, createDictionaries, out CadDictionary scales))
            {
                this.Scales = new ScaleCollection(scales);
            }

            if (this.updateCollection(CadDictionary.AcadMLineStyle, createDictionaries, out CadDictionary mlineStyles))
            {
                this.MLineStyles = new MLineStyleCollection(mlineStyles);
            }

            if (this.updateCollection(CadDictionary.AcadMLeaderStyle, createDictionaries, out CadDictionary mleaderStyles))
            {
                this.MLeaderStyles = new MLeaderStyleCollection(mleaderStyles);
            }

            if (this.updateCollection(CadDictionary.AcadImageDict, createDictionaries, out CadDictionary imageDefinitions))
            {
                this.ImageDefinitions = new ImageDefinitionCollection(imageDefinitions);
            }
        }

        private bool updateCollection(string dictName, bool createDictionary, out CadDictionary dictionary)
        {
            if (this.RootDictionary.TryGetEntry(dictName, out dictionary))
            {
                return true;
            }
            else if (createDictionary)
            {
                dictionary = new CadDictionary(dictName);
                this.RootDictionary.Add(dictionary);
            }

            return dictionary != null;
        }

        private void addCadObject(CadObject cadObject)
        {
            if (cadObject.Document != null)
            {
                throw new ArgumentException($"The item with handle {cadObject.Handle} is already assigned to a document");
            }

            if (cadObject.Handle == 0 || this._cadObjects.ContainsKey(cadObject.Handle))
            {
                var nextHandle = this._cadObjects.Keys.Max() + 1;
                if (nextHandle < this.Header.HandleSeed)
                {
                    nextHandle = this.Header.HandleSeed;
                }

                cadObject.Handle = nextHandle;

                this.Header.HandleSeed = nextHandle + 1;
            }

            this._cadObjects.Add(cadObject.Handle, cadObject);

            if (cadObject is BlockRecord record)
            {
                this.addCadObject(record.BlockEntity);
                this.addCadObject(record.BlockEnd);
            }
            cadObject.AssignDocument(this);
        }

        private void removeCadObject(CadObject cadObject)
        {
            if (!this.TryGetCadObject(cadObject.Handle, out CadObject _)
                || !this._cadObjects.Remove(cadObject.Handle))
            {
                return;
            }

            cadObject.UnassignDocument();
        }

        private void onAdd(object sender, CollectionChangedEventArgs e)
        {
            if (e.Item is CadDictionary dictionary)
            {
                this.RegisterCollection(dictionary);
            }
            else
            {
                this.addCadObject(e.Item);
            }
        }

        private void onRemove(object sender, CollectionChangedEventArgs e)
        {
            if (e.Item is CadDictionary dictionary)
            {
                this.UnregisterCollection(dictionary);
            }
            else
            {
                this.removeCadObject(e.Item);
            }
        }

        internal void RegisterCollection<T>(IObservableCollection<T> collection)
            where T : CadObject
        {
            Type tipo = collection.GetType();
            if(typeof(AppIdsTable)== tipo)
            {
                this.AppIds = (AppIdsTable)collection;
                this.AppIds.Owner = this;
            }
            else if(typeof(BlockRecordsTable) == tipo)
            {
                this.BlockRecords = (BlockRecordsTable)collection;
                this.BlockRecords.Owner = this;

            }
            else if (typeof(DimensionStylesTable) == tipo)
            {
                this.DimensionStyles = (DimensionStylesTable)collection;
                this.DimensionStyles.Owner = this;
            }
            else if (typeof(LayersTable) == tipo)
            {
                this.Layers = (LayersTable)collection;
                this.Layers.Owner = this;
            }
            else if (typeof(LineTypesTable) == tipo)
            {
                this.LineTypes = (LineTypesTable)collection;
                this.LineTypes.Owner = this;
            }
            else if (typeof(TextStylesTable) == tipo)
            {
                this.TextStyles = (TextStylesTable)collection;
                this.TextStyles.Owner = this;
            }
            else if (typeof(UCSTable) == tipo)
            {
                this.UCSs = (UCSTable)collection;
                this.UCSs.Owner = this;
            }
            else if (typeof(ViewsTable) == tipo)
            {
                this.Views = (ViewsTable)collection;
                this.Views.Owner = this;
            }
            else if (typeof(VPortsTable) == tipo)
            {
                this.VPorts = (VPortsTable)collection;
                this.VPorts.Owner = this;
            }

            collection.OnAdd += this.onAdd;
            collection.OnRemove += this.onRemove;

            if (collection is CadObject cadObject)
            {
                this.addCadObject(cadObject);
            }

            if (collection is ISeqendCollection seqendColleciton)
            {
                seqendColleciton.OnSeqendAdded += this.onAdd;
                seqendColleciton.OnSeqendRemoved += this.onRemove;

                if (seqendColleciton.Seqend != null)
                {
                    this.addCadObject(seqendColleciton.Seqend);
                }
            }

            foreach (T item in collection)
            {
                if (item is CadDictionary dictionary)
                {
                    this.RegisterCollection(dictionary);
                }
                else
                {
                    this.addCadObject(item);
                }
            }
        }

        internal void UnregisterCollection<T>(IObservableCollection<T> collection)
            where T : CadObject
        {
            Type tipo = collection.GetType();
            if(tipo == typeof(AppIdsTable) || 
                tipo == typeof(BlockRecordsTable) || 
                tipo == typeof(DimensionStylesTable) || 
                tipo == typeof(LayersTable) || 
                tipo == typeof(LineTypesTable) || 
                tipo == typeof(TextStylesTable) || 
                tipo == typeof(UCSTable) || 
                tipo == typeof(ViewsTable) || 
                tipo == typeof(VPortsTable))
                throw new InvalidOperationException($"The collection {collection.GetType()} cannot be removed from a document.");

            collection.OnAdd -= this.onAdd;
            collection.OnRemove -= this.onRemove;

            if (collection is CadObject cadObject)
            {
                this.removeCadObject(cadObject);
            }

            if (collection is ISeqendCollection seqendColleciton)
            {
                seqendColleciton.OnSeqendAdded -= this.onAdd;
                seqendColleciton.OnSeqendRemoved -= this.onRemove;

                if (seqendColleciton.Seqend != null)
                {
                    this.removeCadObject(seqendColleciton.Seqend);
                }
            }

            foreach (T item in collection)
            {
                if (item is CadDictionary dictionary)
                {
                    this.UnregisterCollection(dictionary);
                }
                else
                {
                    this.removeCadObject(item);
                }
            }
        }
    }
}
