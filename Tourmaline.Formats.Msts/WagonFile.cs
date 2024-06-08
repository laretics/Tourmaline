using System;
using System.Collections;
using System.IO;
using Tourmaline.Parsers.Msts;

namespace Tourmaline.Formats.Msts
{
    /// <summary>
    /// Work with wagon files
    /// </summary>
    public class WagonFile
    {
        public string Name;

        public WagonFile(string filePath)
        {
            Name = Path.GetFileNameWithoutExtension(filePath);
            using (var stf = new STFReader(filePath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("wagon", ()=>{
                        stf.ReadString();
                        stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                        });
                    }),
                });
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
