using ACadSharp.Entities;
using ACadSharp.IO.Templates;
using ACadSharp.Objects;
using ACadSharp.Tables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ACadSharp.IO.DXF
{
    internal class DxfDocumentBuilder : CadDocumentBuilder
    {
        public DxfReaderConfiguration Configuration { get; }

        public CadBlockRecordTemplate ModelSpaceTemplate { get; set; }

        public HashSet<ulong> ModelSpaceEntities { get; } = new HashSet<ulong>();

        public override bool KeepUnknownEntities => this.Configuration.KeepUnknownEntities;

        public DxfDocumentBuilder(ACadVersion version, CadDocument document, DxfReaderConfiguration configuration) : base(version, document)
        {
            this.Configuration = configuration;
        }

        public override void BuildDocument()
        {
            this.buildDictionaries();

            if (this.ModelSpaceTemplate == null)
            {
                BlockRecord record = BlockRecord.ModelSpace;
                this.BlockRecords.Add(record);
                this.ModelSpaceTemplate = new CadBlockRecordTemplate(record);
                this.AddTemplate(this.ModelSpaceTemplate);
            }

            this.ModelSpaceTemplate.OwnedObjectsHandlers.AddRange(this.ModelSpaceEntities);

            this.RegisterTables();

            this.BuildTables();

            //Assign the owners for the different objects
            foreach (CadTemplate template in this.cadObjectsTemplates.Values)
            {
                this.assignOwner(template);
            }

            base.BuildDocument();
        }

        public List<Entity> BuildEntities()
        {
            var entities = new List<Entity>();

            foreach (CadEntityTemplate item in this.cadObjectsTemplates.Values.OfType<CadEntityTemplate>())
            {
                item.Build(this);

                item.SetUnlinkedReferences();
            }

            foreach (var item in this.cadObjectsTemplates.Values
                .OfType<CadEntityTemplate>()
                .Where(o => o.CadObject.Owner == null))
            {
                entities.Add(item.CadObject);
            }

            return entities;
        }

        private void assignOwner(CadTemplate template)
        {
            if (template.CadObject.Owner != null || template.CadObject is CadDictionary || !template.OwnerHandle.HasValue)
                return;

            if (this.TryGetObjectTemplate(template.OwnerHandle, out CadTemplate owner))
            {
                Type tipo = owner.GetType();                
                if(tipo==typeof(CadDictionaryTemplate))
                {
                    //No hace nada. Las entradas del diccionario se asignan en la plantilla
                }
                else
                {
                    Type tipoObjeto = template.CadObject.GetType();
                    if (tipo == typeof(CadBlockRecordTemplate) && tipoObjeto == typeof(Entity))
                    {
                        CadBlockRecordTemplate record = (CadBlockRecordTemplate)owner;
                        Entity entidad = (Entity)template.CadObject;
                        record.OwnedObjectsHandlers.Add(entidad.Handle);
                    }
                    else if(tipo==typeof(CadPolyLineTemplate))
                    {
                        CadPolyLineTemplate pline = (CadPolyLineTemplate)owner;
                        if(tipoObjeto==typeof(Vertex))
                        {
                            Vertex v = (Vertex)template.CadObject;
                            pline.VertexHandles.Add(v.Handle);
                        }
                        else if(tipoObjeto ==typeof(Seqend))
                        {
                            Seqend seqend =(Seqend)template.CadObject;
                            pline.SeqendHandle=seqend.Handle;
                        }                       
                        else
                        {
                            this.Notify($"Owner {owner.GetType().Name} with handle {template.OwnerHandle} assignation not implemented for {template.CadObject.GetType().Name} with handle {template.CadObject.Handle}");
                        }
                    }
                    else if(tipo ==typeof(CadInsertTemplate))
                    {
                        CadInsertTemplate insert = (CadInsertTemplate)owner;
                        if(tipoObjeto==typeof(AttributeEntity))
                        {
                            AttributeEntity att = (AttributeEntity)template.CadObject;
                            insert.AttributesHandles.Add(att.Handle);
                        }
                        else if(tipoObjeto==typeof(Seqend))
                        {
                            Seqend seqend = (Seqend)template.CadObject;
                            insert.SeqendHandle = seqend.Handle;
                        }
                        else
                        {
                            this.Notify($"Owner {owner.GetType().Name} with handle {template.OwnerHandle} assignation not implemented for {template.CadObject.GetType().Name} with handle {template.CadObject.Handle}");
                        }
                    }
                    else
                    {
                        this.Notify($"Owner {owner.GetType().Name} with handle {template.OwnerHandle} assignation not implemented for {template.CadObject.GetType().Name} with handle {template.CadObject.Handle}");
                    }
                }

            }
            else
            {
                this.Notify($"Owner {template.OwnerHandle} not found for {template.GetType().FullName} with handle {template.CadObject.Handle}");
            }
        }
    }
}
