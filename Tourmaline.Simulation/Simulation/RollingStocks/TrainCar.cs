/*
 * Dado que este es el objeto que quiero representar en el display y que en el simulador
 * está relacionado a un montón de parámetros físicos que no tendré en cuenta, voy a hacer
 * una escabechina eliminando directamente todo lo que huela a simulador, para quedarme
 * solo con los parámetros de representación.
 * 
 * Si más adelante quisiera añadir prestaciones, me pondría a importar de nuevo el archivo
 * aunque dudo que tenga tiempo para hacerlo.
*/

using Microsoft.Xna.Framework;
using Tourmaline.Formats.Msts;
using Tourmaline.Parsers.Msts;
using TOURMALINE.Common;
//using TOURMALINE.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Event = Tourmaline.Common.Event;

namespace Tourmaline.Simulation.RollingStocks
{
    public class ViewPoint
    {
        public Vector3 Location;
        public Vector3 StartDirection;
        public Vector3 RotationLimit;

        public ViewPoint()
        {
        }

        public ViewPoint(Vector3 location)
        {
            Location = location;
        }

        public ViewPoint(ViewPoint copy, bool rotate)
        {
            Location = copy.Location;
            StartDirection = copy.StartDirection;
            RotationLimit = copy.RotationLimit;
            if (rotate)
            {
                Location.X *= -1;
                Location.Z *= -1;
                /*StartDirection.X += 180;
                StartDirection.Z += 180;*/
            }
        }
    }

    public abstract class TrainCar
    {
        //public readonly MicroSim Simulator;
        public TourmalineTrain Train; //Tren al que está conectado este coche.
        public readonly string WagFilePath;
        public string RealWagFilePath; //we are substituting missing remote cars in MP, so need to remember this
        public WorldPosition position { get; set; } = new WorldPosition();

        // some properties of this car
        public float CarWidthM = 2.5f;
        public float CarLengthM = 40;       // derived classes must overwrite these defaults
        public float CarHeightM = 4;        // derived classes must overwrite these defaults
        public bool HasFreightAnim = false;
        public bool HasPassengerCapacity = false;
        public bool HasInsideView = false;
        public float CarHeightAboveSeaLevelM;
        public float WagonNumBogies;
        public float CarBogieCentreLengthM;
        public float CarBodyLengthM;
        public float CarCouplerFaceLengthM;

        public int Headlight;

        // instance variables set by train physics when it creates the traincar
        public bool Flipped; // the car is reversed in the consist
        public int UiD;
        public string CarID = "AI"; //CarID = "0 - UID" if player train, "ActivityID - UID" if loose consist, "AI" if AI train

        // status of the traincar - set by the train physics after it calls TrainCar.Update()
        public float _SpeedMpS; // meters per second; updated by train physics, relative to direction of car  50mph = 22MpS
        public float _PrevSpeedMpS;
        public float AbsSpeedMpS; // Math.Abs(SpeedMps) expression is repeated many times in the subclasses, maybe this deserves a class variable
        public TrainCar CarAhead;
        public TrainCar CarBehind;

        public float SpeedMpS
        {
            get
            {
                return _SpeedMpS;
            }
            set
            {
                _SpeedMpS = value;
            }
        }

        // set when model is loaded
        public List<WheelAxle> WheelAxles = new List<WheelAxle>();
        public bool WheelAxlesLoaded;
        public List<TrainCarPart> Parts = new List<TrainCarPart>();

        // For use by cameras, initialized in MSTSWagon class and its derived classes
        public List<ViewPoint> HeadOutViewpoints = new List<ViewPoint>();

        // Used by Curve Speed Method
        protected int WagonNumAxles; // Number of axles on a wagon
        protected float MSTSWagonNumWheels; // Number of axless on a wagon - used to read MSTS value as default

        protected Vector3 InitialCentreOfGravityM = new Vector3(0, 1.8f, 0); // get centre of gravity - read in MSTSWagon
        protected Vector3 CentreOfGravityM = new Vector3(0, 1.8f, 0); // get centre of gravity after adjusted for freight animation

        //public enum WagonTypes
        //{
        //    Unknown,
        //    Engine,
        //    Tender,
        //    Passenger,
        //    Freight,
        //}
        //public WagonTypes WagonType;


        public virtual void Initialize()
        {

        }

        // called when it's time to update the MotiveForce and FrictionForce
        public virtual void Update(float elapsedClockSeconds)
        {
            AbsSpeedMpS = Math.Abs(_SpeedMpS);

        }


        public virtual string GetStatus() { return null; }
        public virtual string GetDebugStatus()
        {
            return string.Empty;
        }

        protected bool WheelHasBeenSet = false; //indicating that the car shape has been loaded, thus no need to reset the wheels

        public TrainCar()
        {
        }

        public virtual void SignalEvent(Event evt) { }

        public TrainCar(string wagFile)
        {
            //Simulator = simulator;
            WagFilePath = wagFile;
            RealWagFilePath = wagFile;
        }

        //================================================================================================//
        /// <summary>
        /// Set starting conditions for TrainCars when initial speed > 0 
        /// 

        public virtual void InitializeMoving()
        {
            _PrevSpeedMpS = SpeedMpS;
        }


        public void AddWheelSet(float offset, int bogieID, int parentMatrix, string wheels, int bogie1Axles, int bogie2Axles)
        {
            if (WheelAxlesLoaded || WheelHasBeenSet)
                return;

            // Currently looking for rolling stock that has more than 3 axles on a bogie.  This is rare, but some models are like this.
            // In this scenario, bogie1 contains 2 sets of axles.  One of them for bogie2.  Both bogie2 axles must be removed.
            // For the time being, the only rail-car that was having issues had 4 axles on one bogie. The second set of axles had a bogie index of 2 and both had to be dropped for the rail-car to operate under OR.
            if (Parts.Count > 0 && bogie1Axles == 4 || bogie2Axles == 4) // 1 bogie will have a Parts.Count of 2.
            {
                if (Parts.Count == 2)
                    if (parentMatrix == Parts[1].iMatrix && wheels.Length == 8)
                        if (bogie1Axles == 4 && bogieID == 2) // This test is strictly testing for and leaving out axles meant for a Bogie2 assignment.
                            return;

                if (Parts.Count == 3)
                {
                    if (parentMatrix == Parts[1].iMatrix && wheels.Length == 8)
                        if (bogie1Axles == 4 && bogieID == 2) // This test is strictly testing for and leaving out axles meant for a Bogie2 assignment.
                            return;
                    if (parentMatrix == Parts[2].iMatrix && wheels.Length == 8)
                        if (bogie2Axles == 4 && bogieID == 1) // This test is strictly testing for and leaving out axles meant for a Bogie1 assignment.
                            return;
                }

            }

            //some old stocks have only two wheels, but defined to have four, two share the same offset, thus all computing of rotations will have problem
            //will check, if so, make the offset different a bit.
            foreach (var axles in WheelAxles)
                if (offset.AlmostEqual(axles.OffsetM, 0.05f)) { offset = axles.OffsetM + 0.7f; break; }

            // Came across a model where the axle offset that is part of a bogie would become 0 during the initial process.  This is something we must test for.
            if (wheels.Length == 8 && Parts.Count > 0)
            {
                if (wheels == "WHEELS11" || wheels == "WHEELS12" || wheels == "WHEELS13" || wheels == "WHEELS14")
                    WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));

                else if (wheels == "WHEELS21" || wheels == "WHEELS22" || wheels == "WHEELS23" || wheels == "WHEELS24")
                    WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));

                else if (wheels == "WHEELS31" || wheels == "WHEELS32" || wheels == "WHEELS33" || wheels == "WHEELS34")
                    WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));

                else if (wheels == "WHEELS41" || wheels == "WHEELS42" || wheels == "WHEELS43" || wheels == "WHEELS44")
                    WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));
                // This else will cover additional Wheels added following the proper naming convention.
                else
                    WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));
            }
            // The else will cover WHEELS spelling where the length is less than 8.
            else
                WheelAxles.Add(new WheelAxle(offset, bogieID, parentMatrix));

        } // end AddWheelSet()

        public void AddBogie(float offset, int matrix, int id, string bogie, int numBogie1, int numBogie2)
        {
            if (WheelAxlesLoaded || WheelHasBeenSet)
                return;
            foreach (var p in Parts) if (p.bogie && offset.AlmostEqual(p.OffsetM, 0.05f)) { offset = p.OffsetM + 0.1f; break; }
            if (bogie == "BOGIE1")
            {
                while (Parts.Count <= id)
                    Parts.Add(new TrainCarPart(0, 0));
                Parts[id].OffsetM = offset;
                Parts[id].iMatrix = matrix;
                Parts[id].bogie = true;//identify this is a bogie, will be used for hold rails on track
            }
            else if (bogie == "BOGIE2")
            {
                // This was the initial problem.  If the shape file contained only one entry that was labeled as BOGIE2(should be BOGIE1)
                // the process would assign 2 to id, causing it to create 2 Parts entries( or 2 bogies) when one was only needed.  It is possible that
                // this issue created many of the problems with articulated wagons later on in the process.
                // 2 would be assigned to id, not because there were 2 entries, but because 2 was in BOGIE2.
                if (numBogie2 == 1 && numBogie1 == 0)
                {
                    id -= 1;
                    while (Parts.Count <= id)
                        Parts.Add(new TrainCarPart(0, 0));
                    Parts[id].OffsetM = offset;
                    Parts[id].iMatrix = matrix;
                    Parts[id].bogie = true;//identify this is a bogie, will be used for hold rails on track
                }
                else
                {
                    while (Parts.Count <= id)
                        Parts.Add(new TrainCarPart(0, 0));
                    Parts[id].OffsetM = offset;
                    Parts[id].iMatrix = matrix;
                    Parts[id].bogie = true;//identify this is a bogie, will be used for hold rails on track
                }
            }
            else if (bogie == "BOGIE3")
            {
                while (Parts.Count <= id)
                    Parts.Add(new TrainCarPart(0, 0));
                Parts[id].OffsetM = offset;
                Parts[id].iMatrix = matrix;
                Parts[id].bogie = true;//identify this is a bogie, will be used for hold rails on track
            }
            else if (bogie == "BOGIE4")
            {
                while (Parts.Count <= id)
                    Parts.Add(new TrainCarPart(0, 0));
                Parts[id].OffsetM = offset;
                Parts[id].iMatrix = matrix;
                Parts[id].bogie = true;//identify this is a bogie, will be used for hold rails on track
            }
            else if (bogie == "BOGIE")
            {
                while (Parts.Count <= id)
                    Parts.Add(new TrainCarPart(0, 0));
                Parts[id].OffsetM = offset;
                Parts[id].iMatrix = matrix;
                Parts[id].bogie = true;//identify this is a bogie, will be used for hold rails on track
            }
            // The else will cover additions not covered above.
            else
            {
                while (Parts.Count <= id)
                    Parts.Add(new TrainCarPart(0, 0));
                Parts[id].OffsetM = offset;
                Parts[id].iMatrix = matrix;
                Parts[id].bogie = true;//identify this is a bogie, will be used for hold rails on track
            }

            WagonNumBogies = Parts.Count - 1;

        } // end AddBogie()

        public void SetUpWheels()
        {

#if DEBUG_WHEELS
            Console.WriteLine(WagFilePath);
            Console.WriteLine("  length {0,10:F4}", LengthM);
            foreach (var w in WheelAxles)
                Console.WriteLine("  axle:  bogie  {1,5:F0}  offset {0,10:F4}", w.OffsetM, w.BogieIndex);
            foreach (var p in Parts)
                Console.WriteLine("  part:  matrix {1,5:F0}  offset {0,10:F4}  weight {2,5:F0}", p.OffsetM, p.iMatrix, p.SumWgt);
#endif
            WheelHasBeenSet = true;
            // No parts means no bogies (always?), so make sure we've got Parts[0] for the car itself.
            if (Parts.Count == 0)
                Parts.Add(new TrainCarPart(0, 0));
            // No axles but we have bogies.
            if (WheelAxles.Count == 0 && Parts.Count > 1)
            {
                // Fake the axles by pretending each has 1 axle.
                foreach (var part in Parts)
                    WheelAxles.Add(new WheelAxle(part.OffsetM, part.iMatrix, 0));
                Trace.TraceInformation("Wheel axle data faked based on {1} bogies for {0}", WagFilePath, Parts.Count - 1);
            }
            bool articFront = !WheelAxles.Any(a => a.OffsetM < 0);
            bool articRear = !WheelAxles.Any(a => a.OffsetM > 0);
            // Validate the axles' assigned bogies and count up the axles on each bogie.
            if (WheelAxles.Count > 0)
            {
                foreach (var w in WheelAxles)
                {
                    if (w.BogieIndex >= Parts.Count)
                        w.BogieIndex = 0;
                    if (w.BogieMatrix > 0)
                    {
                        for (var i = 0; i < Parts.Count; i++)
                            if (Parts[i].iMatrix == w.BogieMatrix)
                            {
                                w.BogieIndex = i;
                                break;
                            }
                    }
                    w.Part = Parts[w.BogieIndex];
                    w.Part.SumWgt++;
                }

                // Make sure the axles are sorted by OffsetM along the car.
                // Attempting to sort car w/o WheelAxles will resort to an error.
                WheelAxles.Sort(WheelAxles[0]);
            }

            //fix bogies with only one wheel set:
            // This process is to fix the bogies that did not pivot under the cab of steam locomotives as well as other locomotives that have this symptom.
            // The cause involved the bogie and axle being close by 0.05f or less on the ZAxis.
            // The ComputePosition() process was unable to work with this.
            // The fix involves first testing for how close they are then moving the bogie offset up.
            // The final fix involves adding an additional axle.  Without this, both bogie and axle would never track properly?
            // Note: Steam locomotive modelers are aware of this issue and are now making sure there is ample spacing between axle and bogie.
            for (var i = 1; i < Parts.Count; i++)
            {
                if (Parts[i].bogie == true && Parts[i].SumWgt < 1.5)
                {
                    foreach (var w in WheelAxles)
                    {
                        if (w.BogieMatrix == Parts[i].iMatrix)
                        {
                            if (w.OffsetM.AlmostEqual(Parts[i].OffsetM, 0.6f))
                            {
                                var w1 = new WheelAxle(w.OffsetM - 0.5f, w.BogieIndex, i);
                                w1.Part = Parts[w1.BogieIndex]; //create virtual wheel
                                w1.Part.SumWgt++;
                                WheelAxles.Add(w1);
                                w.OffsetM += 0.5f; //move the original bogie forward, so we have two bogies to make the future calculation happy
                                Trace.TraceInformation("A virtual wheel axle was added for bogie {1} of {0}", WagFilePath, i);
                                break;
                            }
                        }
                    }
                }
            }

            // Count up the number of bogies (parts) with at least 2 axles.
            for (var i = 1; i < Parts.Count; i++)
                if (Parts[i].SumWgt > 1.5)
                    Parts[0].SumWgt++;

            // This check is for the single axle/bogie issue.
            // Check SumWgt using Parts[0].SumWgt.
            // Certain locomotives do not test well when using Part.SumWgt versus Parts[0].SumWgt.
            // Make sure test using Parts[0] is performed after the above for loop.
            if (!articFront && !articRear && (Parts[0].SumWgt < 1.5))
            {
                foreach (var w in WheelAxles)
                {
                    if (w.BogieIndex >= Parts.Count - 1)
                    {
                        w.BogieIndex = 0;
                        w.Part = Parts[w.BogieIndex];

                    }
                }
            }
            // Using WheelAxles.Count test to control WheelAxlesLoaded flag.
            if (WheelAxles.Count > 2)
            {
                WheelAxles.Sort(WheelAxles[0]);
                WheelAxlesLoaded = true;
            }


#if DEBUG_WHEELS
            Console.WriteLine(WagFilePath);
            Console.WriteLine("  length {0,10:F4}", LengthM);
            Console.WriteLine("  articulated {0}/{1}", articulatedFront, articulatedRear);
            foreach (var w in WheelAxles)
                Console.WriteLine("  axle:  bogie  {1,5:F0}  offset {0,10:F4}", w.OffsetM, w.BogieIndex);
            foreach (var p in Parts)
                Console.WriteLine("  part:  matrix {1,5:F0}  offset {0,10:F4}  weight {2,5:F0}", p.OffsetM, p.iMatrix, p.SumWgt);
#endif
            // Decided to control what is sent to SetUpWheelsArticulation()by using
            // WheelAxlesLoaded as a flag.  This way, wagons that have to be processed are included
            // and the rest left out.
            bool articulatedFront = !WheelAxles.Any(a => a.OffsetM < 0);
            bool articulatedRear = !WheelAxles.Any(a => a.OffsetM > 0);
        } // end SetUpWheels()

        protected void SetUpWheelsArticulation(int carIndex)
        {
            // If there are no forward wheels, this car is articulated (joined
            // to the car in front) at the front. Likewise for the rear.
            bool articulatedFront = !WheelAxles.Any(a => a.OffsetM < 0);
            bool articulatedRear = !WheelAxles.Any(a => a.OffsetM > 0);
            // Original process originally used caused too many issues.
            // The original process did include the below process of just using WheelAxles.Add
            //  if the initial test did not work.  Since the below process is working without issues the
            //  original process was stripped down to what is below
            if (articulatedFront || articulatedRear)
            {
                if (articulatedFront && WheelAxles.Count <= 3)
                    WheelAxles.Add(new WheelAxle(-CarLengthM / 2, 0, 0) { Part = Parts[0] });

                if (articulatedRear && WheelAxles.Count <= 3)
                    WheelAxles.Add(new WheelAxle(CarLengthM / 2, 0, 0) { Part = Parts[0] });

                WheelAxles.Sort(WheelAxles[0]);
            }


#if DEBUG_WHEELS
            Console.WriteLine(WagFilePath);
            Console.WriteLine("  length {0,10:F4}", LengthM);
            Console.WriteLine("  articulated {0}/{1}", articulatedFront, articulatedRear);
            foreach (var w in WheelAxles)
                Console.WriteLine("  axle:  bogie  {1,5:F0}  offset {0,10:F4}", w.OffsetM, w.BogieIndex);
            foreach (var p in Parts)
                Console.WriteLine("  part:  matrix {1,5:F0}  offset {0,10:F4}  weight {2,5:F0}", p.OffsetM, p.iMatrix, p.SumWgt);
#endif
        } // end SetUpWheelsArticulation()
        public void ComputePosition()
        {
            //position = new WorldPosition(rhs);
            Vector3 rhs = position.Location;
            for (var j = 0; j < Parts.Count; j++)
                Parts[j].InitLineFit();
            if (Flipped)
            {
                var o = -CarLengthM / 2 - CentreOfGravityM.Z;
                for (var k = 0; k < WheelAxles.Count; k++)
                {
                    var d = WheelAxles[k].OffsetM - o;
                    o = WheelAxles[k].OffsetM;
                    var x = rhs.X;// + 2048 * (traveler.TileX - tileX);
                    var y = rhs.Y;
                    var z = rhs.Z;// + 2048 * (traveler.TileZ - tileZ);
                    WheelAxles[k].Part.AddWheelSetLocation(1, o, x, y, z, 0);
                }
                o = CarLengthM / 2 - CentreOfGravityM.Z - o;
            }
            else
            {
                var o = CarLengthM / 2 - CentreOfGravityM.Z;
                for (var k = WheelAxles.Count - 1; k >= 0; k--)
                {
                    var d = o - WheelAxles[k].OffsetM;
                    o = WheelAxles[k].OffsetM;
                    var x = rhs.X;// + 2048 * (traveler.TileX - tileX);
                    var y = rhs.Y;
                    var z = rhs.Z;// + 2048 * (traveler.TileZ - tileZ);
                    WheelAxles[k].Part.AddWheelSetLocation(1, o, x, y, z, 0);
                }
                o = CarLengthM / 2 + CentreOfGravityM.Z + o;
            }
            TrainCarPart p0 = Parts[0];
            for (int i = 1; i < Parts.Count; i++)
            {
                TrainCarPart p = Parts[i];
                p.FindCenterLine();
                if (p.SumWgt > 1.5)
                    p0.AddPartLocation(1, p);
            }
            p0.FindCenterLine();
            Vector3 fwd = new Vector3(p0.B[0], p0.B[1], -p0.B[2]);
            // Check if null vector - The Length() is fine also, but may be more time consuming - By GeorgeS
            if (fwd.X != 0 && fwd.Y != 0 && fwd.Z != 0)
                fwd.Normalize();
            Vector3 side = Vector3.Cross(Vector3.Up, fwd);
            // Check if null vector - The Length() is fine also, but may be more time consuming - By GeorgeS
            if (side.X != 0 && side.Y != 0 && side.Z != 0)
                side.Normalize();
            Vector3 up = Vector3.Cross(fwd, side);
            Matrix m = Matrix.Identity;
            m.M11 = side.X;
            m.M12 = side.Y;
            m.M13 = side.Z;
            m.M21 = up.X;
            m.M22 = up.Y;
            m.M23 = up.Z;
            m.M31 = fwd.X;
            m.M32 = fwd.Y;
            m.M33 = fwd.Z;
            m.M41 = p0.A[0];
            m.M42 = p0.A[1] + 0.275f;
            m.M43 = -p0.A[2];

            // calculate truck angles
            for (int i = 1; i < Parts.Count; i++)
            {
                TrainCarPart p = Parts[i];
                if (p.SumWgt < .5)
                    continue;
                if (p.SumWgt < 1.5)
                {   // single axle pony trunk
                    float d = p.OffsetM - p.SumOffset / p.SumWgt;
                    if (-.2 < d && d < .2)
                        continue;
                    p.AddWheelSetLocation(1, p.OffsetM, p0.A[0] + p.OffsetM * p0.B[0], p0.A[1] + p.OffsetM * p0.B[1], p0.A[2] + p.OffsetM * p0.B[2], 0);
                    p.FindCenterLine();
                }
                Vector3 fwd1 = new Vector3(p.B[0], p.B[1], -p.B[2]);
                if (fwd1.X == 0 && fwd1.Y == 0 && fwd1.Z == 0)
                {
                    p.Cos = 1;
                }
                else
                {
                    fwd1.Normalize();
                    p.Cos = Vector3.Dot(fwd, fwd1);
                }

                if (p.Cos >= .99999f)
                    p.Sin = 0;
                else
                {
                    p.Sin = (float)Math.Sqrt(1 - p.Cos * p.Cos);
                    if (fwd.X * fwd1.Z < fwd.Z * fwd1.X)
                        p.Sin = -p.Sin;
                }
            }
        }

    /*
        public void ComputePosition(Traveller traveler, bool backToFront, float elapsedTimeS, float distance, float speed)
        {
            for (var j = 0; j < Parts.Count; j++)
                Parts[j].InitLineFit();
            //var tileX = traveler.TileX;
            //var tileZ = traveler.TileZ;
            if (Flipped == backToFront)
            {
                var o = -CarLengthM / 2 - CentreOfGravityM.Z;
                for (var k = 0; k < WheelAxles.Count; k++)
                {
                    var d = WheelAxles[k].OffsetM - o;
                    o = WheelAxles[k].OffsetM;
                    traveler.Move(d);
                    var x = traveler.X;// + 2048 * (traveler.TileX - tileX);
                    var y = traveler.Y;
                    var z = traveler.Z;// + 2048 * (traveler.TileZ - tileZ);
                    WheelAxles[k].Part.AddWheelSetLocation(1, o, x, y, z, 0, traveler);
                }
                o = CarLengthM / 2 - CentreOfGravityM.Z - o;
                traveler.Move(o);
            }
            else
            {
                var o = CarLengthM / 2 - CentreOfGravityM.Z;
                for (var k = WheelAxles.Count - 1; k >= 0; k--)
                {
                    var d = o - WheelAxles[k].OffsetM;
                    o = WheelAxles[k].OffsetM;
                    traveler.Move(d);
                    var x = traveler.X;// + 2048 * (traveler.TileX - tileX);
                    var y = traveler.Y;
                    var z = traveler.Z;// + 2048 * (traveler.TileZ - tileZ);
                    WheelAxles[k].Part.AddWheelSetLocation(1, o, x, y, z, 0, traveler);
                }
                o = CarLengthM / 2 + CentreOfGravityM.Z + o;
                traveler.Move(o);
            }

            TrainCarPart p0 = Parts[0];
            for (int i = 1; i < Parts.Count; i++)
            {
                TrainCarPart p = Parts[i];
                p.FindCenterLine();
                if (p.SumWgt > 1.5)
                    p0.AddPartLocation(1, p);
            }
            p0.FindCenterLine();
            Vector3 fwd = new Vector3(p0.B[0], p0.B[1], -p0.B[2]);
            // Check if null vector - The Length() is fine also, but may be more time consuming - By GeorgeS
            if (fwd.X != 0 && fwd.Y != 0 && fwd.Z != 0)
                fwd.Normalize();
            Vector3 side = Vector3.Cross(Vector3.Up, fwd);
            // Check if null vector - The Length() is fine also, but may be more time consuming - By GeorgeS
            if (side.X != 0 && side.Y != 0 && side.Z != 0)
                side.Normalize();
            Vector3 up = Vector3.Cross(fwd, side);
            Matrix m = Matrix.Identity;
            m.M11 = side.X;
            m.M12 = side.Y;
            m.M13 = side.Z;
            m.M21 = up.X;
            m.M22 = up.Y;
            m.M23 = up.Z;
            m.M31 = fwd.X;
            m.M32 = fwd.Y;
            m.M33 = fwd.Z;
            m.M41 = p0.A[0];
            m.M42 = p0.A[1] + 0.275f;
            m.M43 = -p0.A[2];
            //WorldPosition.XNAMatrix = m;
            //WorldPosition.TileX = tileX;
            //WorldPosition.TileZ = tileZ;

            UpdatedTraveler(traveler, elapsedTimeS, distance, speed);

            // calculate truck angles
            for (int i = 1; i < Parts.Count; i++)
            {
                TrainCarPart p = Parts[i];
                if (p.SumWgt < .5)
                    continue;
                if (p.SumWgt < 1.5)
                {   // single axle pony trunk
                    float d = p.OffsetM - p.SumOffset / p.SumWgt;
                    if (-.2 < d && d < .2)
                        continue;
                    p.AddWheelSetLocation(1, p.OffsetM, p0.A[0] + p.OffsetM * p0.B[0], p0.A[1] + p.OffsetM * p0.B[1], p0.A[2] + p.OffsetM * p0.B[2], 0, null);
                    p.FindCenterLine();
                }
                Vector3 fwd1 = new Vector3(p.B[0], p.B[1], -p.B[2]);
                if (fwd1.X == 0 && fwd1.Y == 0 && fwd1.Z == 0)
                {
                    p.Cos = 1;
                }
                else
                {
                    fwd1.Normalize();
                    p.Cos = Vector3.Dot(fwd, fwd1);
                }

                if (p.Cos >= .99999f)
                    p.Sin = 0;
                else
                {
                    p.Sin = (float)Math.Sqrt(1 - p.Cos * p.Cos);
                    if (fwd.X * fwd1.Z < fwd.Z * fwd1.X)
                        p.Sin = -p.Sin;
                }
            }
        }
    */
        #region Traveller-based updates
        public float CurrentCurveRadius;

        //internal void UpdatedTraveler(Traveller traveler, float elapsedTimeS, float distanceM, float speedMpS)
        //{
        //    // We need to avoid introducing any unbounded effects, so cap the elapsed time to 0.25 seconds (4FPS).
        //    if (elapsedTimeS > 0.25f)
        //        return;

        //    CurrentCurveRadius = traveler.GetCurveRadius();
        //}
        #endregion
    }

    public class WheelAxle : IComparer<WheelAxle>
    {
        public float OffsetM;   // distance from center of model, positive forward
        public int BogieIndex;
        public int BogieMatrix;
        public TrainCarPart Part;
        public WheelAxle(float offset, int bogie, int parentMatrix)
        {
            OffsetM = offset;
            BogieIndex = bogie;
            BogieMatrix = parentMatrix;
        }
        public int Compare(WheelAxle a, WheelAxle b)
        {
            if (a.OffsetM > b.OffsetM) return 1;
            if (a.OffsetM < b.OffsetM) return -1;
            return 0;
        }
    }

    // data and methods used to align trucks and models to track
    public class TrainCarPart
    {
        public float OffsetM;   // distance from center of model, positive forward
        public int iMatrix;     // matrix in shape that needs to be moved
        public float Cos = 1;       // truck angle cosine
        public float Sin = 0;       // truck angle sin
        // line fitting variables
        public float SumWgt;
        public float SumOffset;
        public float SumOffsetSq;
        public float[] SumX = new float[4];
        public float[] SumXOffset = new float[4];
        public float[] A = new float[4];
        public float[] B = new float[4];
        public bool bogie;
        public TrainCarPart(float offset, int i)
        {
            OffsetM = offset;
            iMatrix = i;
        }
        public void InitLineFit()
        {
            SumWgt = SumOffset = SumOffsetSq = 0;
            for (int i = 0; i < 4; i++)
                SumX[i] = SumXOffset[i] = 0;
        }
        public void AddWheelSetLocation(float w, float o, float x, float y, float z, float t)//, Traveller traveler)
        {
            SumWgt += w;
            SumOffset += w * o;
            SumOffsetSq += w * o * o;
            SumX[0] += w * x;
            SumXOffset[0] += w * x * o;
            SumX[1] += w * y;
            SumXOffset[1] += w * y * o;
            SumX[2] += w * z;
            SumXOffset[2] += w * z * o;
            SumX[3] += w * t;
            SumXOffset[3] += w * t * o;
        }
        public void AddPartLocation(float w, TrainCarPart part)
        {
            SumWgt += w;
            SumOffset += w * part.OffsetM;
            SumOffsetSq += w * part.OffsetM * part.OffsetM;
            for (int i = 0; i < 4; i++)
            {
                float x = part.A[i] + part.OffsetM * part.B[i];
                SumX[i] += w * x;
                SumXOffset[i] += w * x * part.OffsetM;
            }
        }
        public void FindCenterLine()
        {
            float d = SumWgt * SumOffsetSq - SumOffset * SumOffset;
            if (d > 1e-20)
            {
                for (int i = 0; i < 4; i++)
                {
                    A[i] = (SumOffsetSq * SumX[i] - SumOffset * SumXOffset[i]) / d;
                    B[i] = (SumWgt * SumXOffset[i] - SumOffset * SumX[i]) / d;
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    A[i] = SumX[i] / SumWgt;
                    B[i] = 0;
                }
            }
        }
    }
}
