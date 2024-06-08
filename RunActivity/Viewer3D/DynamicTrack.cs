using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Tourmaline.Formats.Msts;
using Tourmaline.Parsers.Msts;
using Tourmaline.Simulation;
using Tourmaline.Viewer3D.Common;
using TOURMALINE.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using static Tourmaline.Viewer3D.LODItem;

namespace Tourmaline.Viewer3D
{
    internal class DynamicTrack
    {
    }

    // Dynamic track profile class
    public class TrProfile
    {
        public string Name; // e.g., "Default track profile"
        public int ReplicationPitch; //TBD: Replication pitch alternative
        public LODMethods LODMethod = LODMethods.None; // LOD method of control
        public float ChordSpan; // Base method: No. of profiles generated such that span is ChordSpan degrees
        // If a PitchControl is defined, then the base method is compared to the PitchControl method,
        // and the ChordSpan is adjusted to compensate.
        public PitchControls PitchControl = PitchControls.None; // Method of control for profile replication pitch
        public float PitchControlScalar; // Scalar parameter for PitchControls
        public ArrayList LODs = new ArrayList(); // Array of Levels-Of-Detail

        /// <summary>
        /// Enumeration of LOD control methods
        /// </summary>
        public enum LODMethods
        {
            /// <summary>
            /// None -- No LODMethod specified; defaults to ComponentAdditive.
            /// </summary>
            None = 0,

            /// <summary>
            /// ComponentAdditive -- Each LOD is a COMPONENT that is ADDED as the camera gets closer.
            /// </summary>
            ComponentAdditive = 1,

            /// <summary>
            /// CompleteReplacement -- Each LOD group is a COMPLETE model that REPLACES another as the camera moves.
            /// </summary>
            CompleteReplacement = 2
        }

        /// <summary>
        /// Enumeration of cross section replication pitch control methods.
        /// </summary>
        public enum PitchControls
        {
            /// <summary>
            /// None -- No pitch control method specified.
            /// </summary>
            None = 0,

            /// <summary>
            /// ChordLength -- Constant length of chord.
            /// </summary>
            ChordLength,

            /// <summary>
            /// Chord Displacement -- Constant maximum displacement of chord from arc.
            /// </summary>
            ChordDisplacement
        }

        /// <summary>
        /// TrProfile constructor (default - builds from self-contained data)
        /// <param name="viewer">Viewer.</param>
        /// </summary>
        public TrProfile(Viewer viewer)
        {
            // Default TrProfile constructor

            Name = "Default Dynatrack profile";
            LODMethod = LODMethods.ComponentAdditive;
            ChordSpan = 1.0f; // Base Method: Generates profiles spanning no more than 1 degree

            PitchControl = PitchControls.ChordLength;       // Target chord length
            PitchControlScalar = 10.0f;                     // Hold to no more than 10 meters
            //PitchControl = PitchControls.ChordDisplacement; // Target chord displacement from arc
            //PitchControlScalar = 0.034f;                    // Hold to no more than 34 mm (half rail width)

            LOD lod;            // Local LOD instance
            LODItem lodItem;    // Local LODItem instance
            Polyline pl;        // Local Polyline instance

            // RAILSIDES
            lod = new LOD(700.0f); // Create LOD for railsides with specified CutoffRadius
            lodItem = new LODItem("Railsides");
            lodItem.TexName = "acleantrack2.ace";
            lodItem.ShaderName = "TexDiff";
            lodItem.LightModelName = "OptSpecular0";
            lodItem.AlphaTestMode = 0;
            lodItem.TexAddrModeName = "Wrap";
            lodItem.ESD_Alternative_Texture = 0;
            lodItem.MipMapLevelOfDetailBias = 0;
            LODItem.LoadMaterial(viewer, lodItem);
            //var gauge = viewer.Simulator.SuperElevationGauge;
            var gauge = 1;
            var inner = gauge / 2f;
            var outer = inner + 0.15f * gauge / 1.435f;

            pl = new Polyline(this, "left_outer", 2);
            pl.DeltaTexCoord = new Vector2(.1673372f, 0f);
            pl.Vertices.Add(new Vertex(-outer, .200f, 0.0f, -1f, 0f, 0f, -.139362f, .101563f));
            pl.Vertices.Add(new Vertex(-outer, .325f, 0.0f, -1f, 0f, 0f, -.139363f, .003906f));
            lodItem.Polylines.Add(pl);
            lodItem.Accum(pl.Vertices.Count);

            pl = new Polyline(this, "left_inner", 2);
            pl.DeltaTexCoord = new Vector2(.1673372f, 0f);
            pl.Vertices.Add(new Vertex(-inner, .325f, 0.0f, 1f, 0f, 0f, -.139363f, .003906f));
            pl.Vertices.Add(new Vertex(-inner, .200f, 0.0f, 1f, 0f, 0f, -.139362f, .101563f));
            lodItem.Polylines.Add(pl);
            lodItem.Accum(pl.Vertices.Count);

            pl = new Polyline(this, "right_inner", 2);
            pl.DeltaTexCoord = new Vector2(.1673372f, 0f);
            pl.Vertices.Add(new Vertex(inner, .200f, 0.0f, -1f, 0f, 0f, -.139362f, .101563f));
            pl.Vertices.Add(new Vertex(inner, .325f, 0.0f, -1f, 0f, 0f, -.139363f, .003906f));
            lodItem.Polylines.Add(pl);
            lodItem.Accum(pl.Vertices.Count);

            pl = new Polyline(this, "right_outer", 2);
            pl.DeltaTexCoord = new Vector2(.1673372f, 0f);
            pl.Vertices.Add(new Vertex(outer, .325f, 0.0f, 1f, 0f, 0f, -.139363f, .003906f));
            pl.Vertices.Add(new Vertex(outer, .200f, 0.0f, 1f, 0f, 0f, -.139362f, .101563f));
            lodItem.Polylines.Add(pl);
            lodItem.Accum(pl.Vertices.Count);

            lod.LODItems.Add(lodItem); // Append this LODItem to LODItems array
            LODs.Add(lod); // Append this LOD to LODs array

            // RAILTOPS
            lod = new LOD(1200.0f); // Create LOD for railtops with specified CutoffRadius
            // Single LODItem in this case
            lodItem = new LODItem("Railtops");
            lodItem.TexName = "acleantrack2.ace";
            lodItem.ShaderName = "TexDiff";
            lodItem.LightModelName = "OptSpecular25";
            lodItem.AlphaTestMode = 0;
            lodItem.TexAddrModeName = "Wrap";
            lodItem.ESD_Alternative_Texture = 0;
            lodItem.MipMapLevelOfDetailBias = 0;
            LODItem.LoadMaterial(viewer, lodItem);

            pl = new Polyline(this, "right", 2);
            pl.DeltaTexCoord = new Vector2(.0744726f, 0f);
            pl.Vertices.Add(new Vertex(-outer, .325f, 0.0f, 0f, 1f, 0f, .232067f, .126953f));
            pl.Vertices.Add(new Vertex(-inner, .325f, 0.0f, 0f, 1f, 0f, .232067f, .224609f));
            lodItem.Polylines.Add(pl);
            lodItem.Accum(pl.Vertices.Count);

            pl = new Polyline(this, "left", 2);
            pl.DeltaTexCoord = new Vector2(.0744726f, 0f);
            pl.Vertices.Add(new Vertex(inner, .325f, 0.0f, 0f, 1f, 0f, .232067f, .126953f));
            pl.Vertices.Add(new Vertex(outer, .325f, 0.0f, 0f, 1f, 0f, .232067f, .224609f));
            lodItem.Polylines.Add(pl);
            lodItem.Accum(pl.Vertices.Count);

            lod.LODItems.Add(lodItem); // Append this LODItem to LODItems array
            LODs.Add(lod); // Append this LOD to LODs array

            // BALLAST
            lod = new LOD(float.MaxValue); // Create LOD for ballast with specified CutoffRadius (infinite)
            // Single LODItem in this case
            lodItem = new LODItem("Ballast");
            lodItem.TexName = "acleantrack1.ace";
            lodItem.ShaderName = "BlendATexDiff";
            lodItem.LightModelName = "OptSpecular0";
            lodItem.AlphaTestMode = 0;
            lodItem.TexAddrModeName = "Wrap";
            lodItem.ESD_Alternative_Texture = (int)Helpers.TextureFlags.SnowTrack; // Match MSTS global road/track behaviour.
            lodItem.MipMapLevelOfDetailBias = -1f;
            LODItem.LoadMaterial(viewer, lodItem);

            pl = new Polyline(this, "ballast", 2);
            pl.DeltaTexCoord = new Vector2(0.0f, 0.2088545f);
            pl.Vertices.Add(new Vertex(-2.5f * gauge / 1.435f, 0.2f, 0.0f, 0f, 1f, 0f, -.153916f, -.280582f));
            pl.Vertices.Add(new Vertex(2.5f * gauge / 1.435f, 0.2f, 0.0f, 0f, 1f, 0f, .862105f, -.280582f));
            lodItem.Polylines.Add(pl);
            lodItem.Accum(pl.Vertices.Count);

            lod.LODItems.Add(lodItem); // Append this LODItem to LODItems array
            LODs.Add(lod); // Append this LOD to LODs array
        }

        /// <summary>
        /// TrProfile constructor from STFReader-style profile file
        /// </summary>
        public TrProfile(Viewer viewer, STFReader stf)
        {
            Name = "Default Dynatrack profile";

            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("lodmethod", ()=> { LODMethod = GetLODMethod(stf.ReadStringBlock(null)); }),
                new STFReader.TokenProcessor("chordspan", ()=>{ ChordSpan = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("pitchcontrol", ()=> { PitchControl = GetPitchControl(stf.ReadStringBlock(null)); }),
                new STFReader.TokenProcessor("pitchcontrolscalar", ()=>{ PitchControlScalar = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("lod", ()=> { LODs.Add(new LOD(viewer, stf)); }),
            });

            if (LODs.Count == 0) throw new Exception("missing LODs");
        }

        /// <summary>
        /// TrProfile constructor from XML profile file
        /// </summary>
        public TrProfile(Viewer viewer, XmlReader reader)
        {
            if (reader.IsStartElement())
            {
                if (reader.Name == "TrProfile")
                {
                    // root
                    Name = reader.GetAttribute("Name");
                    LODMethod = GetLODMethod(reader.GetAttribute("LODMethod"));
                    ChordSpan = float.Parse(reader.GetAttribute("ChordSpan"));
                    PitchControl = GetPitchControl(reader.GetAttribute("PitchControl"));
                    PitchControlScalar = float.Parse(reader.GetAttribute("PitchControlScalar"));
                }
                else
                {
                    //TODO: Need to handle ill-formed XML profile
                }
            }
            LOD lod = null;
            LODItem lodItem = null;
            Polyline pl = null;
            Vertex v;
            string[] s;
            char[] sep = new char[] { ' ' };
            while (reader.Read())
            {
                if (reader.IsStartElement())
                {
                    switch (reader.Name)
                    {
                        case "LOD":
                            lod = new LOD(float.Parse(reader.GetAttribute("CutoffRadius")));
                            LODs.Add(lod);
                            break;
                        case "LODItem":
                            lodItem = new LODItem(reader.GetAttribute("Name"));
                            lodItem.TexName = reader.GetAttribute("TexName");

                            lodItem.ShaderName = reader.GetAttribute("ShaderName");
                            lodItem.LightModelName = reader.GetAttribute("LightModelName");
                            lodItem.AlphaTestMode = int.Parse(reader.GetAttribute("AlphaTestMode"));
                            lodItem.TexAddrModeName = reader.GetAttribute("TexAddrModeName");
                            lodItem.ESD_Alternative_Texture = int.Parse(reader.GetAttribute("ESD_Alternative_Texture"));
                            lodItem.MipMapLevelOfDetailBias = float.Parse(reader.GetAttribute("MipMapLevelOfDetailBias"));

                            LODItem.LoadMaterial(viewer, lodItem);
                            lod.LODItems.Add(lodItem);
                            break;
                        case "Polyline":
                            pl = new Polyline();
                            pl.Name = reader.GetAttribute("Name");
                            s = reader.GetAttribute("DeltaTexCoord").Split(sep, StringSplitOptions.RemoveEmptyEntries);
                            pl.DeltaTexCoord = new Vector2(float.Parse(s[0]), float.Parse(s[1]));
                            lodItem.Polylines.Add(pl);
                            break;
                        case "Vertex":
                            v = new Vertex();
                            s = reader.GetAttribute("Position").Split(sep, StringSplitOptions.RemoveEmptyEntries);
                            v.Position = new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
                            s = reader.GetAttribute("Normal").Split(sep, StringSplitOptions.RemoveEmptyEntries);
                            v.Normal = new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
                            s = reader.GetAttribute("TexCoord").Split(sep, StringSplitOptions.RemoveEmptyEntries);
                            v.TexCoord = new Vector2(float.Parse(s[0]), float.Parse(s[1]));
                            pl.Vertices.Add(v);
                            lodItem.NumVertices++; // Bump vertex count
                            if (pl.Vertices.Count > 1) lodItem.NumSegments++;
                            break;
                        default:
                            break;
                    }
                }
            }
            if (LODs.Count == 0) throw new Exception("missing LODs");
        }

        /// <summary>
        /// TrProfile constructor (default - builds from self-contained data)
        /// <param name="viewer">Viewer3D.</param>
        /// <param name="x">Parameter x is a placeholder.</param>
        /// </summary>
        public TrProfile(Viewer viewer, int x)
        {
            // Default TrProfile constructor
            Name = "Default Dynatrack profile";
        }

        /// <summary>
        /// Gets a member of the LODMethods enumeration that corresponds to sLODMethod.
        /// </summary>
        /// <param name="sLODMethod">String that identifies desired LODMethod.</param>
        /// <returns>LODMethod</returns>
        public static LODMethods GetLODMethod(string sLODMethod)
        {
            string s = sLODMethod.ToLower();
            switch (s)
            {
                case "none":
                    return LODMethods.None;

                case "completereplacement":
                    return LODMethods.CompleteReplacement;

                case "componentadditive":
                default:
                    return LODMethods.ComponentAdditive;
            }
        }

        /// <summary>
        /// Gets a member of the PitchControls enumeration that corresponds to sPitchControl.
        /// </summary>
        /// <param name="sPitchControl">String that identifies desired PitchControl.</param>
        /// <returns></returns>
        public static PitchControls GetPitchControl(string sPitchControl)
        {
            string s = sPitchControl.ToLower();
            switch (s)
            {
                case "chordlength":
                    return PitchControls.ChordLength;

                case "chorddisplacement":
                    return PitchControls.ChordDisplacement;

                case "none":
                default:
                    return PitchControls.None; ;

            }
        }
    }

    public class Polyline
    {
        public ArrayList Vertices = new ArrayList();    // Array of vertices 

        public string Name;                             // e.g., "1:1 embankment"
        public Vector2 DeltaTexCoord;                   // Incremental change in (u, v) from one cross section to the next

        /// <summary>
        /// Polyline constructor (DAT)
        /// </summary>
        public Polyline(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("vertex", ()=>{ Vertices.Add(new Vertex(stf)); }),
                new STFReader.TokenProcessor("deltatexcoord", ()=>{
                    stf.MustMatch("(");
                    DeltaTexCoord.X = stf.ReadFloat(STFReader.UNITS.None, null);
                    DeltaTexCoord.Y = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
            });
            // Checks for required member variables: 
            // Name not required.
            if (DeltaTexCoord == Vector2.Zero) throw new Exception("missing DeltaTexCoord");
            if (Vertices.Count == 0) throw new Exception("missing Vertices");
        }

        /// <summary>
        /// Bare-bones Polyline constructor (used for XML)
        /// </summary>
        public Polyline()
        {
        }

        /// <summary>
        /// Polyline constructor (default)
        /// </summary>
        public Polyline(TrProfile parent, string name, uint num)
        {
            Name = name;
        }
    }

    public struct Vertex
    {
        public Vector3 Position;                           // Position vector (x, y, z)
        public Vector3 Normal;                             // Normal vector (nx, ny, nz)
        public Vector2 TexCoord;                           // Texture coordinate (u, v)

        // Vertex constructor (default)
        public Vertex(float x, float y, float z, float nx, float ny, float nz, float u, float v)
        {
            Position = new Vector3(x, y, z);
            Normal = new Vector3(nx, ny, nz);
            TexCoord = new Vector2(u, v);
        }

        // Vertex constructor (DAT)
        public Vertex(STFReader stf)
        {
            Vertex v = new Vertex(); // Temp variable used to construct the struct in ParseBlock
            v.Position = new Vector3();
            v.Normal = new Vector3();
            v.TexCoord = new Vector2();
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("position", ()=>{
                    stf.MustMatch("(");
                    v.Position.X = stf.ReadFloat(STFReader.UNITS.None, null);
                    v.Position.Y = stf.ReadFloat(STFReader.UNITS.None, null);
                    v.Position.Z = 0.0f;
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("normal", ()=>{
                    stf.MustMatch("(");
                    v.Normal.X = stf.ReadFloat(STFReader.UNITS.None, null);
                    v.Normal.Y = stf.ReadFloat(STFReader.UNITS.None, null);
                    v.Normal.Z = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("texcoord", ()=>{
                    stf.MustMatch("(");
                    v.TexCoord.X = stf.ReadFloat(STFReader.UNITS.None, null);
                    v.TexCoord.Y = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
            });
            this = v;
            // Checks for required member variables
            // No way to check for missing Position.
            if (Normal == Vector3.Zero) throw new Exception("improper Normal");
            // No way to check for missing TexCoord
        }
    }

    public class LOD
    {
        public float CutoffRadius; // Distance beyond which LODItem is not seen
        public ArrayList LODItems = new ArrayList(); // Array of arrays of LODItems
        public int PrimIndexStart; // Start index of ShapePrimitive block for this LOD
        public int PrimIndexStop;

        /// <summary>
        /// LOD class constructor
        /// </summary>
        /// <param name="cutoffRadius">Distance beyond which LODItem is not seen</param>
        public LOD(float cutoffRadius)
        {
            CutoffRadius = cutoffRadius;
        }

        public LOD(Viewer viewer, STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("cutoffradius", ()=>{ CutoffRadius = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); }),
                new STFReader.TokenProcessor("loditem", ()=>{
                    LODItem lodItem = new LODItem(viewer, stf);
                    LODItems.Add(lodItem); // Append to Polylines array
                    }),
            });
            if (CutoffRadius == 0) throw new Exception("missing CutoffRadius");
        }

        [CallOnThread("Loader")]
        public void Mark()
        {
            foreach (LODItem lodItem in LODItems)
                lodItem.Mark();
        }
    }

    public class LODItem
    {
        public ArrayList Polylines = new ArrayList();  // Array of arrays of vertices 

        public string Name;                            // e.g., "Rail sides"
        public string ShaderName;
        public string LightModelName;
        public int AlphaTestMode;
        public string TexAddrModeName;
        public int ESD_Alternative_Texture; // Equivalent to that of .sd file
        public float MipMapLevelOfDetailBias;

        public string TexName; // Texture file name

        public Material LODMaterial; // SceneryMaterial reference

        // NumVertices and NumSegments used for sizing vertex and index buffers
        public uint NumVertices;                     // Total independent vertices in LOD
        public uint NumSegments;                     // Total line segment count in LOD

        /// <summary>
        /// LODITem constructor (used for default and XML-style profiles)
        /// </summary>
        public LODItem(string name)
        {
            Name = name;
        }

        /// <summary>
        /// LODITem constructor (used for STF-style profile)
        /// </summary>
        public LODItem(Viewer viewer, STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("name", ()=>{ Name = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("texname", ()=>{ TexName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("shadername", ()=>{ ShaderName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("lightmodelname", ()=>{ LightModelName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("alphatestmode", ()=>{ AlphaTestMode = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("texaddrmodename", ()=>{ TexAddrModeName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("esd_alternative_texture", ()=>{ ESD_Alternative_Texture = stf.ReadIntBlock(null); }),
                new STFReader.TokenProcessor("mipmaplevelofdetailbias", ()=>{ MipMapLevelOfDetailBias = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("polyline", ()=>{
                    Polyline pl = new Polyline(stf);
                    Polylines.Add(pl); // Append to Polylines array
                    //parent.Accum(pl.Vertices.Count); }),
                    Accum(pl.Vertices.Count); }),
            });

            // Checks for required member variables:
            // Name not required.
            // MipMapLevelOfDetail bias initializes to 0.
            if (Polylines.Count == 0) throw new Exception("missing Polylines");

            LoadMaterial(viewer, this);
        }

        public void Accum(int count)
        {
            // Accumulates total independent vertices and total line segments
            // Used for sizing of vertex and index buffers
            NumVertices += (uint)count;
            NumSegments += (uint)count - 1;
        }

        public static void LoadMaterial(Viewer viewer, LODItem lod)
        {
            var options = Helpers.EncodeMaterialOptions(lod);
            lod.LODMaterial = viewer.MaterialManager.Load("Scenery", Helpers.GetRouteTextureFile(viewer.microSim, (Helpers.TextureFlags)lod.ESD_Alternative_Texture, lod.TexName), (int)options, lod.MipMapLevelOfDetailBias);
        }

        [CallOnThread("Loader")]
        public void Mark()
        {
            LODMaterial.Mark();
        }
    }

}
