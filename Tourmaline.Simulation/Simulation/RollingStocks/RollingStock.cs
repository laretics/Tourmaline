using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tourmaline.Parsers.Msts;
using Tourmaline.Simulation.RollingStocks;

namespace Tourmaline.Simulation.RollingStocks
{
    //Parse abreviado para determinar dónde dirigir el archivo
    public class GenericWAGFile
    {
        public EngineClass Engine;
        public OpenRailsData OpenRails;
        public bool isEngine => null!= Engine;

        public GenericWAGFile(string filenamewithpath) 
        {
            WagFile(filenamewithpath);
        }

        public void WagFile(string filenamewithpath)
        {
            using (STFReader stf = new STFReader(filenamewithpath, false))
                stf.ParseBlock(new STFReader.TokenProcessor[]
                {
                    new STFReader.TokenProcessor("engine", ()=>{ Engine = new EngineClass(stf); }),
                    new STFReader.TokenProcessor("_openrails", () =>{ OpenRails = new OpenRailsData(stf); }),
                });
        }

        public class EngineClass
        {
            public string Type;

            public EngineClass(STFReader stf)
            {
                stf.MustMatch("(");
                stf.ReadString();
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("type", ()=>{ Type = stf.ReadStringBlock(null); }),
                    });
            }
        } // class WAGFil
        public class OpenRailsData
        {
            public string DLL;

            public OpenRailsData(STFReader stf)
            {
                stf.MustMatch("(");
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("ortsdll", ()=>{ DLL = stf.ReadStringBlock(null); }),
                    });
            }
        } // class WAGFile.Engine
    }

    //Clase de utilidad para evitar cargar múltiples copias del mismo archivo
    public class SharedGenericWAGFileManager
    {
        private static Dictionary<string, GenericWAGFile> SharedWAGFiles = new Dictionary<string, GenericWAGFile>();

        public static GenericWAGFile Get(string path)
        {
            if(!SharedWAGFiles.ContainsKey(path))
            {
                GenericWAGFile wagFile = new GenericWAGFile(path);
                SharedWAGFiles.Add(path, wagFile);
                return wagFile;
            }
            else
            {
                return SharedWAGFiles[path];
            }
        }
    }

    public class RollingStock
    {
        public static TrainCar Load(string wagFilePath, bool initialize = true)
        {
            GenericWAGFile wagFile = SharedGenericWAGFileManager.Get(wagFilePath);
            TrainCar coche;
            //Coche de viajeros
            coche = new MSTSWagon(wagFilePath);
            MSTSWagon vagon = coche as MSTSWagon;
            if(null!=coche)
            {
                vagon.Load();
                if (initialize)
                    vagon.Initialize();
            }
            return coche;
        }
    }
}
