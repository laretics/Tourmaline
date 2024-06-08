// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

/*
 *    TrainCarSimulator
 *    
 *    TrainCarViewer
 *    
 *  Every TrainCar generates a FrictionForce.
 *  
 *  The viewer is a separate class object since there could be multiple 
 *  viewers potentially on different devices for a single car. 
 *  
 */

//#define ALLOW_ORTS_SPECIFIC_ENG_PARAMETERS
//#define DEBUG_AUXTENDER

// Debug for Friction Force
//#define DEBUG_FRICTION

// Debug for Freight Animation Variable Mass
//#define DEBUG_VARIABLE_MASS

using Microsoft.Xna.Framework;
using Tourmaline.Formats.Msts;
using Tourmaline.Parsers.Msts;
using TOURMALINE.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Event = Tourmaline.Common.Event;

namespace Tourmaline.Simulation.RollingStocks
{

    ///////////////////////////////////////////////////
    ///   SIMULATION BEHAVIOUR
    ///////////////////////////////////////////////////


    /// <summary>
    /// Represents the physical motion and behaviour of the car.
    /// </summary>

    public class MSTSWagon : TrainCar
    {
        public bool DoorLeftOpen;
        public bool DoorRightOpen;
        public bool MirrorOpen;
        public bool UnloadingPartsOpen;
        public bool WaitForAnimationReady; // delay counter to start loading/unliading is on;
        public bool IsRollerBearing = false; // Has roller bearings
        public bool IsLowTorqueRollerBearing = false; // Has low torque roller bearings
        public bool IsFrictionBearing = false; //Has oil based friction (or solid bearings)
        public bool IsGreaseFrictionBearing = false; // Has grease based friction (or solid bearings)
        public bool IsStandStill = true;  // Used for MSTS type friction
        public bool IsDavisFriction = true; // Default to new Davis type friction
        public bool IsBelowMergeSpeed = true; // set indicator for low speed operation as per given speed

        Interpolator BrakeShoeFrictionFactor;  // Factor of friction for wagon brake shoes
        const float WaterLBpUKG = 10.0f;    // lbs of water in 1 gal (uk)
        float TempMassDiffRatio;

        // simulation parameters
        public float Variable1;  // used to convey status to soundsource
        public float Variable2;
        public float Variable3;

        internal float CarAirHoseLengthM;
        internal float CarAirHoseHorizontalLengthM;
        internal float TrainHeatBoilerWaterUsageGalukpH;
        internal float TrainHeatBoilerFuelUsageGalukpH;
        internal float MaximumSteamHeatingBoilerSteamUsageRateLbpS;
        internal float CurrentSteamHeatBoilerFuelCapacityL;
        internal float MaximumSteamHeatBoilerWaterTankCapacityL;
        internal float MaximiumSteamHeatBoilerFuelTankCapacityL;
        internal float CurrentCarSteamHeatBoilerWaterCapacityL;
        internal float MassKG;
        internal float InitialMassKG;
        internal float InitialMaxHandbrakeForceN;
        internal float InitialMaxBrakeForceN;
        internal float MaxHandbrakeForceN;
        internal float MaxBrakeForceN;
        internal float TrackGaugeM;
        internal float UnbalancedSuperElevationM;
        internal float RigidWheelBaseM;
        internal float AuxTenderWaterMassKG;
        internal float WindowDeratingFactor;
        internal float DesiredCompartmentTempSetpointC;
        internal float CompartmentHeatingPipeAreaFactor;
        internal float MainSteamHeatPipeOuterDiaM;
        internal float MainSteamHeatPipeInnerDiaM;
        internal float CarConnectSteamHoseInnerDiaM;
        internal float CarConnectSteamHoseOuterDiaM;
        internal float DriverWheelRadiusM;
        internal float FrontCouplerAnimWidthM;
        internal float FrontCouplerAnimHeightM;
        internal float FrontCouplerAnimLengthM;
        internal float FrontAirHoseAnimWidthM;
        internal float FrontAirHoseAnimHeightM;
        internal float FrontAirHoseAnimLengthM;
        internal float RearCouplerAnimWidthM;
        internal float RearCouplerAnimHeightM;
        internal float RearCouplerAnimLengthM;
        internal float RearAirHoseAnimWidthM;
        internal float RearAirHoseAnimHeightM;
        internal float RearAirHoseAnimLengthM;
        internal float FrontCouplerOpenAnimWidthM;
        internal float FrontCouplerOpenAnimHeightM;
        internal float FrontCouplerOpenAnimLengthM;
        internal float RearCouplerOpenAnimWidthM;
        internal float RearCouplerOpenAnimHeightM;
        internal float RearCouplerOpenAnimLengthM;
        internal float FrontAirHoseDisconnectedAnimWidthM;
        internal float FrontAirHoseDisconnectedAnimHeightM;
        internal float FrontAirHoseDisconnectedAnimLengthM;
        internal float RearAirHoseDisconnectedAnimWidthM;
        internal float RearAirHoseDisconnectedAnimHeightM;
        internal float RearAirHoseDisconnectedAnimLengthM;

        internal bool WheelBrakeSlideProtectionFitted;
        internal bool WheelBrakeSlideProtectionLimitDisabled;
        internal bool FrontCouplerOpenFitted;
        internal bool RearCouplerOpenFitted;
        internal bool IsAdvancedCoupler;

        internal string CarBrakeSystemType;
        internal string FrontCouplerOpenShapeFileName;
        internal string RearCouplerOpenShapeFileName;
        internal string FrontAirHoseDisconnectedShapeFileName;
        internal string RearAirHoseDisconnectedShapeFileName;

        // wag file data
        public string MainShapeFileName;
        public string FreightShapeFileName;
        public float FreightAnimMaxLevelM;
        public float FreightAnimMinLevelM;
        public float FreightAnimFlag = 1;   // if absent or >= 0 causes the freightanim to drop in tenders
        public string Cab3DShapeFileName; // 3DCab view shape file name
        public string InteriorShapeFileName; // passenger view shape file name
        public string MainSoundFileName;
        public string InteriorSoundFileName;
        public string Cab3DSoundFileName;
        public float ExternalSoundPassThruPercent = -1;
        public float WheelRadiusM = 18.0f;  // Provide some defaults in case it's missing from the wag - Wagon wheels could vary in size from approx 10" to 25".
        protected float StaticFrictionFactorN;    // factor to multiply friction by to determine static or starting friction - will vary depending upon whether roller or friction bearing
        float FrictionLowSpeedN; // Davis low speed value 0 - 5 mph
        float FrictionBelowMergeSpeedN; // Davis low speed value for defined speed
        public float Friction0N;        // static friction
        protected float Friction5N;               // Friction at 5mph
        public float StandstillFrictionN;
        public float MergeSpeedFrictionN;
        public float MergeSpeedMpS = 5f;
        public float DavisAN;           // davis equation constant
        public float DavisBNSpM;        // davis equation constant for speed
        public float DavisCNSSpMM;      // davis equation constant for speed squared
        public float DavisDragConstant; // Drag coefficient for wagon
        public float WagonFrontalAreaM2; // Frontal area of wagon
        public float TrailLocoResistanceFactor; // Factor to reduce base and wind resistance if locomotive is not leading - based upon original Davis drag coefficients

        bool TenderWeightInitialize = true;
        float TenderWagonMaxCoalMassKG;
        float TenderWagonMaxWaterMassKG;

        // Wind Impacts
        float WagonDirectionDeg;
        float WagonResultantWindComponentDeg;
        float WagonWindResultantSpeedMpS;

        protected float FrictionC1; // MSTS Friction parameters
        protected float FrictionE1; // MSTS Friction parameters
        protected float FrictionV2; // MSTS Friction parameters
        protected float FrictionC2; // MSTS Friction parameters
        protected float FrictionE2; // MSTS Friction parameters

        //protected float FrictionSpeedMpS; // Train current speed value for friction calculations ; this value is never used outside of this class, and FrictionSpeedMpS is always = AbsSpeedMpS
        public List<MSTSCoupling> Couplers = new List<MSTSCoupling>();
        public float Adhesion1 = .27f;   // 1st MSTS adhesion value
        public float Adhesion2 = .49f;   // 2nd MSTS adhesion value
        public float Adhesion3 = 2;   // 3rd MSTS adhesion value
        public float Curtius_KnifflerA = 7.5f;               //Curtius-Kniffler constants                   A
        public float Curtius_KnifflerB = 44.0f;              // (adhesion coeficient)       umax = ---------------------  + C
        public float Curtius_KnifflerC = 0.161f;             //                                      speedMpS * 3.6 + B
        public float AdhesionK = 0.7f;   //slip characteristics slope
        //public AntislipControl AntislipControl = AntislipControl.None;
        public float AxleInertiaKgm2;    //axle inertia
        public float AdhesionDriveWheelRadiusM;
        public float WheelSpeedMpS;
        public float WheelSpeedSlipMpS; // speed of wheel if locomotive is slipping
        public float SlipWarningThresholdPercent = 70;
        public float AbsWheelSpeedMpS; // Math.Abs(WheelSpeedMpS) is used frequently in the subclasses, maybe it's more efficient to compute it once

        // Colours for smoke and steam effects
        public Color ExhaustTransientColor = Color.Black;
        public Color ExhaustDecelColor = Color.WhiteSmoke;
        public Color ExhaustSteadyColor = Color.Gray;

        // Wagon steam leaks
        public float HeatingHoseParticleDurationS;
        public float HeatingHoseSteamVelocityMpS;
        public float HeatingHoseSteamVolumeM3pS;

        // Wagon heating compartment steamtrap leaks
        public float HeatingCompartmentSteamTrapParticleDurationS;
        public float HeatingCompartmentSteamTrapVelocityMpS;
        public float HeatingCompartmentSteamTrapVolumeM3pS;

        // Wagon heating steamtrap leaks
        public float HeatingMainPipeSteamTrapDurationS;
        public float HeatingMainPipeSteamTrapVelocityMpS;
        public float HeatingMainPipeSteamTrapVolumeM3pS;

        // Steam Brake leaks
        public float SteamBrakeLeaksDurationS;
        public float SteamBrakeLeaksVelocityMpS;
        public float SteamBrakeLeaksVolumeM3pS;

        // Water Scoop Spray
        public float WaterScoopParticleDurationS;
        public float WaterScoopWaterVelocityMpS;
        public float WaterScoopWaterVolumeM3pS;

        // Tender Water overflow
        public float TenderWaterOverflowParticleDurationS;
        public float TenderWaterOverflowVelocityMpS;
        public float TenderWaterOverflowVolumeM3pS;

        // Wagon Power Generator
        public float WagonGeneratorDurationS = 1.5f;
        public float WagonGeneratorVolumeM3pS = 2.0f;
        public Color WagonGeneratorSteadyColor = Color.Gray;

        // Heating Steam Boiler
        public float HeatingSteamBoilerDurationS;
        public float HeatingSteamBoilerVolumeM3pS;
        public Color HeatingSteamBoilerSteadyColor = Color.LightSlateGray;
        public bool HeatingBoilerSet = false;

        // Wagon Smoke
        public float WagonSmokeVolumeM3pS;
        float InitialWagonSmokeVolumeM3pS = 3.0f;
        public float WagonSmokeDurationS;
        float InitialWagonSmokeDurationS = 1.0f;
        public float WagonSmokeVelocityMpS = 15.0f;
        public Color WagonSmokeSteadyColor = Color.Gray;

        float TrueCouplerCount = 0;
        int CouplerCountLocation;

        // Bearing Hot Box Smoke
        public float BearingHotBoxSmokeVolumeM3pS;
        public float BearingHotBoxSmokeDurationS;
        public float BearingHotBoxSmokeVelocityMpS = 15.0f;
        public Color BearingHotBoxSmokeSteadyColor = Color.Gray;

        /// <summary>
        /// True if vehicle is equipped with an additional emergency brake reservoir
        /// </summary>
        public bool EmergencyReservoirPresent;
        /// <summary>
        /// True if triple valve is capable of releasing brake gradually
        /// </summary>
        public bool DistributorPresent;
        /// <summary>
        /// True if equipped with handbrake. (Not common for older steam locomotives.)
        /// </summary>
        public bool HandBrakePresent;
        /// <summary>
        /// Number of available retainer positions. (Used on freight cars, mostly.) Might be 0, 3 or 4.
        /// </summary>
        public int RetainerPositions;

        /// <summary>
        /// Indicates whether a brake is present or not when Manual Braking is selected.
        /// </summary>
        public bool ManualBrakePresent;

        /// <summary>
        /// Indicates whether a non auto (straight) brake is present or not when braking is selected.
        /// </summary>
        public bool NonAutoBrakePresent;

        /// <summary>
        /// Indicates whether an auxiliary reservoir is present on the wagon or not.
        /// </summary>
        public bool AuxiliaryReservoirPresent;

        public Dictionary<string, List<ParticleEmitterData>> EffectData = new Dictionary<string, List<ParticleEmitterData>>();

        protected void ParseEffects(string lowercasetoken, STFReader stf)
        {
            stf.MustMatch("(");
            string s;

            while ((s = stf.ReadItem()) != ")")
            {
                var data = new ParticleEmitterData(stf);
                if (!EffectData.ContainsKey(s))
                    EffectData.Add(s, new List<ParticleEmitterData>());
                EffectData[s].Add(data);
            }

        }


        public List<IntakePoint> IntakePointList = new List<IntakePoint>();

        /// <summary>
        /// Supply types for freight wagons and locos
        /// </summary>
        public enum PickupType
        {
            None = 0,
            FreightGrain = 1,
            FreightCoal = 2,
            FreightGravel = 3,
            FreightSand = 4,
            FuelWater = 5,
            FuelCoal = 6,
            FuelDiesel = 7,
            FuelWood = 8,    // Think this is new to OR and not recognised by MSTS
            FuelSand = 9,  // New to OR
            FreightGeneral = 10, // New to OR
            FreightLivestock = 11,  // New to OR
            FreightFuel = 12,  // New to OR
            FreightMilk = 13,   // New to OR
            SpecialMail = 14  // New to OR
        }

        public class RefillProcess
        {
            public static bool OkToRefill { get; set; }
            public static int ActivePickupObjectUID { get; set; }
            public static bool Unload { get; set; }
        }

        public MSTSWagon(MicroSim simulator, string wagFilePath)
            : base(simulator, wagFilePath)
        {

        }

        public void Load()
        {
            if (CarManager.LoadedCars.ContainsKey(WagFilePath))
            {
                Copy(CarManager.LoadedCars[WagFilePath]);
            }
            else
            {
                LoadFromWagFile(WagFilePath);
                CarManager.LoadedCars.Add(WagFilePath, this);
            }

        }

        // Values for adjusting wagon physics due to load changes
        float LoadEmptyMassKg;
        float LoadEmptyORTSDavis_A;
        float LoadEmptyORTSDavis_B;
        float LoadEmptyORTSDavis_C;
        float LoadEmptyWagonFrontalAreaM2;
        float LoadEmptyDavisDragConstant;
        float LoadEmptyMaxBrakeForceN;
        float LoadEmptyMaxHandbrakeForceN;
        float LoadEmptyCentreOfGravityM_Y;

        float LoadFullMassKg;
        float LoadFullORTSDavis_A;
        float LoadFullORTSDavis_B;
        float LoadFullORTSDavis_C;
        float LoadFullWagonFrontalAreaM2;
        float LoadFullDavisDragConstant;
        float LoadFullMaxBrakeForceN;
        float LoadFullMaxHandbrakeForceN;
        float LoadFullCentreOfGravityM_Y;
        internal string FrontCouplerShapeFileName;
        internal string RearCouplerShapeFileName;
        internal string FrontAirHoseShapeFileName;
        internal string RearAirHoseShapeFileName;

        /// <summary>
        /// This initializer is called when we haven't loaded this type of car before
        /// and must read it new from the wag file.
        /// </summary>
        public virtual void LoadFromWagFile(string wagFilePath)
        {
            string dir = Path.GetDirectoryName(wagFilePath);
            string file = Path.GetFileName(wagFilePath);
            string orFile = dir + @"\openrails\" + file;
            if (File.Exists(orFile))
                wagFilePath = orFile;

            using (STFReader stf = new STFReader(wagFilePath, true))
            {
                while (!stf.Eof)
                {
                    stf.ReadItem();
                    Parse(stf.Tree.ToLower(), stf);
                }
            }

            var wagonFolderSlash = Path.GetDirectoryName(WagFilePath) + @"\";
            if (MainShapeFileName != null && !File.Exists(wagonFolderSlash + MainShapeFileName))
            {
                Trace.TraceWarning("{0} references non-existent shape {1}", WagFilePath, wagonFolderSlash + MainShapeFileName);
                MainShapeFileName = string.Empty;
            }
            if (FreightShapeFileName != null && !File.Exists(wagonFolderSlash + FreightShapeFileName))
            {
                Trace.TraceWarning("{0} references non-existent shape {1}", WagFilePath, wagonFolderSlash + FreightShapeFileName);
                FreightShapeFileName = null;
            }
            if (InteriorShapeFileName != null && !File.Exists(wagonFolderSlash + InteriorShapeFileName))
            {
                Trace.TraceWarning("{0} references non-existent shape {1}", WagFilePath, wagonFolderSlash + InteriorShapeFileName);
                InteriorShapeFileName = null;
            }

            

            if (FrontCouplerShapeFileName != null && !File.Exists(wagonFolderSlash + FrontCouplerShapeFileName))
            {
                Trace.TraceWarning("{0} references non-existent shape {1}", WagFilePath, wagonFolderSlash + FrontCouplerShapeFileName);
                FrontCouplerShapeFileName = null;
            }

            if (RearCouplerShapeFileName != null && !File.Exists(wagonFolderSlash + RearCouplerShapeFileName))
            {
                Trace.TraceWarning("{0} references non-existent shape {1}", WagFilePath, wagonFolderSlash + RearCouplerShapeFileName);
                RearCouplerShapeFileName = null;
            }

            if (FrontAirHoseShapeFileName != null && !File.Exists(wagonFolderSlash + FrontAirHoseShapeFileName))
            {
                Trace.TraceWarning("{0} references non-existent shape {1}", WagFilePath, wagonFolderSlash + FrontAirHoseShapeFileName);
                FrontAirHoseShapeFileName = null;
            }

            if (RearAirHoseShapeFileName != null && !File.Exists(wagonFolderSlash + RearAirHoseShapeFileName))
            {
                Trace.TraceWarning("{0} references non-existent shape {1}", WagFilePath, wagonFolderSlash + RearAirHoseShapeFileName);
                RearAirHoseShapeFileName = null;
            }

            // If trailing loco resistance constant has not been  defined in WAG/ENG file then assign default value based upon orig Davis values
            if (TrailLocoResistanceFactor == 0)
            {
                if (WagonType == WagonTypes.Engine)
                {
                    TrailLocoResistanceFactor = 0.2083f;  // engine drag value
                }
                else if (WagonType == WagonTypes.Tender)
                {
                    TrailLocoResistanceFactor = 1.0f;  // assume that tenders have been set with a value of 0.0005 as per freight wagons
                }
                else  //Standard default if not declared anywhere else
                {
                    TrailLocoResistanceFactor = 1.0f;
                }
            }

            // Initialise car body lengths. Assume overhang is 2.0m each end, and bogie centres are the car length minus this value

            if (CarCouplerFaceLengthM == 0)
            {
                CarCouplerFaceLengthM = CarLengthM;
            }

            if (CarBodyLengthM == 0)
            {
                CarBodyLengthM = CarCouplerFaceLengthM - 0.8f;
            }

            if (CarBogieCentreLengthM == 0)
            {
                CarBogieCentreLengthM = (CarCouplerFaceLengthM - 4.3f);
            }

            if (CarAirHoseLengthM == 0)
            {
                CarAirHoseLengthM = 26.25f; // 26.25 inches
            }

            var couplerlength = ((CarCouplerFaceLengthM - CarBodyLengthM) / 2) + 0.1f; // coupler length at rest, allow 0.1m also for slack

            if (CarAirHoseHorizontalLengthM == 0)
            {
                CarAirHoseHorizontalLengthM = 0.3862f; // 15.2 inches
            }

            // Ensure Drive Axles is set to a default if no OR value added to WAG file
            if (WagonNumAxles == 0 && WagonType != WagonTypes.Engine)
            {
                if (MSTSWagonNumWheels != 0 && MSTSWagonNumWheels < 6)
                {
                    WagonNumAxles = (int)MSTSWagonNumWheels;
                }
                else
                {
                    WagonNumAxles = 4; // Set 4 axles as default
                }
                Trace.TraceInformation("Number of Wagon Axles set to default value of {0}", WagonNumAxles);
            }

            CurrentSteamHeatBoilerFuelCapacityL = MaximiumSteamHeatBoilerFuelTankCapacityL;

            if (MaximumSteamHeatBoilerWaterTankCapacityL != 0)
            {
                CurrentCarSteamHeatBoilerWaterCapacityL = MaximumSteamHeatBoilerWaterTankCapacityL;
            }
            else
            {
                CurrentCarSteamHeatBoilerWaterCapacityL = 800.0f;
            }

            // If Drag constant not defined in WAG/ENG file then assign default value based upon orig Davis values
            if (DavisDragConstant == 0)
            {
                if (WagonType == WagonTypes.Engine)
                {
                    DavisDragConstant = 0.0024f;
                }
                else if (WagonType == WagonTypes.Freight)
                {
                    DavisDragConstant = 0.0005f;
                }
                else if (WagonType == WagonTypes.Passenger)
                {
                    DavisDragConstant = 0.00034f;
                }
                else if (WagonType == WagonTypes.Tender)
                {
                    DavisDragConstant = 0.0005f;
                }
                else  //Standard default if not declared anywhere else
                {
                    DavisDragConstant = 0.0005f;
                }
            }

            // If wagon frontal area not user defined, assign a default value based upon the wagon dimensions

            if (WagonFrontalAreaM2 == 0)
            {
                WagonFrontalAreaM2 = CarWidthM * CarHeightM;
            }

            // Initialise key wagon parameters
            MassKG = InitialMassKG;
            MaxHandbrakeForceN = InitialMaxHandbrakeForceN;
            MaxBrakeForceN = InitialMaxBrakeForceN;
            CentreOfGravityM = InitialCentreOfGravityM;

            // Determine whether or not to use the Davis friction model. Must come after freight animations are initialized.
            IsDavisFriction = DavisAN != 0 && DavisBNSpM != 0 && DavisCNSSpMM != 0;
        }


        public override void Initialize()
        {
            base.Initialize();
        }

        public override void InitializeMoving()
        {
            base.InitializeMoving();
        }

        /// <summary>
        /// Parse the wag file parameters required for the simulator and viewer classes
        /// </summary>
        public virtual void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "wagon(wagonshape": MainShapeFileName = stf.ReadStringBlock(null); break;
                case "wagon(type":
                    stf.MustMatch("(");
                    var wagonType = stf.ReadString();
                    try
                    {
                        WagonType = (WagonTypes)Enum.Parse(typeof(WagonTypes), wagonType.Replace("Carriage", "Passenger"));
                    }
                    catch
                    {
                        STFException.TraceWarning(stf, "Skipped unknown wagon type " + wagonType);
                    }
                    break;
                case "wagon(ortswagonspecialtype":
                    stf.MustMatch("(");
                    var wagonspecialType = stf.ReadString();
                    try
                    {
                        //WagonSpecialType = (WagonSpecialTypes)Enum.Parse(typeof(WagonSpecialTypes), wagonspecialType);
                    }
                    catch
                    {
                        STFException.TraceWarning(stf, "Assumed unknown engine type " + wagonspecialType);
                    }
                    break;
                case "wagon(freightanim":
                    stf.MustMatch("(");
                    FreightShapeFileName = stf.ReadString();
                    FreightAnimMaxLevelM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    FreightAnimMinLevelM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    // Flags are optional and we want to avoid a warning.
                    if (!stf.EndOfBlock())
                    {
                        // TODO: The variable name (Flag), data type (Float), and unit (Distance) don't make sense here.
                        FreightAnimFlag = stf.ReadFloat(STFReader.UNITS.Distance, 1.0f);
                        stf.SkipRestOfBlock();
                    }
                    break;
                case "wagon(size":
                    stf.MustMatch("(");
                    CarWidthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    CarHeightM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    CarLengthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortslengthbogiecentre": CarBogieCentreLengthM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortslengthcarbody": CarBodyLengthM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortslengthairhose": CarAirHoseLengthM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortshorizontallengthairhose": CarAirHoseHorizontalLengthM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortslengthcouplerface": CarCouplerFaceLengthM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortstrackgauge":
                    stf.MustMatch("(");
                    TrackGaugeM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    // Allow for imperial feet and inches to be specified separately (not ideal - please don't copy this).
                    if (!stf.EndOfBlock())
                    {
                        TrackGaugeM += stf.ReadFloat(STFReader.UNITS.Distance, 0);
                        stf.SkipRestOfBlock();
                    }
                    break;
                case "wagon(centreofgravity":
                    stf.MustMatch("(");
                    InitialCentreOfGravityM.X = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    InitialCentreOfGravityM.Y = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    InitialCentreOfGravityM.Z = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    if (Math.Abs(InitialCentreOfGravityM.Z) > 1)
                    {
                        STFException.TraceWarning(stf, string.Format("Ignored CentreOfGravity Z value {0} outside range -1 to +1", InitialCentreOfGravityM.Z));
                        InitialCentreOfGravityM.Z = 0;
                    }
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsunbalancedsuperelevation": UnbalancedSuperElevationM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortsrigidwheelbase":
                    stf.MustMatch("(");
                    RigidWheelBaseM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    // Allow for imperial feet and inches to be specified separately (not ideal - please don't copy this).
                    if (!stf.EndOfBlock())
                    {
                        RigidWheelBaseM += stf.ReadFloat(STFReader.UNITS.Distance, 0);
                        stf.SkipRestOfBlock();
                    }
                    break;
                case "wagon(ortsauxtenderwatermass": AuxTenderWaterMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "wagon(ortstenderwagoncoalmass": TenderWagonMaxCoalMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "wagon(ortstenderwagonwatermass": TenderWagonMaxWaterMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); break;
                case "wagon(ortsheatingwindowderatingfactor": WindowDeratingFactor = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortsheatingcompartmenttemperatureset": DesiredCompartmentTempSetpointC = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortsheatingcompartmentpipeareafactor": CompartmentHeatingPipeAreaFactor = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortsheatingtrainpipeouterdiameter": MainSteamHeatPipeOuterDiaM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortsheatingtrainpipeinnerdiameter": MainSteamHeatPipeInnerDiaM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortsheatingconnectinghoseinnerdiameter": CarConnectSteamHoseInnerDiaM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(ortsheatingconnectinghoseouterdiameter": CarConnectSteamHoseOuterDiaM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(mass": InitialMassKG = stf.ReadFloatBlock(STFReader.UNITS.Mass, null); if (InitialMassKG < 0.1f) InitialMassKG = 0.1f; break;
                case "wagon(ortsheatingboilerwatertankcapacity": MaximumSteamHeatBoilerWaterTankCapacityL = stf.ReadFloatBlock(STFReader.UNITS.Volume, null); break;
                case "wagon(ortsheatingboilerfueltankcapacity": MaximiumSteamHeatBoilerFuelTankCapacityL = stf.ReadFloatBlock(STFReader.UNITS.Volume, null); break;
                case "wagon(ortsheatingboilerwaterusage":  break;
                case "wagon(ortsheatingboilerfuelusage":  break;
                case "wagon(wheelradius": WheelRadiusM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "engine(wheelradius": DriverWheelRadiusM = stf.ReadFloatBlock(STFReader.UNITS.Distance, null); break;
                case "wagon(sound": MainSoundFileName = stf.ReadStringBlock(null); break;
                case "wagon(ortsbrakeshoefriction": BrakeShoeFrictionFactor = new Interpolator(stf); break;
                case "wagon(maxhandbrakeforce": InitialMaxHandbrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(maxbrakeforce": InitialMaxBrakeForceN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(ortswheelbrakeslideprotection":
                    // stf.MustMatch("(");
                    var brakeslideprotection = stf.ReadFloatBlock(STFReader.UNITS.None, null);
                    if (brakeslideprotection == 1)
                    {
                        WheelBrakeSlideProtectionFitted = true;
                    }
                    else
                    {
                        WheelBrakeSlideProtectionFitted = false;
                    }
                    break;
                case "wagon(ortswheelbrakesslideprotectionlimitdisable":
                    // stf.MustMatch("(");
                    var brakeslideprotectiondisable = stf.ReadFloatBlock(STFReader.UNITS.None, null);
                    if (brakeslideprotectiondisable == 1)
                    {
                        WheelBrakeSlideProtectionLimitDisabled = true;
                    }
                    else
                    {
                        WheelBrakeSlideProtectionLimitDisabled = false;
                    }
                    break;
                case "wagon(ortsdavis_a": DavisAN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(ortsdavis_b": DavisBNSpM = stf.ReadFloatBlock(STFReader.UNITS.Resistance, null); break;
                case "wagon(ortsdavis_c": DavisCNSSpMM = stf.ReadFloatBlock(STFReader.UNITS.ResistanceDavisC, null); break;
                case "wagon(ortsdavisdragconstant": DavisDragConstant = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortswagonfrontalarea": WagonFrontalAreaM2 = stf.ReadFloatBlock(STFReader.UNITS.AreaDefaultFT2, null); break;
                case "wagon(ortstraillocomotiveresistancefactor": TrailLocoResistanceFactor = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "wagon(ortsstandstillfriction": StandstillFrictionN = stf.ReadFloatBlock(STFReader.UNITS.Force, null); break;
                case "wagon(ortsmergespeed": MergeSpeedMpS = stf.ReadFloatBlock(STFReader.UNITS.Speed, MergeSpeedMpS); break;
                case "wagon(effects(specialeffects": ParseEffects(lowercasetoken, stf); break;
                case "wagon(ortsbearingtype":
                    stf.MustMatch("(");
                    string typeString2 = stf.ReadString();
                    IsRollerBearing = String.Compare(typeString2, "Roller") == 0;
                    IsLowTorqueRollerBearing = String.Compare(typeString2, "Low") == 0;
                    IsFrictionBearing = String.Compare(typeString2, "Friction") == 0;
                    IsGreaseFrictionBearing = String.Compare(typeString2, "Grease") == 0;
                    break;
                case "wagon(friction":
                    stf.MustMatch("(");
                    FrictionC1 = stf.ReadFloat(STFReader.UNITS.Resistance, null);
                    FrictionE1 = stf.ReadFloat(STFReader.UNITS.None, null);
                    FrictionV2 = stf.ReadFloat(STFReader.UNITS.Speed, null);
                    FrictionC2 = stf.ReadFloat(STFReader.UNITS.Resistance, null);
                    FrictionE2 = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                    ; break;
                case "wagon(brakesystemtype":
                    CarBrakeSystemType = stf.ReadStringBlock(null).ToLower();
                    //BrakeSystem = MSTSBrakeSystem.Create(CarBrakeSystemType, this);
                    break;
                case "wagon(brakeequipmenttype":
                    foreach (var equipment in stf.ReadStringBlock("").ToLower().Replace(" ", "").Split(','))
                    {
                        switch (equipment)
                        {
                            case "distributor":
                            case "graduated_release_triple_valve": DistributorPresent = true; break;
                            case "emergency_brake_reservoir": EmergencyReservoirPresent = true; break;
                            case "handbrake": HandBrakePresent = true; break;
                            case "auxiliary_reservoir": AuxiliaryReservoirPresent = true; break;
                            case "manual_brake": ManualBrakePresent = true; break;
                            case "retainer_3_position": RetainerPositions = 3; break;
                            case "retainer_4_position": RetainerPositions = 4; break;
                        }
                    }
                    break;
                case "wagon(coupling":
                    Couplers.Add(new MSTSCoupling()); // Adds a new coupler every time "Coupler" parameters found in WAG and INC file
                    CouplerCountLocation = 0;
                    TrueCouplerCount += 1;
                    // it is possible for there to be more then two couplers per car if the coupler details are added via an INC file. Therefore the couplers need to be adjusted appropriately.
                    // Front coupler stored in slot 0, and rear coupler stored in slot 1
                    if (Couplers.Count > 2 && TrueCouplerCount == 3)  // If front coupler has been added via INC file
                    {
                        Couplers.RemoveAt(0);  // Remove old front coupler
                        CouplerCountLocation = 0;  // Write info to old front coupler location. 
                    }
                    else if (Couplers.Count > 2 && TrueCouplerCount == 4)  // If rear coupler has been added via INC file
                    {
                        Couplers.RemoveAt(1);  // Remove old rear coupler
                        CouplerCountLocation = 1;  // Write info to old rear coupler location. 
                    }
                    else
                    {
                        CouplerCountLocation = Couplers.Count - 1;  // By default write info into 0 and 1 slots as required.
                    };
                    break;

                // Used for simple or legacy coupler
                case "wagon(coupling(spring(break":
                    stf.MustMatch("(");
                    Couplers[CouplerCountLocation].SetSimpleBreak(stf.ReadFloat(STFReader.UNITS.Force, null), stf.ReadFloat(STFReader.UNITS.Force, null));
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(coupling(spring(r0":
                    stf.MustMatch("(");
                    Couplers[CouplerCountLocation].SetSimpleR0(stf.ReadFloat(STFReader.UNITS.Distance, null), stf.ReadFloat(STFReader.UNITS.Distance, null));
                    stf.SkipRestOfBlock();
                    break;

                case "wagon(coupling(spring(stiffness":
                    stf.MustMatch("(");
                    Couplers[CouplerCountLocation].SetSimpleStiffness(stf.ReadFloat(STFReader.UNITS.Stiffness, null), stf.ReadFloat(STFReader.UNITS.Stiffness, null));
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(coupling(spring(ortsslack":
                    stf.MustMatch("(");
                    // IsAdvancedCoupler = true; // If this parameter is present in WAG file then treat coupler as advanced ones.  Temporarily disabled for v1.3 release
                    Couplers[CouplerCountLocation].SetSlack(stf.ReadFloat(STFReader.UNITS.Distance, null), stf.ReadFloat(STFReader.UNITS.Distance, null));
                    stf.SkipRestOfBlock();
                    break;

                // Used for advanced coupler

                case "wagon(coupling(frontcoupleranim":
                    stf.MustMatch("(");
                    FrontCouplerShapeFileName = stf.ReadString();
                    FrontCouplerAnimWidthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    FrontCouplerAnimHeightM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    FrontCouplerAnimLengthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;

                case "wagon(coupling(frontairhoseanim":
                    stf.MustMatch("(");
                    FrontAirHoseShapeFileName = stf.ReadString();
                    FrontAirHoseAnimWidthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    FrontAirHoseAnimHeightM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    FrontAirHoseAnimLengthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;

                case "wagon(coupling(rearcoupleranim":
                    stf.MustMatch("(");
                    RearCouplerShapeFileName = stf.ReadString();
                    RearCouplerAnimWidthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    RearCouplerAnimHeightM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    RearCouplerAnimLengthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;

                case "wagon(coupling(rearairhoseanim":
                    stf.MustMatch("(");
                    RearAirHoseShapeFileName = stf.ReadString();
                    RearAirHoseAnimWidthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    RearAirHoseAnimHeightM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    RearAirHoseAnimLengthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;

                case "wagon(coupling(spring(ortstensionstiffness":
                    stf.MustMatch("(");
                    Couplers[CouplerCountLocation].SetTensionStiffness(stf.ReadFloat(STFReader.UNITS.Force, null), stf.ReadFloat(STFReader.UNITS.Force, null));
                    stf.SkipRestOfBlock();
                    break;

                case "wagon(coupling(frontcoupleropenanim":
                    stf.MustMatch("(");
                    FrontCouplerOpenFitted = true;
                    FrontCouplerOpenShapeFileName = stf.ReadString();
                    FrontCouplerOpenAnimWidthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    FrontCouplerOpenAnimHeightM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    FrontCouplerOpenAnimLengthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;

                case "wagon(coupling(rearcoupleropenanim":
                    stf.MustMatch("(");
                    RearCouplerOpenFitted = true;
                    RearCouplerOpenShapeFileName = stf.ReadString();
                    RearCouplerOpenAnimWidthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    RearCouplerOpenAnimHeightM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    RearCouplerOpenAnimLengthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;

                case "wagon(coupling(frontairhosediconnectedanim":
                    stf.MustMatch("(");
                    FrontAirHoseDisconnectedShapeFileName = stf.ReadString();
                    FrontAirHoseDisconnectedAnimWidthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    FrontAirHoseDisconnectedAnimHeightM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    FrontAirHoseDisconnectedAnimLengthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;

                case "wagon(coupling(rearairhosediconnectedanim":
                    stf.MustMatch("(");
                    RearAirHoseDisconnectedShapeFileName = stf.ReadString();
                    RearAirHoseDisconnectedAnimWidthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    RearAirHoseDisconnectedAnimHeightM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    RearAirHoseDisconnectedAnimLengthM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;


                case "wagon(coupling(spring(ortscompressionstiffness":
                    stf.MustMatch("(");
                    Couplers[CouplerCountLocation].SetCompressionStiffness(stf.ReadFloat(STFReader.UNITS.Force, null), stf.ReadFloat(STFReader.UNITS.Force, null));
                    stf.SkipRestOfBlock();
                    break;

                case "wagon(coupling(spring(ortstensionslack":
                    stf.MustMatch("(");
                    IsAdvancedCoupler = true; // If this parameter is present in WAG file then treat coupler as advanced ones.
                    Couplers[CouplerCountLocation].SetTensionSlack(stf.ReadFloat(STFReader.UNITS.Distance, null), stf.ReadFloat(STFReader.UNITS.Distance, null));
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(coupling(spring(ortscompressionslack":
                    stf.MustMatch("(");
                    IsAdvancedCoupler = true; // If this parameter is present in WAG file then treat coupler as advanced ones.
                    Couplers[CouplerCountLocation].SetCompressionSlack(stf.ReadFloat(STFReader.UNITS.Distance, null), stf.ReadFloat(STFReader.UNITS.Distance, null));
                    stf.SkipRestOfBlock();
                    break;
                // This is for the advanced coupler and is designed to be used instead of the MSTS parameter Break

                case "wagon(coupling(spring(ortsbreak":
                    stf.MustMatch("(");
                    Couplers[CouplerCountLocation].SetAdvancedBreak(stf.ReadFloat(STFReader.UNITS.Force, null), stf.ReadFloat(STFReader.UNITS.Force, null));
                    stf.SkipRestOfBlock();
                    break;

                // This is for the advanced coupler and is designed to be used instead of the MSTS parameter R0
                case "wagon(coupling(spring(ortstensionr0":
                    stf.MustMatch("(");
                    Couplers[CouplerCountLocation].SetTensionR0(stf.ReadFloat(STFReader.UNITS.Distance, null), stf.ReadFloat(STFReader.UNITS.Distance, null));
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(coupling(spring(ortscompressionr0":
                    stf.MustMatch("(");
                    Couplers[CouplerCountLocation].SetCompressionR0(stf.ReadFloat(STFReader.UNITS.Distance, null), stf.ReadFloat(STFReader.UNITS.Distance, null));
                    stf.SkipRestOfBlock();
                    break;


                // Used for both coupler types
                case "wagon(coupling(couplinghasrigidconnection":
                    Couplers[CouplerCountLocation].Rigid = false;
                    Couplers[CouplerCountLocation].Rigid = stf.ReadBoolBlock(true);
                    break;



                case "wagon(adheasion":
                    stf.MustMatch("(");
                    Adhesion1 = stf.ReadFloat(STFReader.UNITS.None, null);
                    Adhesion2 = stf.ReadFloat(STFReader.UNITS.None, null);
                    Adhesion3 = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsadhesion(ortscurtius_kniffler":
                    //e.g. Wagon ( ORTSAdhesion ( ORTSCurtius_Kniffler ( 7.5 44 0.161 0.7 ) ) )
                    stf.MustMatch("(");
                    Curtius_KnifflerA = stf.ReadFloat(STFReader.UNITS.None, 7.5f); if (Curtius_KnifflerA <= 0) Curtius_KnifflerA = 7.5f;
                    Curtius_KnifflerB = stf.ReadFloat(STFReader.UNITS.None, 44.0f); if (Curtius_KnifflerB <= 0) Curtius_KnifflerB = 44.0f;
                    Curtius_KnifflerC = stf.ReadFloat(STFReader.UNITS.None, 0.161f); if (Curtius_KnifflerC <= 0) Curtius_KnifflerC = 0.161f;
                    AdhesionK = stf.ReadFloat(STFReader.UNITS.None, 0.7f); if (AdhesionK <= 0) AdhesionK = 0.7f;
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsadhesion(ortsslipwarningthreshold":
                    stf.MustMatch("(");
                    SlipWarningThresholdPercent = stf.ReadFloat(STFReader.UNITS.None, 70.0f); if (SlipWarningThresholdPercent <= 0) SlipWarningThresholdPercent = 70.0f;
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsadhesion(ortsantislip":
                    stf.MustMatch("(");
                    //AntislipControl = stf.ReadStringBlock(null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsadhesion(wheelset(axle(ortsinertia":
                    stf.MustMatch("(");
                    AxleInertiaKgm2 = stf.ReadFloat(STFReader.UNITS.RotationalInertia, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(ortsadhesion(wheelset(axle(ortsradius":
                    stf.MustMatch("(");
                    AdhesionDriveWheelRadiusM = stf.ReadFloat(STFReader.UNITS.Distance, null);
                    stf.SkipRestOfBlock();
                    break;
                case "wagon(lights":
                    //Lights = new LightCollection(stf);
                    break;
                case "wagon(inside": HasInsideView = true; ParseWagonInside(stf); break;
                case "wagon(orts3dcab": Parse3DCab(stf); break;
                case "wagon(numwheels": MSTSWagonNumWheels = stf.ReadFloatBlock(STFReader.UNITS.None, 4.0f); break;
                case "wagon(ortsnumberaxles": WagonNumAxles = stf.ReadIntBlock(null); break;
                case "wagon(ortsnumberbogies": WagonNumBogies = stf.ReadIntBlock(null); break;
                case "wagon(ortspantographs":
                    //Pantographs.Parse(lowercasetoken, stf);
                    break;

                case "wagon(ortspowersupply":
                case "wagon(ortspowerondelay":
                case "wagon(ortsbattery(mode":
                case "wagon(ortsbattery(delay":
                case "wagon(ortspowersupplycontinuouspower":
                case "wagon(ortspowersupplyheatingpower":
                case "wagon(ortspowersupplyairconditioningpower":
                case "wagon(ortspowersupplyairconditioningyield":
                    //if (this is MSTSLocomotive)
                    //{
                    //    Trace.TraceWarning($"Defining the {lowercasetoken} parameter is forbidden for locomotives (in {stf.FileName}:line {stf.LineNumber})");
                    //}
                    //else if (PassengerCarPowerSupply == null)
                    //{
                    //    PowerSupply = new ScriptedPassengerCarPowerSupply(this);
                    //}
                    //PassengerCarPowerSupply?.Parse(lowercasetoken, stf);
                    break;

                case "wagon(intakepoint": IntakePointList.Add(new IntakePoint(stf)); break;
                case "wagon(passengercapacity": HasPassengerCapacity = true; break;
                case "wagon(ortsfreightanims":
                    //FreightAnimations = new FreightAnimations(stf, this);
                    break;
                case "wagon(ortsexternalsoundpassedthroughpercent": ExternalSoundPassThruPercent = stf.ReadFloatBlock(STFReader.UNITS.None, -1); break;
                case "wagon(ortsalternatepassengerviewpoints": // accepted only if there is already a passenger viewpoint
                    if (HasInsideView)
                    {
                        ParseAlternatePassengerViewPoints(stf);
                    }
                    else stf.SkipRestOfBlock();
                    break;
                default:
                    //if (MSTSBrakeSystem != null)
                    //    MSTSBrakeSystem.Parse(lowercasetoken, stf);
                    break;
            }
        }

        /// <summary>
        /// This initializer is called when we are making a new copy of a car already
        /// loaded in memory.  We use this one to speed up loading by eliminating the
        /// need to parse the wag file multiple times.
        /// 
        /// IMPORTANT NOTE:  everything you initialized in parse, must be initialized here
        /// </summary>
        public virtual void Copy(MSTSWagon copy)
        {
            MainShapeFileName = copy.MainShapeFileName;
            HasPassengerCapacity = copy.HasPassengerCapacity;
            WagonType = copy.WagonType;
            //WagonSpecialType = copy.WagonSpecialType;
            FreightShapeFileName = copy.FreightShapeFileName;
            FreightAnimMaxLevelM = copy.FreightAnimMaxLevelM;
            FreightAnimMinLevelM = copy.FreightAnimMinLevelM;
            FreightAnimFlag = copy.FreightAnimFlag;
            FrontCouplerShapeFileName = copy.FrontCouplerShapeFileName;
            FrontCouplerAnimWidthM = copy.FrontCouplerAnimWidthM;
            FrontCouplerAnimHeightM = copy.FrontCouplerAnimHeightM;
            FrontCouplerAnimLengthM = copy.FrontCouplerAnimLengthM;
            FrontCouplerOpenShapeFileName = copy.FrontCouplerOpenShapeFileName;
            FrontCouplerOpenAnimWidthM = copy.FrontCouplerOpenAnimWidthM;
            FrontCouplerOpenAnimHeightM = copy.FrontCouplerOpenAnimHeightM;
            FrontCouplerOpenAnimLengthM = copy.FrontCouplerOpenAnimLengthM;
            FrontCouplerOpenFitted = copy.FrontCouplerOpenFitted;
            RearCouplerShapeFileName = copy.RearCouplerShapeFileName;
            RearCouplerAnimWidthM = copy.RearCouplerAnimWidthM;
            RearCouplerAnimHeightM = copy.RearCouplerAnimHeightM;
            RearCouplerAnimLengthM = copy.RearCouplerAnimLengthM;
            RearCouplerOpenShapeFileName = copy.RearCouplerOpenShapeFileName;
            RearCouplerOpenAnimWidthM = copy.RearCouplerOpenAnimWidthM;
            RearCouplerOpenAnimHeightM = copy.RearCouplerOpenAnimHeightM;
            RearCouplerOpenAnimLengthM = copy.RearCouplerOpenAnimLengthM;
            RearCouplerOpenFitted = copy.RearCouplerOpenFitted;

            FrontAirHoseShapeFileName = copy.FrontAirHoseShapeFileName;
            FrontAirHoseAnimWidthM = copy.FrontAirHoseAnimWidthM;
            FrontAirHoseAnimHeightM = copy.FrontAirHoseAnimHeightM;
            FrontAirHoseAnimLengthM = copy.FrontAirHoseAnimLengthM;

            FrontAirHoseDisconnectedShapeFileName = copy.FrontAirHoseDisconnectedShapeFileName;
            FrontAirHoseDisconnectedAnimWidthM = copy.FrontAirHoseDisconnectedAnimWidthM;
            FrontAirHoseDisconnectedAnimHeightM = copy.FrontAirHoseDisconnectedAnimHeightM;
            FrontAirHoseDisconnectedAnimLengthM = copy.FrontAirHoseDisconnectedAnimLengthM;

            RearAirHoseShapeFileName = copy.RearAirHoseShapeFileName;
            RearAirHoseAnimWidthM = copy.RearAirHoseAnimWidthM;
            RearAirHoseAnimHeightM = copy.RearAirHoseAnimHeightM;
            RearAirHoseAnimLengthM = copy.RearAirHoseAnimLengthM;

            RearAirHoseDisconnectedShapeFileName = copy.RearAirHoseDisconnectedShapeFileName;
            RearAirHoseDisconnectedAnimWidthM = copy.RearAirHoseDisconnectedAnimWidthM;
            RearAirHoseDisconnectedAnimHeightM = copy.RearAirHoseDisconnectedAnimHeightM;
            RearAirHoseDisconnectedAnimLengthM = copy.RearAirHoseDisconnectedAnimLengthM;

            CarWidthM = copy.CarWidthM;
            CarHeightM = copy.CarHeightM;
            CarLengthM = copy.CarLengthM;
            TrackGaugeM = copy.TrackGaugeM;
            CentreOfGravityM = copy.CentreOfGravityM;
            InitialCentreOfGravityM = copy.InitialCentreOfGravityM;
            UnbalancedSuperElevationM = copy.UnbalancedSuperElevationM;
            RigidWheelBaseM = copy.RigidWheelBaseM;
            CarBogieCentreLengthM = copy.CarBogieCentreLengthM;
            CarBodyLengthM = copy.CarBodyLengthM;
            CarCouplerFaceLengthM = copy.CarCouplerFaceLengthM;
            CarAirHoseLengthM = copy.CarAirHoseLengthM;
            CarAirHoseHorizontalLengthM = copy.CarAirHoseHorizontalLengthM;
            AuxTenderWaterMassKG = copy.AuxTenderWaterMassKG;
            TenderWagonMaxCoalMassKG = copy.TenderWagonMaxCoalMassKG;
            TenderWagonMaxWaterMassKG = copy.TenderWagonMaxWaterMassKG;
            WagonNumAxles = copy.WagonNumAxles;
            WagonNumBogies = copy.WagonNumBogies;
            MSTSWagonNumWheels = copy.MSTSWagonNumWheels;
            MassKG = copy.MassKG;
            InitialMassKG = copy.InitialMassKG;
            WheelRadiusM = copy.WheelRadiusM;
            DriverWheelRadiusM = copy.DriverWheelRadiusM;
            MainSoundFileName = copy.MainSoundFileName;
            BrakeShoeFrictionFactor = copy.BrakeShoeFrictionFactor;
            WheelBrakeSlideProtectionFitted = copy.WheelBrakeSlideProtectionFitted;
            WheelBrakeSlideProtectionLimitDisabled = copy.WheelBrakeSlideProtectionLimitDisabled;
            InitialMaxBrakeForceN = copy.InitialMaxBrakeForceN;
            InitialMaxHandbrakeForceN = copy.InitialMaxHandbrakeForceN;
            MaxBrakeForceN = copy.MaxBrakeForceN;
            MaxHandbrakeForceN = copy.MaxHandbrakeForceN;
            WindowDeratingFactor = copy.WindowDeratingFactor;
            DesiredCompartmentTempSetpointC = copy.DesiredCompartmentTempSetpointC;
            CompartmentHeatingPipeAreaFactor = copy.CompartmentHeatingPipeAreaFactor;
            MainSteamHeatPipeOuterDiaM = copy.MainSteamHeatPipeOuterDiaM;
            MainSteamHeatPipeInnerDiaM = copy.MainSteamHeatPipeInnerDiaM;
            CarConnectSteamHoseInnerDiaM = copy.CarConnectSteamHoseInnerDiaM;
            CarConnectSteamHoseOuterDiaM = copy.CarConnectSteamHoseOuterDiaM;
            MaximumSteamHeatBoilerWaterTankCapacityL = copy.MaximumSteamHeatBoilerWaterTankCapacityL;
            MaximiumSteamHeatBoilerFuelTankCapacityL = copy.MaximiumSteamHeatBoilerFuelTankCapacityL;
            //TrainHeatBoilerWaterUsageGalukpH = new Interpolator(copy.TrainHeatBoilerWaterUsageGalukpH);
            //TrainHeatBoilerFuelUsageGalukpH = new Interpolator(copy.TrainHeatBoilerFuelUsageGalukpH);
            DavisAN = copy.DavisAN;
            DavisBNSpM = copy.DavisBNSpM;
            DavisCNSSpMM = copy.DavisCNSSpMM;
            DavisDragConstant = copy.DavisDragConstant;
            WagonFrontalAreaM2 = copy.WagonFrontalAreaM2;
            TrailLocoResistanceFactor = copy.TrailLocoResistanceFactor;
            FrictionC1 = copy.FrictionC1;
            FrictionE1 = copy.FrictionE1;
            FrictionV2 = copy.FrictionV2;
            FrictionC2 = copy.FrictionC2;
            FrictionE2 = copy.FrictionE2;
            EffectData = copy.EffectData;
            IsBelowMergeSpeed = copy.IsBelowMergeSpeed;
            StandstillFrictionN = copy.StandstillFrictionN;
            MergeSpeedFrictionN = copy.MergeSpeedFrictionN;
            MergeSpeedMpS = copy.MergeSpeedMpS;
            IsDavisFriction = copy.IsDavisFriction;
            IsRollerBearing = copy.IsRollerBearing;
            IsLowTorqueRollerBearing = copy.IsLowTorqueRollerBearing;
            IsFrictionBearing = copy.IsFrictionBearing;
            IsGreaseFrictionBearing = copy.IsGreaseFrictionBearing;
            CarBrakeSystemType = copy.CarBrakeSystemType;
            //BrakeSystem = MSTSBrakeSystem.Create(CarBrakeSystemType, this);
            EmergencyReservoirPresent = copy.EmergencyReservoirPresent;
            DistributorPresent = copy.DistributorPresent;
            HandBrakePresent = copy.HandBrakePresent;
            ManualBrakePresent = copy.ManualBrakePresent;
            AuxiliaryReservoirPresent = copy.AuxiliaryReservoirPresent;
            RetainerPositions = copy.RetainerPositions;
            InteriorShapeFileName = copy.InteriorShapeFileName;
            InteriorSoundFileName = copy.InteriorSoundFileName;
            Cab3DShapeFileName = copy.Cab3DShapeFileName;
            Cab3DSoundFileName = copy.Cab3DSoundFileName;
            Adhesion1 = copy.Adhesion1;
            Adhesion2 = copy.Adhesion2;
            Adhesion3 = copy.Adhesion3;
            Curtius_KnifflerA = copy.Curtius_KnifflerA;
            Curtius_KnifflerB = copy.Curtius_KnifflerB;
            Curtius_KnifflerC = copy.Curtius_KnifflerC;
            AdhesionK = copy.AdhesionK;
            AxleInertiaKgm2 = copy.AxleInertiaKgm2;
            AdhesionDriveWheelRadiusM = copy.AdhesionDriveWheelRadiusM;
            SlipWarningThresholdPercent = copy.SlipWarningThresholdPercent;
            //Lights = copy.Lights;
            ExternalSoundPassThruPercent = copy.ExternalSoundPassThruPercent;
            //foreach (PassengerViewPoint passengerViewPoint in copy.PassengerViewpoints)
            //    PassengerViewpoints.Add(passengerViewPoint);
            foreach (ViewPoint headOutViewPoint in copy.HeadOutViewpoints)
                HeadOutViewpoints.Add(headOutViewPoint);
            //if (copy.CabViewpoints != null)
            //{
            //    CabViewpoints = new List<PassengerViewPoint>();
            //    foreach (PassengerViewPoint cabViewPoint in copy.CabViewpoints)
            //        CabViewpoints.Add(cabViewPoint);
            //}
            IsAdvancedCoupler = copy.IsAdvancedCoupler;
            foreach (MSTSCoupling coupler in copy.Couplers)
                Couplers.Add(coupler);
            //Pantographs.Copy(copy.Pantographs);
            //if (copy.FreightAnimations != null)
            //{
            //    FreightAnimations = new FreightAnimations(copy.FreightAnimations, this);
            //}

            LoadEmptyMassKg = copy.LoadEmptyMassKg;
            LoadEmptyCentreOfGravityM_Y = copy.LoadEmptyCentreOfGravityM_Y;
            LoadEmptyMaxBrakeForceN = copy.LoadEmptyMaxBrakeForceN;
            LoadEmptyMaxHandbrakeForceN = copy.LoadEmptyMaxHandbrakeForceN;
            LoadEmptyORTSDavis_A = copy.LoadEmptyORTSDavis_A;
            LoadEmptyORTSDavis_B = copy.LoadEmptyORTSDavis_B;
            LoadEmptyORTSDavis_C = copy.LoadEmptyORTSDavis_C;
            LoadEmptyDavisDragConstant = copy.LoadEmptyDavisDragConstant;
            LoadEmptyWagonFrontalAreaM2 = copy.LoadEmptyWagonFrontalAreaM2;
            LoadFullMassKg = copy.LoadFullMassKg;
            LoadFullCentreOfGravityM_Y = copy.LoadFullCentreOfGravityM_Y;
            LoadFullMaxBrakeForceN = copy.LoadFullMaxBrakeForceN;
            LoadFullMaxHandbrakeForceN = copy.LoadFullMaxHandbrakeForceN;
            LoadFullORTSDavis_A = copy.LoadFullORTSDavis_A;
            LoadFullORTSDavis_B = copy.LoadFullORTSDavis_B;
            LoadFullORTSDavis_C = copy.LoadFullORTSDavis_C;
            LoadFullDavisDragConstant = copy.LoadFullDavisDragConstant;
            LoadFullWagonFrontalAreaM2 = copy.LoadFullWagonFrontalAreaM2;

            if (copy.IntakePointList != null)
            {
                foreach (IntakePoint copyIntakePoint in copy.IntakePointList)
                {
                    // If freight animations not used or else wagon is a tender or locomotive, use the "MSTS" type IntakePoints if present in WAG / ENG file

                    //if (copyIntakePoint.LinkedFreightAnim == null)
                        //     if (copyIntakePoint.LinkedFreightAnim == null || WagonType == WagonTypes.Engine || WagonType == WagonTypes.Tender || AuxWagonType == "AuxiliaryTender")
                        //IntakePointList.Add(new IntakePoint(copyIntakePoint));
                }
            }

            //MSTSBrakeSystem.InitializeFromCopy(copy.BrakeSystem);
            //if (copy.WeightLoadController != null) WeightLoadController = new MSTSNotchController(copy.WeightLoadController);

            //if (copy.PassengerCarPowerSupply != null)
            //{
            //    PowerSupply = new ScriptedPassengerCarPowerSupply(this);
            //    PassengerCarPowerSupply.Copy(copy.PassengerCarPowerSupply);
            //}
        }

        protected void ParseWagonInside(STFReader stf)
        {
            //PassengerViewPoint passengerViewPoint = new PassengerViewPoint();
            stf.MustMatch("(");
            Vector3 popo;
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("sound", ()=>{ InteriorSoundFileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("passengercabinfile", ()=>{ InteriorShapeFileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("passengercabinheadpos", ()=>{ popo = stf.ReadVector3Block(STFReader.UNITS.Distance, new Vector3()); }),
                new STFReader.TokenProcessor("rotationlimit", ()=>{ popo = stf.ReadVector3Block(STFReader.UNITS.None, new Vector3()); }),
                new STFReader.TokenProcessor("startdirection", ()=>{ popo = stf.ReadVector3Block(STFReader.UNITS.None, new Vector3()); }),
            });
            // Set initial direction
            //passengerViewPoint.RotationXRadians = MathHelper.ToRadians(passengerViewPoint.StartDirection.X);
            //passengerViewPoint.RotationYRadians = MathHelper.ToRadians(passengerViewPoint.StartDirection.Y);
            //PassengerViewpoints.Add(passengerViewPoint);
        }
        protected void Parse3DCab(STFReader stf)
        {
            //PassengerViewPoint passengerViewPoint = new PassengerViewPoint();
            stf.MustMatch("(");
            Vector3 popo;
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("sound", ()=>{ Cab3DSoundFileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("orts3dcabfile", ()=>{ Cab3DShapeFileName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("orts3dcabheadpos", ()=>{ popo = stf.ReadVector3Block(STFReader.UNITS.Distance, new Vector3()); }),
                new STFReader.TokenProcessor("rotationlimit", ()=>{ popo = stf.ReadVector3Block(STFReader.UNITS.None, new Vector3()); }),
                new STFReader.TokenProcessor("startdirection", ()=>{ popo = stf.ReadVector3Block(STFReader.UNITS.None, new Vector3()); }),
            });
            // Set initial direction
            //passengerViewPoint.RotationXRadians = MathHelper.ToRadians(passengerViewPoint.StartDirection.X);
            //passengerViewPoint.RotationYRadians = MathHelper.ToRadians(passengerViewPoint.StartDirection.Y);
            //if (this.CabViewpoints == null) CabViewpoints = new List<PassengerViewPoint>();
            //CabViewpoints.Add(passengerViewPoint);
        }

        // parses additional passenger viewpoints, if any
        protected void ParseAlternatePassengerViewPoints(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new[] {
                new STFReader.TokenProcessor("ortsalternatepassengerviewpoint", ()=>{ ParseWagonInside(stf); }),
            });
        }

        public static float ParseFloat(string token)
        {   // is there a better way to ignore any suffix?
            while (token.Length > 0)
            {
                try
                {
                    return float.Parse(token);
                }
                catch (System.Exception)
                {
                    token = token.Substring(0, token.Length - 1);
                }
            }
            return 0;
        }




        /// <summary>
        /// Read the coupler state(s) from a save stream.
        /// </summary>
        /// <remarks>
        /// Has no side effects besides advancing the save stream, thus avoiding any shared-state pitfalls.
        /// </remarks>
        /// <param name="inf">The save stream.</param>
        /// <returns>A list of newly restored <see cref="MSTSCoupling"/> instances.</returns>
        private static IEnumerable<MSTSCoupling> ReadCouplersFromSave(BinaryReader inf)
        {
            var n = inf.ReadInt32();
            foreach (int _ in Enumerable.Range(0, n))
            {
                var coupler = new MSTSCoupling();
                coupler.Restore(inf);
                yield return coupler;
            }
        }

        public override void Update(float elapsedClockSeconds)
        {
            base.Update(elapsedClockSeconds);           
        }
        public override void SignalEvent(Event evt)
        {

            // TODO: This should be moved to TrainCar probably.
            try
            {
                foreach (var eventHandler in EventHandlers) // e.g. for HandleCarEvent() in Sounds.cs
                    eventHandler.HandleEvent(evt);
            }
            catch (Exception error)
            {
                Trace.TraceInformation("Sound event skipped due to thread safety problem " + error.Message);
            }

            base.SignalEvent(evt);
        }


        public void ToggleDoorsLeft()
        {
            DoorLeftOpen = !DoorLeftOpen;
            if (Simulator.PlayerLocomotive == this) // second part for remote trains
            {//inform everyone else in the train
                foreach (var car in Train.Cars)
                {
                    var mstsWagon = car as MSTSWagon;
                    if (car != this && mstsWagon != null)
                    {
                        if (!car.Flipped ^ Flipped)
                        {
                            mstsWagon.DoorLeftOpen = DoorLeftOpen;
                            mstsWagon.SignalEvent(DoorLeftOpen ? Event.DoorOpen : Event.DoorClose); // hook for sound trigger
                        }
                        else
                        {
                            mstsWagon.DoorRightOpen = DoorLeftOpen;
                            mstsWagon.SignalEvent(DoorLeftOpen ? Event.DoorOpen : Event.DoorClose); // hook for sound trigger
                        }
                    }
                }
                if (DoorLeftOpen) SignalEvent(Event.DoorOpen); // hook for sound trigger
                else SignalEvent(Event.DoorClose);
            }
        }

        public void ToggleDoorsRight()
        {
            DoorRightOpen = !DoorRightOpen;
            if (Simulator.PlayerLocomotive == this) // second part for remote trains
            { //inform everyone else in the train
                foreach (TrainCar car in Train.Cars)
                {
                    var mstsWagon = car as MSTSWagon;
                    if (car != this && mstsWagon != null)
                    {
                        if (!car.Flipped ^ Flipped)
                        {
                            mstsWagon.DoorRightOpen = DoorRightOpen;
                            mstsWagon.SignalEvent(DoorRightOpen ? Event.DoorOpen : Event.DoorClose); // hook for sound trigger
                        }
                        else
                        {
                            mstsWagon.DoorLeftOpen = DoorRightOpen;
                            mstsWagon.SignalEvent(DoorRightOpen ? Event.DoorOpen : Event.DoorClose); // hook for sound trigger
                        }
                    }
                }
                if (DoorRightOpen) SignalEvent(Event.DoorOpen); // hook for sound trigger
                else SignalEvent(Event.DoorClose);
            }
        }

        public void ToggleMirrors()
        {
            MirrorOpen = !MirrorOpen;
            if (MirrorOpen) SignalEvent(Event.MirrorOpen); // hook for sound trigger
            else SignalEvent(Event.MirrorClose);
            //if (Simulator.PlayerLocomotive == this) Simulator.Confirmer.Confirm(CabControl.Mirror, MirrorOpen ? CabSetting.On : CabSetting.Off);
        }


        // sound sources and viewers can register themselves to get direct notification of an event
        public List<Tourmaline.Common.EventHandler> EventHandlers = new List<Tourmaline.Common.EventHandler>();

        public MSTSCoupling Coupler
        {
            get  // This determines which coupler to use from WAG file, typically it will be the first one as by convention the rear coupler is always read first.
            {
                if (Couplers.Count == 0) return null;
                if (Flipped && Couplers.Count > 1) return Couplers[1];
                return Couplers[0]; // defaults to the rear coupler (typically the first read)
            }
        }
    }



    /// <summary>
    /// An IntakePoint object is created for any engine or wagon having a 
    /// IntakePoint block in its ENG/WAG file. 
    /// Called from within the MSTSWagon class.
    /// </summary>
    public class IntakePoint
    {
        public float OffsetM = 0f;   // distance forward? from the centre of the vehicle as defined by LengthM/2.
        public float WidthM = 10f;   // of the filling point. Is the maximum positioning error allowed equal to this or half this value? 
        public MSTSWagon.PickupType Type;          // 'freightgrain', 'freightcoal', 'freightgravel', 'freightsand', 'fuelcoal', 'fuelwater', 'fueldiesel', 'fuelwood', freightgeneral, freightlivestock, specialmail
        public float? DistanceFromFrontOfTrainM;
        //public FreightAnimationContinuous LinkedFreightAnim = null;

        public IntakePoint()
        {
        }

        public IntakePoint(STFReader stf)
        {
            stf.MustMatch("(");
            OffsetM = stf.ReadFloat(STFReader.UNITS.None, 0f);
            WidthM = stf.ReadFloat(STFReader.UNITS.None, 10f);
            Type = (MSTSWagon.PickupType)Enum.Parse(typeof(MSTSWagon.PickupType), stf.ReadString().ToLower(), true);
            stf.SkipRestOfBlock();
        }

        // for copy
        public IntakePoint(IntakePoint copy)
        {
            OffsetM = copy.OffsetM;
            WidthM = copy.WidthM;
            Type = copy.Type;

        }

    }

    public class MSTSCoupling
    {
        public bool Rigid;
        public float R0X;
        public float R0Y;
        public float R0Diff = 0.012f;
        public float Stiffness1NpM = 1e7f;
        public float Stiffness2NpM = 2e7f;
        public float Break1N = 1e10f;
        public float Break2N = 1e10f;
        public float CouplerSlackAM;
        public float CouplerSlackBM;
        public float CouplerTensionSlackAM;
        public float CouplerTensionSlackBM;
        public float TensionStiffness1N = 1e7f;
        public float TensionStiffness2N = 2e7f;
        public float TensionR0X;
        public float TensionR0Y;
        public float CompressionR0X;
        public float CompressionR0Y;
        public float CompressionStiffness1N;
        public float CompressionStiffness2N;
        public float CouplerCompressionSlackAM;
        public float CouplerCompressionSlackBM;


        public MSTSCoupling()
        {
        }
        public MSTSCoupling(MSTSCoupling copy)
        {
            Rigid = copy.Rigid;
            R0X = copy.R0X;
            R0Y = copy.R0Y;
            R0Diff = copy.R0Diff;
            Break1N = copy.Break1N;
            Break2N = copy.Break2N;
            Stiffness1NpM = copy.Stiffness1NpM;
            Stiffness2NpM = copy.Stiffness2NpM;
            CouplerSlackAM = copy.CouplerSlackAM;
            CouplerSlackBM = copy.CouplerSlackBM;
            TensionStiffness1N = copy.TensionStiffness1N;
            TensionStiffness2N = copy.TensionStiffness2N;
            CouplerTensionSlackAM = copy.CouplerTensionSlackAM;
            CouplerTensionSlackBM = copy.CouplerTensionSlackBM;
            TensionR0X = copy.TensionR0X;
            TensionR0Y = copy.TensionR0Y;
            CompressionR0X = copy.CompressionR0X;
            CompressionR0Y = copy.CompressionR0Y;
            CompressionStiffness1N = copy.CompressionStiffness1N;
            CompressionStiffness2N = copy.CompressionStiffness2N;
            CouplerCompressionSlackAM = copy.CouplerCompressionSlackAM;
            CouplerCompressionSlackBM = copy.CouplerCompressionSlackBM;
        }
        public void SetSimpleR0(float a, float b)
        {
            R0X = a;
            R0Y = b;
            if (a == 0)
                R0Diff = b / 2 * Stiffness2NpM / (Stiffness1NpM + Stiffness2NpM);
            else
                R0Diff = 0.012f;
            //               R0Diff = b - a;

            // Ensure R0Diff stays within "reasonable limits"
            if (R0Diff < 0.001)
                R0Diff = 0.001f;
            else if (R0Diff > 0.1)
                R0Diff = 0.1f;

        }
        public void SetSimpleStiffness(float a, float b)
        {
            if (a + b < 0)
                return;

            Stiffness1NpM = a;
            Stiffness2NpM = b;
        }

        public void SetTensionR0(float a, float b)
        {
            TensionR0X = a;
            TensionR0Y = b;
        }

        public void SetCompressionR0(float a, float b)
        {
            CompressionR0X = a;
            CompressionR0Y = b;
        }

        public void SetTensionStiffness(float a, float b)
        {
            if (a + b < 0)
                return;

            TensionStiffness1N = a;
            TensionStiffness2N = b;
        }

        public void SetCompressionStiffness(float a, float b)
        {
            if (a + b < 0)
                return;

            CompressionStiffness1N = a;
            CompressionStiffness2N = b;
        }

        public void SetTensionSlack(float a, float b)
        {
            if (a + b < 0)
                return;

            CouplerTensionSlackAM = a;
            CouplerTensionSlackBM = b;
        }

        public void SetCompressionSlack(float a, float b)
        {
            if (a + b < 0)
                return;

            CouplerCompressionSlackAM = a;
            CouplerCompressionSlackBM = b;
        }

        public void SetAdvancedBreak(float a, float b)
        {
            if (a + b < 0)
                return;

            Break1N = a;

            // Check if b = 0, as some stock has a zero value, set a default
            if (b == 0)
            {
                Break2N = 2e7f;
            }
            else
            {
                Break2N = b;
            }

        }


        public void SetSlack(float a, float b)
        {
            if (a + b < 0)
                return;

            CouplerSlackAM = a;
            CouplerSlackBM = b;
        }

        public void SetSimpleBreak(float a, float b)
        {
            if (a + b < 0)
                return;

            Break1N = a;

            // Check if b = 0, as some stock has a zero value, set a default
            if (b == 0)
            {
                Break2N = 2e7f;
            }
            else
            {
                Break2N = b;
            }

        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public void Save(BinaryWriter outf)
        {
            outf.Write(Rigid);
            outf.Write(R0X);
            outf.Write(R0Y);
            outf.Write(R0Diff);
            outf.Write(Stiffness1NpM);
            outf.Write(Stiffness2NpM);
            outf.Write(CouplerSlackAM);
            outf.Write(CouplerSlackBM);
            outf.Write(Break1N);
            outf.Write(Break2N);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public void Restore(BinaryReader inf)
        {
            Rigid = inf.ReadBoolean();
            R0X = inf.ReadSingle();
            R0Y = inf.ReadSingle();
            R0Diff = inf.ReadSingle();
            Stiffness1NpM = inf.ReadSingle();
            Stiffness2NpM = inf.ReadSingle();
            CouplerSlackAM = inf.ReadSingle();
            CouplerSlackBM = inf.ReadSingle();
            Break1N = inf.ReadSingle();
            Break2N = inf.ReadSingle();
        }
    }

    /// <summary>
    /// Utility class to avoid loading the wag file multiple times
    /// </summary>
    public class CarManager
    {
        public static Dictionary<string, MSTSWagon> LoadedCars = new Dictionary<string, MSTSWagon>();
    }

    public struct ParticleEmitterData
    {
        public readonly Vector3 XNALocation;
        public readonly Vector3 XNADirection;
        public readonly float NozzleWidth;

        public ParticleEmitterData(STFReader stf)
        {
            stf.MustMatch("(");
            XNALocation.X = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNALocation.Y = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNALocation.Z = -stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNADirection.X = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNADirection.Y = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNADirection.Z = -stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            XNADirection.Normalize();
            NozzleWidth = stf.ReadFloat(STFReader.UNITS.Distance, 0.0f);
            stf.SkipRestOfBlock();
        }
    }
}
