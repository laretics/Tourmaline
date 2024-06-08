using System.Collections.Generic;
using Tourmaline.Parsers.Msts;

namespace Tourmaline.Formats.Msts
{

    // TODO - this is an incomplete parse of the cvf file.
    public class TrackTypesFile : List<TrackTypesFile.TrackType>
    {

        public TrackTypesFile(string filePath)
        {
            using (STFReader stf = new STFReader(filePath, false))
            {
                var count = stf.ReadInt(null);
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("tracktype", ()=>{
                        if (--count < 0)
                            STFException.TraceWarning(stf, "Skipped extra TrackType");
                        else
                            Add(new TrackType(stf));
                    }),
                });
                if (count > 0)
                    STFException.TraceWarning(stf, count + " missing TrackType(s)");
            }
        }

        public class TrackType
        {
            public string Label;
            public string InsideSound;
            public string OutsideSound;

            public TrackType(STFReader stf)
            {
                stf.MustMatch("(");
                Label = stf.ReadString();
                InsideSound = stf.ReadString();
                OutsideSound = stf.ReadString();
                stf.SkipRestOfBlock();
            }
        } // TrackType

    } // class CVFFile
}

