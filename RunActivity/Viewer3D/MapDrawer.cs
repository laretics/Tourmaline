using ACadSharp;
using ACadSharp.IO;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using TOURMALINE.Common;
using ACadSharp.Tables;
using ACadSharp.Entities;

namespace Tourmaline.Viewer3D
{
    public class MapDrawer
    {
        readonly Viewer viewer;
        //Cartografía CAD del mapa        
        public CadDocument DefaultMap { get; private set; }
        public string mapFileName { get; set; }
        public Microsoft.Xna.Framework.Point location { get; set; }

        [CallOnThread("Loader")]
        public void Load()
        {
            //Carga el archivo dwg en el visualizador por defecto
            if (File.Exists(mapFileName))
            {
                DwgReader reader = new DwgReader(mapFileName);
                CadDocument document = new CadDocument();
                document = reader.Read();
                this.DefaultMap = document;
                location = new Microsoft.Xna.Framework.Point();
            }
        }

        [CallOnThread("Updater")]
        public void LoadPrep()
        {
            //Carga en una lista SOLO los componentes que se van a usar para
            //hacer render. Esto evita sobrecargar el renderizador.
        }

        [CallOnThread("Updater")]
        public void PrepareFrame(RenderFrame frame, long elapsedTime)
        {
            foreach (Entity entidad in DefaultMap.Entities)
                drawEntity(entidad, frame);
        }

        private void drawEntity(Entity entity,RenderFrame frame)
        {
            Type tipo = entity.GetType();
            if(typeof(ACadSharp.Entities.Polyline3D)==tipo) 
            { 
            
            }
            else if (typeof(ACadSharp.Entities.LwPolyline) == tipo)
            {

            }
            else if (typeof(ACadSharp.Entities.Insert) == tipo)
            {

            }
            else if (typeof(ACadSharp.Entities.TextEntity) == tipo)
            {

            }
            else
            {

            }
        }

    }
}
