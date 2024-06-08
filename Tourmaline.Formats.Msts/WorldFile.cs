using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Tourmaline.Parsers.Msts;
using TOURMALINE.Common;

namespace Tourmaline.Formats.Msts
{
    internal class WorldFile
    {
    }
    /// <summary>
    /// Super-class for similar track items SidingObj and PlatformObj.
    /// </summary>
    public class TrObject : WorldObject
    {
        public readonly List<TrItemId> trItemIDList = new List<TrItemId>();

        // this one called by PlatformObj
        public TrObject()
        { }

        // this one called by SidingObj
        public TrObject(SBR block, int detailLevel)
        {
            StaticDetailLevel = detailLevel;

            while (!block.EndOfBlock())
            {
                using (var subBlock = block.ReadSubBlock())
                {
                    switch (subBlock.ID)
                    {
                        case TokenID.UiD: UID = subBlock.ReadUInt(); break;
                        case TokenID.TrItemId: trItemIDList.Add(new TrItemId(subBlock)); break;
                        case TokenID.StaticFlags: StaticFlags = subBlock.ReadFlags(); break;
                        case TokenID.Position: Position = new STFPositionItem(subBlock); break;
                        case TokenID.QDirection: QDirection = new STFQDirectionItem(subBlock); break;
                        case TokenID.VDbId: VDbId = subBlock.ReadUInt(); break;
                        default: subBlock.Skip(); break;
                    }
                }
            }
        }

        public int getTrItemID(int index)
        {
            var i = 0;
            foreach (var tID in trItemIDList)
            {
                if (tID.db == 0)
                {
                    if (index == i)
                        return tID.dbID;
                    i++;
                }
            }
            return -1;
        }

        public class TrItemId
        {
            public readonly int db, dbID;

            public TrItemId(SBR block)
            {
                block.VerifyID(TokenID.TrItemId);
                db = block.ReadInt();
                dbID = block.ReadInt();
                block.VerifyEndOfBlock();
            }
        }
    }



    public abstract class WorldObject
    {
        public string FileName;
        public uint UID;
        public STFPositionItem Position;
        public STFQDirectionItem QDirection;
        public Matrix3x3 Matrix3x3;
        public int StaticDetailLevel;
        public uint StaticFlags;
        public uint VDbId;

        public virtual void AddOrModifyObj(SBR subBlock)
        {

        }

        public void ReadBlock(SBR block)
        {
            while (!block.EndOfBlock())
            {
                using (var subBlock = block.ReadSubBlock())
                {
                    if (subBlock.ID == TokenID.UiD) UID = subBlock.ReadUInt();
                    else
                    {
                        AddOrModifyObj(subBlock);
                    }
                }
            }
        }
    }

    public class STFPositionItem : TWorldPosition
    {
        public STFPositionItem(TWorldPosition p)
            : base(p)
        {
        }

        public STFPositionItem(SBR block)
        {
            block.VerifyID(TokenID.Position);
            X = block.ReadFloat();
            Y = block.ReadFloat();
            Z = block.ReadFloat();
            block.VerifyEndOfBlock();
        }
    }

    public class STFQDirectionItem : TWorldDirection
    {
        public STFQDirectionItem(TWorldDirection d)
            : base(d)
        {
        }

        public STFQDirectionItem(SBR block)
        {
            block.VerifyID(TokenID.QDirection);
            A = block.ReadFloat();
            B = block.ReadFloat();
            C = block.ReadFloat();
            D = block.ReadFloat();
            block.VerifyEndOfBlock();
        }
    }

    public class Matrix3x3
    {
        public readonly float AX, AY, AZ, BX, BY, BZ, CX, CY, CZ;

        public Matrix3x3(SBR block)
        {
            block.VerifyID(TokenID.Matrix3x3);
            AX = block.ReadFloat();
            AY = block.ReadFloat();
            AZ = block.ReadFloat();
            BX = block.ReadFloat();
            BY = block.ReadFloat();
            BZ = block.ReadFloat();
            CX = block.ReadFloat();
            CY = block.ReadFloat();
            CZ = block.ReadFloat();
            block.VerifyEndOfBlock();
        }
    }

    public class TWorldDirection
    {
        public float A;
        public float B;
        public float C;
        public float D;

        public TWorldDirection(float a, float b, float c, float d) { A = a; B = b; C = c; D = d; }
        public TWorldDirection() { A = 0; B = 0; C = 0; D = 1; }
        public TWorldDirection(TWorldDirection d) { A = d.A; B = d.B; C = d.C; D = d.D; }

        public void SetBearing(float compassRad)
        {
            var slope = GetSlope();
            SetAngles(compassRad, slope);
        }

        public void SetBearing(float dx, float dz)
        {
            var slope = GetSlope();
            var compassRad = MstsUtility.AngleDxDz(dx, dz);
            SetAngles(compassRad, slope);
        }

        public void Rotate(float radians)  // Rotate around world vertical axis - +degrees is clockwise
        // This rotates about the surface normal
        {
            SetBearing(GetBearing() + radians);
        }

        public void Pivot(float radians)	// This rotates about object Y axis
        {
            radians += GetBearing();
            var slope = GetSlope();
            SetAngles(radians, -slope);
        }

        public void SetAngles(float compassRad, float tiltRad)  // + rad is tilted up or rotated east
        // from http://www.euclideanspace.com/maths/geometry/rotations/conversions/eulerToQuaternion/
        /*
         *  w = Math.sqrt(1.0 + C1 * C2 + C1*C3 - S1 * S2 * S3 + C2*C3) / 2
            x = (C2 * S3 + C1 * S3 + S1 * S2 * C3) / (4.0 * w) 
            y = (S1 * C2 + S1 * C3 + C1 * S2 * S3) / (4.0 * w)
            z = (-S1 * S3 + C1 * S2 * C3 + S2) /(4.0 * w) 


            where:

            C1 = cos(heading) 
            C2 = cos(attitude) 
            C3 = cos(bank) 
            S1 = sin(heading) 
            S2 = sin(attitude) 
            S3 = sin(bank)     it seems in MSTS - tilt forward back is bank
				
        Applied in order of heading, attitude then bank 
        */
        {
            var a1 = compassRad;
            var a2 = 0F;
            var a3 = tiltRad;

            var C1 = (float)Math.Cos(a1);
            var S1 = (float)Math.Sin(a1);
            var C2 = (float)Math.Cos(a2);
            var S2 = (float)Math.Sin(a2);
            var C3 = (float)Math.Cos(a3);
            var S3 = (float)Math.Sin(a3);

            var w = (float)Math.Sqrt(1.0 + C1 * C2 + C1 * C3 - S1 * S2 * S3 + C2 * C3) / 2.0f;
            float x, y, z;

            if (Math.Abs(w) < .000005)
            {
                A = 0.0f;
                B = -1.0f;
                C = 0.0f;
                D = 0.0f;
            }
            else
            {
                x = (float)(-(C2 * S3 + C1 * S3 + S1 * S2 * C3) / (4.0 * w));
                y = (float)(-(S1 * C2 + S1 * C3 + C1 * S2 * S3) / (4.0 * w));
                z = (float)(-(-S1 * S3 + C1 * S2 * C3 + S2) / (4.0 * w));

                A = x;
                B = y;
                C = z;
                D = w;
            }
        }

        public void SetSlope(float tiltRad) // +v is tilted up
        {
            var compassAngleRad = MstsUtility.AngleDxDz(DX(), DZ());
            SetAngles(compassAngleRad, tiltRad);
        }

        public void Tilt(float radians)   // Tilt up the specified number of radians
        {
            SetSlope(GetSlope() + radians);
        }

        public void MakeLevel()  // Remove any tilt from the direction.
        {
            SetSlope(0);
        }

        public float DY()
        {
            var x = -A; // imaginary i part of quaternion
            var y = -B; // imaginary j part of quaternion
            var z = -C; // imaginary k part of quaternion
            var w = D; // real part of quaternionfloat 

            //From http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/
            //p2.x = ( w*w*p1.x + 2*y*w*p1.z - 2*z*w*p1.y + x*x*p1.x + 2*y*x*p1.y + 2*z*x*p1.z - z*z*p1.x - y*y*p1.x );	
            //p2.y = ( 2*x*y*p1.x + y*y*p1.y + 2*z*y*p1.z + 2*w*z*p1.x - z*z*p1.y + w*w*p1.y - 2*x*w*p1.z - x*x*p1.y );	
            //p2.z = ( 2*x*z*p1.x + 2*y*z*p1.y + z*z*p1.z - 2*w*y*p1.x - y*y*p1.z + 2*w*x*p1.y - x*x*p1.z + w*w*p1.z );

            var dy = (2 * z * y - 2 * x * w);
            return dy;
        }

        public float DX()
        {

            // WAS return -2.0*B*D; 

            /* Was
            float x = C;
            float y = A;
            float z = B;
            float w = D;

            return -2.0 * ( x * y + z * w );
            */

            var x = -A;
            var y = -B;
            var z = -C;
            var w = D;

            var dX = (2 * y * w + 2 * z * x);
            return dX;
        }

        public float DZ()
        {
            var x = -A;
            var y = -B;
            var z = -C;
            var w = D;

            return z * z - y * y - x * x + w * w;
        }

        public float GetSlope()   // Return the slope, +radians is tilted up
        {
            // see http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/

            var qx = -A;
            var qy = -B;
            var qz = -C;
            var qw = D;

            //float heading;
            //float attitude;
            float bank;

            if (Math.Abs(qx * qy + qz * qw - 0.5) < .00001)
            {
                //heading = 2 * Math.Atan2(qx,qw);
                bank = 0;
            }
            else if (Math.Abs(qx * qy + qz * qw + 0.5) < .00001)
            {
                //heading = -2 * Math.Atan2(qx,qw);
                bank = 0;
            }

            //heading = Math.Atan2(2*qy*qw-2*qx*qz , 1 - 2*qy*qy - 2*qz*qz);
            //attitude = Math.Asin(2*qx*qy + 2*qz*qw);
            bank = (float)Math.Atan2(2 * qx * qw - 2 * qy * qz, 1 - 2 * qx * qx - 2 * qz * qz);

            return bank;
        }

        public float GetBearing()   // Return the bearing
        {
            // see http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/

            var qx = -A;
            var qy = -B;
            var qz = -C;
            var qw = D;

            float heading;
            //float attitude;
            //float bank;

            if (Math.Abs(qx * qy + qz * qw - 0.5) < .00001)
            {
                heading = 2f * (float)Math.Atan2(qx, qw);
                //bank = 0;
            }
            else if (Math.Abs(qx * qy + qz * qw + 0.5) < .00001)
            {
                heading = -2f * (float)Math.Atan2(qx, qw);
                //bank = 0;
            }
            else
            {
                heading = (float)Math.Atan2(2 * qy * qw - 2 * qx * qz, 1 - 2 * qy * qy - 2 * qz * qz);
                //attitude = Math.Asin(2*qx*qy + 2*qz*qw);
                //bank = Math.Atan2(2*qx*qw-2*qy*qz , 1 - 2*qx*qx - 2*qz*qz);
            }

            return heading;
        }

        public static float AngularDistance(TWorldDirection d1, TWorldDirection d2)
        // number of radians separating angle one and angle two - always positive
        {
            var a1 = d1.GetBearing();
            var a2 = d2.GetBearing();

            var a = a1 - a2;

            a = Math.Abs(a);

            while (a > Math.PI)
                a -= 2.0f * (float)Math.PI;

            return (float)Math.Abs(a);
        }

        /// <summary>
        /// Rotate the specified point in model space to a new location according to the quaternion 
        /// Center of rotation is 0,0,0 in model space
        /// Example   xyz = 0,1,2 rotated 90 degrees east becomes 2,1,0
        /// </summary>
        /// <param name="p1"></param>
        private TWorldPosition RotatePoint(TWorldPosition p1)
        {

            var x = -A; // imaginary i part of quaternion
            var y = -B; // imaginary j part of quaternion
            var z = -C; // imaginary k part of quaternion
            var w = D; // real part of quaternionfloat 

            var p2 = new TWorldPosition();

            p2.X = (w * w * p1.X + 2 * y * w * p1.Z - 2 * z * w * p1.Y + x * x * p1.X + 2 * y * x * p1.Y + 2 * z * x * p1.Z - z * z * p1.X - y * y * p1.X);
            p2.Y = (2 * x * y * p1.X + y * y * p1.Y + 2 * z * y * p1.Z + 2 * w * z * p1.X - z * z * p1.Y + w * w * p1.Y - 2 * x * w * p1.Z - x * x * p1.Y);
            p2.Z = (2 * x * z * p1.X + 2 * y * z * p1.Y + z * z * p1.Z - 2 * w * y * p1.X - y * y * p1.Z + 2 * w * x * p1.Y - x * x * p1.Z + w * w * p1.Z);

            return p2;
        }
    }

    public class TWorldPosition
    {
        public float X;
        public float Y;
        public float Z;

        public TWorldPosition(float x, float y, float z) { X = x; Y = y; Z = z; }
        public TWorldPosition() { X = 0.0f; Y = 0.0f; Z = 0.0f; }
        public TWorldPosition(TWorldPosition p)
        {
            X = p.X;
            Y = p.Y;
            Z = p.Z;
        }

        public void Move(TWorldDirection q, float distance)
        {
            X += (q.DX() * distance);
            Y += (q.DY() * distance);
            Z += (q.DZ() * distance);
        }

        public void Offset(TWorldDirection d, float distanceRight)
        {
            var DRight = new TWorldDirection(d);
            DRight.Rotate(MathHelper.ToRadians(90));
            Move(DRight, distanceRight);
        }

        public static float PointDistance(TWorldPosition p1, TWorldPosition p2)
        // distance between p1 and p2 along the surface
        {
            var dX = p1.X - p2.X;
            var dZ = p1.Z - p2.Z;
            return (float)Math.Sqrt(dX * dX + dZ * dZ);
        }
    }

    public class SignalUnits
    {
        public readonly SignalUnit[] Units;

        public SignalUnits(SBR block)
        {
            var units = new List<SignalUnit>();
            block.VerifyID(TokenID.SignalUnits);
            var count = block.ReadUInt();
            for (var i = 0; i < count; i++)
            {
                using (var subBlock = block.ReadSubBlock())
                {
                    units.Add(new SignalUnit(subBlock));
                }
            }
            block.VerifyEndOfBlock();
            Units = units.ToArray();
        }
    }

    public class SignalUnit
    {
        public readonly int SubObj;
        public readonly uint TrItem;

        public SignalUnit(SBR block)
        {
            block.VerifyID(TokenID.SignalUnit);
            SubObj = block.ReadInt();
            using (var subBlock = block.ReadSubBlock())
            {
                subBlock.VerifyID(TokenID.TrItemId);
                subBlock.ReadUInt(); // Unk?
                TrItem = subBlock.ReadUInt();
                subBlock.VerifyEndOfBlock();
            }
            block.VerifyEndOfBlock();
        }
    }
}
