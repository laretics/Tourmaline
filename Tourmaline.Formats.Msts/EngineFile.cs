using System;
using System.Collections;
using System.IO;
using Tourmaline.Parsers.Msts;

namespace Tourmaline.Formats.Msts
{
    /// <summary>
    /// Work with engine files
    /// </summary>
    public class EngineFile
    {
        public string Name;
        public string Description;
        public string CabViewFile;

        public EngineFile(string filePath)
        {
            Name = Path.GetFileNameWithoutExtension(filePath);
            using (var stf = new STFReader(filePath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("engine", ()=>{
                        stf.ReadString();
                        stf.ParseBlock(new STFReader.TokenProcessor[] {
                            new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                            new STFReader.TokenProcessor("description", ()=>{ Description = stf.ReadStringBlock(null); }),
                            new STFReader.TokenProcessor("cabview", ()=>{ CabViewFile = stf.ReadStringBlock(null); }),
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
