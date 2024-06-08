using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tourmaline.Parsers.Msts;

namespace Tourmaline.Formats.Msts
{

    public class ConsistFile
    {
        public string Name; // from the Name field or label field of the consist file
        public Train_Config Train;

        public ConsistFile(string filePath)
        {
            using (var stf = new STFReader(filePath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("train", ()=>{ Train = new Train_Config(stf); }),
                });
            Name = Train.TrainCfg.Name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class Train_Config
    {
        public TrainCfg TrainCfg;

        public Train_Config(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("traincfg", ()=>{ TrainCfg = new TrainCfg(stf); }),
            });
        }
    }

    public class TrainCfg
    {
        public string Name = "Loose consist.";
        int Serial = 1;
        public MaxVelocity MaxVelocity;
        int NextWagonUID;
        public float Durability = 1.0f;   // Value assumed if attribute not found.

        public List<Wagon> WagonList = new List<Wagon>();

        public TrainCfg(STFReader stf)
        {
            stf.MustMatch("(");
            Name = stf.ReadString();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("serial", ()=>{ Serial = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("maxvelocity", ()=>{ MaxVelocity = new MaxVelocity(stf); }),
                new STFReader.TokenProcessor("nextwagonuid", ()=>{ NextWagonUID = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("durability", ()=>{ Durability = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("wagon", ()=>{ WagonList.Add(new Wagon(stf)); }),
                new STFReader.TokenProcessor("engine", ()=>{ WagonList.Add(new Wagon(stf)); }),
            });
        }
    }

    public class Wagon
    {
        public string Folder;
        public string Name;
        public int UiD;
        public bool IsEngine;
        public bool Flip;

        public Wagon(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("uid", ()=>{ UiD = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("flip", ()=>{ stf.MustMatch("("); stf.MustMatch(")"); Flip = true; }),
                new STFReader.TokenProcessor("enginedata", ()=>{ stf.MustMatch("("); Name = stf.ReadString(); Folder = stf.ReadString(); stf.MustMatch(")"); IsEngine = true; }),
                new STFReader.TokenProcessor("wagondata", ()=>{ stf.MustMatch("("); Name = stf.ReadString(); Folder = stf.ReadString(); stf.MustMatch(")"); }),
            });
        }

        public string GetName(uint uId, List<Wagon> wagonList)
        {
            foreach (var item in wagonList)
            {
                var wagon = item as Wagon;
                if (wagon.UiD == uId)
                {
                    return wagon.Name;
                }
            }
            return "<unknown name>";
        }
    }

    public class MaxVelocity
    {
        public float A;
        public float B = 0.001f;

        public MaxVelocity(STFReader stf)
        {
            stf.MustMatch("(");
            A = stf.ReadFloat(STFReader.UNITS.Speed, null);
            B = stf.ReadFloat(STFReader.UNITS.Speed, null);
            stf.MustMatch(")");
        }
    }

}
