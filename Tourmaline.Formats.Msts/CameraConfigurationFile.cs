using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Tourmaline.Parsers.Msts;

namespace Tourmaline.Formats.Msts
{
    /// <summary>
    /// Object used by ORTS.Cameras to set up views (3dviewer\camera.cs)
    /// </summary>
    public class CameraConfigurationFile
    {
        public List<Camera> Cameras = new List<Camera>();

        public CameraConfigurationFile(string filename)
        {
            using (STFReader stf = new STFReader(filename, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("camera", ()=>{ Cameras.Add(new Camera(stf)); }),
                });
        }
    }

    /// <summary>
    /// Individual camera object from the config file
    /// </summary>
    public class Camera
    {
        public Camera(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("camtype", ()=>{ CamType = stf.ReadStringBlock(null); CamControl = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("cameraoffset", ()=>{ CameraOffset = stf.ReadVector3Block(STFReader.UNITS.None, CameraOffset); }),
                new STFReader.TokenProcessor("direction", ()=>{ Direction = stf.ReadVector3Block(STFReader.UNITS.None, Direction); }),
                new STFReader.TokenProcessor("objectoffset", ()=>{ ObjectOffset = stf.ReadVector3Block(STFReader.UNITS.None, ObjectOffset); }),
                new STFReader.TokenProcessor("rotationlimit", ()=>{ RotationLimit = stf.ReadVector3Block(STFReader.UNITS.None, RotationLimit); }),
                new STFReader.TokenProcessor("description", ()=>{ Description = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("fov", ()=>{ Fov = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("zclip", ()=>{ ZClip = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("wagonnum", ()=>{ WagonNum = stf.ReadIntBlock(null); }),
            });
        }

        public string CamType;
        public string CamControl;
        public Vector3 CameraOffset = new Vector3();
        public Vector3 Direction = new Vector3();
        public float Fov = 55f;
        public float ZClip = 0.1f;
        public int WagonNum = -1;
        public Vector3 ObjectOffset = new Vector3();
        public Vector3 RotationLimit = new Vector3();
        public string Description = "";

    }


}
