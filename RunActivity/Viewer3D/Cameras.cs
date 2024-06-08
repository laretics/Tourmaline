//#define ENABLE_ORTS_PARTS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Tourmaline.Common;
using TOURMALINE.Common;
using TOURMALINE.Common.Input;
//using TOURMALINE.Settings;
using Tourmaline.Viewer3D;
using Tourmaline.Simulation;
using Tourmaline.Simulation.RollingStocks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

#if ENABLE_ORTS_PARTS
using Orts.Formats.Msts;
using Orts.Simulation.Physics;
using Orts.Simulation.Signalling;
#endif

namespace Tourmaline.Viewer3D
{
    public abstract class Camera
    {
        protected long CommandStartTime;

        // 2.1 sets the limit at just under a right angle as get unwanted swivel at the full right angle.
        protected static CameraAngleClamper VerticalClamper = new CameraAngleClamper(-MathHelper.Pi / 2.1f, MathHelper.Pi / 2.1f);
        public const int TerrainAltitudeMargin = 2;

        protected readonly Viewer Viewer;

        //protected WorldLocation cameraLocation = new WorldLocation();
        protected Vector3 cameraLocation = new Vector3();
        //public int TileX { get { return cameraLocation.TileX; } }
        //public int TileZ { get { return cameraLocation.TileZ; } }
        public Vector3 Location { get { return cameraLocation; } }
        //public WorldLocation CameraWorldLocation { get { return cameraLocation; } }
        public Vector3 CameraWorldLocation { get => cameraLocation; }
        protected int MouseScrollValue;
        protected internal float FieldOfView;

        protected Matrix xnaView;
        public Matrix XnaView { get { return xnaView; } }

        Matrix xnaProjection;
        public Matrix XnaProjection { get { return xnaProjection; } }
        public static Matrix XnaDistantMountainProjection;
        Vector3 frustumRightProjected;
        Vector3 frustumLeft;
        Vector3 frustumRight;

        // This sucks. It's really not camera-related at all.
        public static Matrix XNASkyProjection;

        // The following group of properties are used by other code to vary
        // behavior by camera; e.g. Style is used for activating sounds,
        // AttachedCar for rendering the train or not, and IsUnderground for
        // automatically switching to/from cab view in tunnels.
        public enum Styles { External, Cab, Passenger, ThreeDimCab }
        public virtual Styles Style { get { return Styles.External; } }
        public virtual TrainCar AttachedCar { get { return null; } }
        public virtual bool IsAvailable { get { return true; } }
        public virtual bool IsUnderground { get { return false; } }
        public virtual string Name { get { return ""; } }

        // We need to allow different cameras to have different near planes.
        public virtual float NearPlane { get { return 1.0f; } }

        public float ReplaySpeed { get; set; }
        const int SpeedFactorFastSlow = 8;  // Use by GetSpeed
        protected const float SpeedAdjustmentForRotation = 0.1f;

        protected Camera(Viewer viewer)
        {
            Viewer = viewer;
            FieldOfView = viewer.Game.ViewingFOV;
        }

        protected Camera(Viewer viewer, Camera previousCamera) // maintain visual continuity
            : this(viewer)
        {
            if (previousCamera != null)
            {
                cameraLocation = previousCamera.CameraWorldLocation;
                FieldOfView = previousCamera.FieldOfView;
            }
        }

        [CallOnThread("Updater")]
        protected internal virtual void Save(BinaryWriter output)
        {
            //cameraLocation.Save(output);
            output.Write(FieldOfView);
        }

        [CallOnThread("Render")]
        protected internal virtual void Restore(BinaryReader input)
        {
            //cameraLocation.Restore(input);
            FieldOfView = input.ReadSingle();
        }

        /// <summary>
        /// Resets a camera's position, location and attachment information.
        /// </summary>
        public virtual void Reset()
        {
            FieldOfView = Viewer.Game.ViewingFOV;
            ScreenChanged();
        }

        /// <summary>
        /// Switches the <see cref="Viewer3D"/> to this camera, updating the view information.
        /// </summary>
        public void Activate()
        {
            ScreenChanged();
            OnActivate(Viewer.Camera == this);
            Viewer.Camera = this;
            Update(0);
            xnaView = GetCameraView();
            //SoundBaseTile = new Point(cameraLocation.TileX, cameraLocation.TileZ);
        }

        /// <summary>
        /// A camera can use this method to handle any preparation when being activated.
        /// </summary>
        protected virtual void OnActivate(bool sameCamera)
        {
        }

        /// <summary>
        /// A camera can use this method to respond to user input.
        /// </summary>
        /// <param name="elapsedTime"></param>
        public virtual void HandleUserInput(long elapsedTime)
        {

        }

        /// <summary>
        /// A camera can use this method to update any calculated data that may have changed.
        /// </summary>
        /// <param name="elapsedTime"></param>
        public virtual void Update(long elapsedTime)
        {
        }

        /// <summary>
        /// A camera should use this method to return a unique view.
        /// </summary>
        protected abstract Matrix GetCameraView();

        /// <summary>
        /// Notifies the camera that the screen dimensions have changed.
        /// </summary>
        public void ScreenChanged()
        {
            var aspectRatio = (float)Viewer.DisplaySize.X / Viewer.DisplaySize.Y;
            var farPlaneDistance = SkyConstants.skyRadius + 100;  // so far the sky is the biggest object in view
            var fovWidthRadians = MathHelper.ToRadians(FieldOfView);
            xnaProjection = Matrix.CreatePerspectiveFieldOfView(fovWidthRadians, aspectRatio, NearPlane, Viewer.Game.ViewingDistance);
            XNASkyProjection = Matrix.CreatePerspectiveFieldOfView(fovWidthRadians, aspectRatio, NearPlane, farPlaneDistance);    // TODO remove? 
            frustumRightProjected.X = (float)Math.Cos(fovWidthRadians / 2 * aspectRatio);  // Precompute the right edge of the view frustrum.
            frustumRightProjected.Z = (float)Math.Sin(fovWidthRadians / 2 * aspectRatio);
        }

        /// <summary>
        /// Updates view and projection from this camera's data.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="elapsedTime"></param>
        public void PrepareFrame(RenderFrame frame, long elapsedTime)
        {
            xnaView = GetCameraView();
            frame.SetCamera(this);
            frustumLeft.X = -xnaView.M11 * frustumRightProjected.X + xnaView.M13 * frustumRightProjected.Z;
            frustumLeft.Y = -xnaView.M21 * frustumRightProjected.X + xnaView.M23 * frustumRightProjected.Z;
            frustumLeft.Z = -xnaView.M31 * frustumRightProjected.X + xnaView.M33 * frustumRightProjected.Z;
            frustumLeft.Normalize();
            frustumRight.X = xnaView.M11 * frustumRightProjected.X + xnaView.M13 * frustumRightProjected.Z;
            frustumRight.Y = xnaView.M21 * frustumRightProjected.X + xnaView.M23 * frustumRightProjected.Z;
            frustumRight.Z = xnaView.M31 * frustumRightProjected.X + xnaView.M33 * frustumRightProjected.Z;
            frustumRight.Normalize();
        }

        // Cull for fov
        public bool InFov(Vector3 mstsObjectCenter, float objectRadius)
        {
            mstsObjectCenter.X -= cameraLocation.X;
            mstsObjectCenter.Y -= cameraLocation.Y;
            mstsObjectCenter.Z -= cameraLocation.Z;
            // TODO: This *2 is a complete fiddle because some objects don't currently pass in a correct radius and e.g. track sections vanish.
            objectRadius *= 2;
            if (frustumLeft.X * mstsObjectCenter.X + frustumLeft.Y * mstsObjectCenter.Y - frustumLeft.Z * mstsObjectCenter.Z > objectRadius)
                return false;
            if (frustumRight.X * mstsObjectCenter.X + frustumRight.Y * mstsObjectCenter.Y - frustumRight.Z * mstsObjectCenter.Z > objectRadius)
                return false;
            return true;
        }

        // Cull for distance
        public bool InRange(Vector3 mstsObjectCenter, float objectRadius, float objectViewingDistance)
        {
            mstsObjectCenter.X -= cameraLocation.X;
            mstsObjectCenter.Z -= cameraLocation.Z;

            // An object cannot be visible further away than the viewing distance.
            if (objectViewingDistance > Viewer.Game.ViewingDistance)
                objectViewingDistance = Viewer.Game.ViewingDistance;

            var distanceSquared = mstsObjectCenter.X * mstsObjectCenter.X + mstsObjectCenter.Z * mstsObjectCenter.Z;

            return distanceSquared < (objectRadius + objectViewingDistance) * (objectRadius + objectViewingDistance);
        }

        /// <summary>
        /// If the nearest part of the object is within camera viewing distance
        /// and is within the object's defined viewing distance then
        /// we can see it.   The objectViewingDistance allows a small object
        /// to specify a cutoff beyond which the object can't be seen.
        /// </summary>
        public bool CanSee(Vector3 mstsObjectCenter, float objectRadius, float objectViewingDistance)
        {
            if (!InRange(mstsObjectCenter, objectRadius, objectViewingDistance))
                return false;

            if (!InFov(mstsObjectCenter, objectRadius))
                return false;

            return true;
        }

        protected static float GetSpeed(long elapsed)
        {
            var speed = 5 * (float)elapsed/1000;
            if (UserInput.IsDown(UserCommand.CameraMoveFast))
                speed *= SpeedFactorFastSlow;
            if (UserInput.IsDown(UserCommand.CameraMoveSlow))
                speed /= SpeedFactorFastSlow;
            return speed;
        }

        protected virtual void ZoomIn(float speed)
        {
        }

        // TODO: Add a way to record this zoom operation for Replay.
        protected void ZoomByMouseWheel(float speed)
        {
            // Will not zoom-in-out when help windows is up.
            // TODO: Property input processing through WindowManager.
            if (UserInput.IsMouseWheelChanged)// && !Viewer.HelpWindow.Visible)
            {
                var fieldOfView = MathHelper.Clamp(FieldOfView - speed * UserInput.MouseWheelChange / 10, 1, 135);
                new FieldOfViewCommand(Viewer.Log, fieldOfView);
            }
        }

        /// <summary>
        /// Returns a position in XNA space relative to the camera's tile
        /// </summary>
        /// <param name="worldLocation"></param>
        /// <returns></returns>
        public Vector3 XnaLocation(Vector3 worldLocation)
        {
            var xnaVector = worldLocation;
            //xnaVector.X += 2048 * (worldLocation.TileX - cameraLocation.TileX);
            //xnaVector.Z += 2048 * (worldLocation.TileZ - cameraLocation.TileZ);
            xnaVector.Z *= -1;
            return xnaVector;
        }


        protected class CameraAngleClamper
        {
            readonly float Minimum;
            readonly float Maximum;

            public CameraAngleClamper(float minimum, float maximum)
            {
                Minimum = minimum;
                Maximum = maximum;
            }

            public float Clamp(float angle)
            {
                return MathHelper.Clamp(angle, Minimum, Maximum);
            }
        }

        /// <summary>
        /// All OpenAL sound positions are normalized to this tile.
        /// Cannot be (0, 0) constantly, because some routes use extremely large tile coordinates,
        /// which would lead to imprecise absolute world coordinates, thus stuttering.
        /// </summary>
        public static Point SoundBaseTile = new Point(0, 0);
        /// <summary>
        /// CameraWorldLocation normalized to SoundBaseTile
        /// </summary>
        Vector3 ListenerLocation;
        /// <summary>
        /// Set OpenAL listener position based on CameraWorldLocation normalized to SoundBaseTile
        /// </summary>

    }

    public abstract class LookAtCamera : RotatingCamera
    {
        protected Vector3 targetLocation = new Vector3();
        public Vector3 TargetWorldLocation { get { return targetLocation; } }

        public override bool IsUnderground
        {
            get
            {
                //var elevationAtTarget = Viewer.Tiles.GetElevation(targetLocation);
                //return targetLocation.Location.Y + TerrainAltitudeMargin < elevationAtTarget;
                return false;
            }
        }

        protected LookAtCamera(Viewer viewer)
            : base(viewer)
        {
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            //targetLocation.Save(outf);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            //targetLocation.Restore(inf);
        }

        protected override Matrix GetCameraView()
        {
            return Matrix.CreateLookAt(XnaLocation(cameraLocation), XnaLocation(targetLocation), Vector3.UnitY);
        }
    }

    public abstract class RotatingCamera : Camera
    {
        // Current camera values
        protected float RotationXRadians;
        protected float RotationYRadians;
        protected float XRadians;
        protected float YRadians;
        protected float ZRadians;

        // Target camera values
        public float? RotationXTargetRadians;
        public float? RotationYTargetRadians;
        public float? XTargetRadians;
        public float? YTargetRadians;
        public float? ZTargetRadians;
        public long EndTime;

        protected float axisZSpeedBoost = 1.0f;

        protected RotatingCamera(Viewer viewer)
            : base(viewer)
        {
        }

        protected RotatingCamera(Viewer viewer, Camera previousCamera)
            : base(viewer, previousCamera)
        {
            if (previousCamera != null)
            {
                float h, a, b;
                TOURMALINEMath.MatrixToAngles(previousCamera.XnaView, out h, out a, out b);
                RotationXRadians = -b;
                RotationYRadians = -h;
            }
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(RotationXRadians);
            outf.Write(RotationYRadians);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            RotationXRadians = inf.ReadSingle();
            RotationYRadians = inf.ReadSingle();
        }

        public override void Reset()
        {
            base.Reset();
            RotationXRadians = RotationYRadians = XRadians = YRadians = ZRadians = 0;
        }

        protected override Matrix GetCameraView()
        {
            var lookAtPosition = Vector3.UnitZ;
            lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationX(RotationXRadians));
            lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationY(RotationYRadians));
            lookAtPosition += cameraLocation;
            lookAtPosition.Z *= -1;
            return Matrix.CreateLookAt(XnaLocation(cameraLocation), lookAtPosition, Vector3.Up);
        }

        //protected static float GetMouseDelta(int mouseMovementPixels)
        //{
        //    // Ignore CameraMoveFast as that is too fast to be useful
        //    var delta = 0.01f;
        //    if (UserInput.IsDown(UserCommand.CameraMoveSlow))
        //        delta *= 0.1f;
        //    return delta * mouseMovementPixels;
        //}

        protected virtual void RotateByMouse()
        {
            //if (UserInput.IsMouseRightButtonDown)
            //{
            //    // Mouse movement doesn't use 'var speed' because the MouseMove 
            //    // parameters are already scaled down with increasing frame rates, 
            //    RotationXRadians += GetMouseDelta(UserInput.MouseMoveY);
            //    RotationYRadians += GetMouseDelta(UserInput.MouseMoveX);
            //}
            //// Support for replaying mouse movements
            //if (UserInput.IsMouseRightButtonPressed)
            //{
            //    Viewer.CheckReplaying();
            //    CommandStartTime = Viewer.Simulator.ClockTime;
            //}
            //if (UserInput.IsMouseRightButtonReleased)
            //{
            //    var commandEndTime = Viewer.Simulator.ClockTime;
            //    new CameraMouseRotateCommand(Viewer.Log, CommandStartTime, commandEndTime, RotationXRadians, RotationYRadians);
            //}
        }

        protected void UpdateRotation(long elapsedTime)
        {
            //var replayRemainingS = EndTime.Subtract(Viewer.microSim.gameTime.ElapsedGameTime).Millisecond;
            var replayRemainingS = (float)elapsedTime / 1000;
            if (replayRemainingS > 0)
            {
                var replayFraction = elapsedTime / replayRemainingS;
                if (RotationXTargetRadians != null && RotationYTargetRadians != null)
                {
                    var replayRemainingX = RotationXTargetRadians - RotationXRadians;
                    var replayRemainingY = RotationYTargetRadians - RotationYRadians;
                    var replaySpeedX = (float)(replayRemainingX * replayFraction);
                    var replaySpeedY = (float)(replayRemainingY * replayFraction);

                    if (IsCloseEnough(RotationXRadians, RotationXTargetRadians, replaySpeedX))
                    {
                        RotationXTargetRadians = null;
                    }
                    else
                    {
                        RotateDown(replaySpeedX);
                    }
                    if (IsCloseEnough(RotationYRadians, RotationYTargetRadians, replaySpeedY))
                    {
                        RotationYTargetRadians = null;
                    }
                    else
                    {
                        RotateRight(replaySpeedY);
                    }
                }
                else
                {
                    if (RotationXTargetRadians != null)
                    {
                        var replayRemainingX = RotationXTargetRadians - RotationXRadians;
                        var replaySpeedX = (float)(replayRemainingX * replayFraction);
                        if (IsCloseEnough(RotationXRadians, RotationXTargetRadians, replaySpeedX))
                        {
                            RotationXTargetRadians = null;
                        }
                        else
                        {
                            RotateDown(replaySpeedX);
                        }
                    }
                    if (RotationYTargetRadians != null)
                    {
                        var replayRemainingY = RotationYTargetRadians - RotationYRadians;
                        var replaySpeedY = (float)(replayRemainingY * replayFraction);
                        if (IsCloseEnough(RotationYRadians, RotationYTargetRadians, replaySpeedY))
                        {
                            RotationYTargetRadians = null;
                        }
                        else
                        {
                            RotateRight(replaySpeedY);
                        }
                    }
                }
            }
        }

        protected virtual void RotateDown(float speed)
        {
            RotationXRadians += speed;
            RotationXRadians = VerticalClamper.Clamp(RotationXRadians);
            MoveCamera();
        }

        protected virtual void RotateRight(float speed)
        {
            RotationYRadians += speed;
            MoveCamera();
        }

        protected void MoveCamera()
        {
            MoveCamera(new Vector3(0, 0, 0));
        }

        protected void MoveCamera(Vector3 movement)
        {
            movement = Vector3.Transform(movement, Matrix.CreateRotationX(RotationXRadians));
            movement = Vector3.Transform(movement, Matrix.CreateRotationY(RotationYRadians));
            cameraLocation += movement;
            cameraLocation.Normalize();
        }

        public override void HandleUserInput(long elapsedTime)
        {
            base.HandleUserInput(elapsedTime);
            // Rotate camera
            var speed = GetSpeed(elapsedTime);
            if (UserInput.IsDown(UserCommand.CameraRotateUp)) RotateDown(-speed);
            if (UserInput.IsDown(UserCommand.CameraRotateDown)) RotateDown(speed);
            if (UserInput.IsDown(UserCommand.CameraRotateLeft)) RotateRight(-speed);
            if (UserInput.IsDown(UserCommand.CameraRotateRight)) RotateRight(speed);

        }

        /// <summary>
        /// A margin of half a step (increment/2) is used to prevent hunting once the target is reached.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="target"></param>
        /// <param name="increment"></param>
        /// <returns></returns>
        protected static bool IsCloseEnough(float current, float? target, float increment)
        {
            Trace.Assert(target != null, "Camera target position must not be null");
            // If a pause interrupts a camera movement, then the increment will become zero.
            if (increment == 0)
            {  // To avoid divide by zero error, just kill the movement.
                return true;
            }
            else
            {
                var error = (float)target - current;
                return error / increment < 0.5;
            }
        }
    }


    public class SFMCamera : RotatingCamera
    {
        protected TrainCar mvarAttachedCar;
        public SFMCamera(Viewer visor):base(visor) {}
        public override TrainCar AttachedCar => mvarAttachedCar;
        protected Vector3 mvarAttachedLocation;
        protected float CameraAltitudeOffset;
        protected float CameraXOffset;
        protected float CameraZoomOffset;
        protected override void OnActivate(bool sameCamera)
        {
            if(null==mvarAttachedCar)
            {
                mvarAttachedCar = Viewer.microSim.PlayerTrain.FirstCar;
            }
        }
        public void UpdateLocation(WorldPosition position)
        {          
            cameraLocation = mvarAttachedLocation;
            cameraLocation.Z *= -1;
            cameraLocation = Vector3.Transform(cameraLocation, position.XNAMatrix);
            cameraLocation.Z *= -1;
        }
        protected override Matrix GetCameraView()
        {
            Vector3 lookAtPosition = Vector3.UnitZ;
            lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationX(RotationXRadians));
            lookAtPosition = Vector3.Transform(lookAtPosition,Matrix.CreateRotationY(RotationYRadians));
            lookAtPosition.X += mvarAttachedLocation.X;
            lookAtPosition.Y += mvarAttachedLocation.Y;
            lookAtPosition.Z += mvarAttachedLocation.Z;
            lookAtPosition.Z *= -1;
            Matrix upRotation = mvarAttachedCar.position.XNAMatrix;
            upRotation.Translation = Vector3.Zero;
            Vector3 up = Vector3.Transform(Vector3.Up, upRotation);
            return Matrix.CreateLookAt(XnaLocation(cameraLocation), lookAtPosition, up);
        }
        public override void Update(long elapsedTime)
        {
            if(null!=mvarAttachedCar)
            {
                cameraLocation.X = mvarAttachedLocation.X;
                cameraLocation.Y = mvarAttachedLocation.Y;
                cameraLocation.Z = mvarAttachedLocation.Z;
                cameraLocation.Z *= -1;
                cameraLocation = Vector3.Transform(cameraLocation, mvarAttachedCar.position.XNAMatrix);
                cameraLocation.Z *= -1;
            }
            UpdateRotation(elapsedTime);
        }
        public override void HandleUserInput(long elapsedTime)
        {
            RotationYRadians = -TOURMALINEMath.MatrixToYAngle(xnaView);
            float speed = GetSpeed(elapsedTime);
            if(UserInput.IsDown(UserCommand.CameraPanUp))
            {
                CameraAltitudeOffset += speed;
                mvarAttachedLocation.Y += speed;
            }
            if(UserInput.IsDown(UserCommand.CameraPanDown))
            {
                CameraAltitudeOffset -= speed;
                mvarAttachedLocation.Y -= speed;
                if(mvarAttachedLocation.Y < 0)
                        {
                    cameraLocation.Y -= CameraAltitudeOffset;
                    CameraAltitudeOffset = 0;
                }
            }
            if (UserInput.IsDown(UserCommand.CameraPanRight)) PanRight(speed);
            if (UserInput.IsDown(UserCommand.CameraPanLeft)) PanRight(-speed);
            if (UserInput.IsDown(UserCommand.CameraZoomIn)) ZoomIn(speed * 2);
            if (UserInput.IsDown(UserCommand.CameraZoomOut)) ZoomIn(-speed * 2);
            // Rotate camera
            if (UserInput.IsDown(UserCommand.CameraRotateUp)) RotateDown(-speed);
            if (UserInput.IsDown(UserCommand.CameraRotateDown)) RotateDown(speed);
            if (UserInput.IsDown(UserCommand.CameraRotateLeft)) RotateRight(-speed);
            if (UserInput.IsDown(UserCommand.CameraRotateRight)) RotateRight(speed);
        }
        protected virtual void PanRight(float speed)
        {

            CameraXOffset += speed;
            mvarAttachedLocation.X += speed;
        }
        protected override void ZoomIn(float speed)
        {
            CameraZoomOffset += speed;
            mvarAttachedLocation.Z += speed;
        }


    }

    public class TracksideCamera : LookAtCamera
    {
        protected const int MaximumDistance = 100;
        protected const float SidewaysScale = MaximumDistance / 10;
        // Heights above the terrain for the camera.
        protected const float CameraAltitude = 2;
        // Height above the coordinate center of target.
        protected const float TargetAltitude = TerrainAltitudeMargin;

        protected TrainCar attachedCar;
        public override TrainCar AttachedCar { get { return attachedCar; } }
        public override string Name { get => "Trackside"; }

        protected TrainCar LastCheckCar;
        protected Vector3 TrackCameraLocation;
        protected float CameraAltitudeOffset;

        public override bool IsUnderground
        {
            get
            {
                // Camera is underground if target (base) is underground or
                // track location is underground. The latter means we switch
                // to cab view instead of putting the camera above the tunnel.
                if (base.IsUnderground)
                    return true;
                //if (TrackCameraLocation == WorldLocation.None) return false;
                //var elevationAtCameraTarget = Viewer.Tiles.GetElevation(TrackCameraLocation);
                //return TrackCameraLocation.Location.Y + TerrainAltitudeMargin < elevationAtCameraTarget;
                return false;
            }
        }


        public override void HandleUserInput(long elapsedTime)
        {
            RotationYRadians = -TOURMALINEMath.MatrixToYAngle(XnaView);
            var speed = GetSpeed(elapsedTime);

            if (UserInput.IsDown(UserCommand.CameraPanUp))
            {
                CameraAltitudeOffset += speed;
                cameraLocation.Y += speed;
            }
            if (UserInput.IsDown(UserCommand.CameraPanDown))
            {
                CameraAltitudeOffset -= speed;
                cameraLocation.Y -= speed;
                if (CameraAltitudeOffset < 0)
                {
                    cameraLocation.Y -= CameraAltitudeOffset;
                    CameraAltitudeOffset = 0;
                }
            }
            if (UserInput.IsDown(UserCommand.CameraPanRight)) PanRight(speed);
            if (UserInput.IsDown(UserCommand.CameraPanLeft)) PanRight(-speed);
            if (UserInput.IsDown(UserCommand.CameraZoomIn)) ZoomIn(speed * 2);
            if (UserInput.IsDown(UserCommand.CameraZoomOut)) ZoomIn(-speed * 2);

            ZoomByMouseWheel(speed);

            //var trainCars = Viewer.SelectedTrain.Cars;
            //if (UserInput.IsPressed(UserCommand.CameraCarNext))
            //    attachedCar = attachedCar == trainCars.First() ? attachedCar : trainCars[trainCars.IndexOf(attachedCar) - 1];
            //else if (UserInput.IsPressed(UserCommand.CameraCarPrevious))
            //    attachedCar = attachedCar == trainCars.Last() ? attachedCar : trainCars[trainCars.IndexOf(attachedCar) + 1];
            //else if (UserInput.IsPressed(UserCommand.CameraCarFirst))
            //    attachedCar = trainCars.First();
            //else if (UserInput.IsPressed(UserCommand.CameraCarLast))
            //    attachedCar = trainCars.Last();
        }

        public TracksideCamera(Viewer viewer)
            : base(viewer)
        {
        }
        public TracksideCamera(Viewer viewer, TrainCar attachedCar)
            : base(viewer)
        {
            this.attachedCar = attachedCar;
        }

        public override void Reset()
        {
            base.Reset();
            cameraLocation.Y -= CameraAltitudeOffset;
            CameraAltitudeOffset = 0;
        }

        protected override void OnActivate(bool sameCamera)
        {
            if (sameCamera)
            {
                //cameraLocation.TileX = 0;
                //cameraLocation.TileZ = 0;
            }
            //if (attachedCar == null || attachedCar.Train != Viewer.SelectedTrain)            
            //    {
            //    if (Viewer.SelectedTrain.MUDirection != Direction.Reverse)
            //        attachedCar = Viewer.SelectedTrain.Cars.First();
            //    else
            //        attachedCar = Viewer.SelectedTrain.Cars.Last();
            //}            
            base.OnActivate(sameCamera);
        }



        public override void Update(long elapsedTime)
        {
            bool trainForwards;
            //var train = PrepUpdate(out trainForwards);

            // Train is close enough if the last car we used is part of the same train and still close enough.
            //var trainClose = (LastCheckCar != null) && (LastCheckCar.Train == train) && (WorldLocation.GetDistance2D(LastCheckCar.WorldPosition.WorldLocation, cameraLocation).Length() < MaximumDistance);

            // Otherwise, let's check out every car and remember which is the first one close enough for next time.
            //if (!trainClose)
            //{
            //foreach (var car in train.Cars)
            //{
            //    if (WorldLocation.GetDistance2D(car.WorldPosition.WorldLocation, cameraLocation).Length() < MaximumDistance)
            //    {
            //        LastCheckCar = car;
            //        trainClose = true;
            //        break;
            //    }
            //}
            //}

            // Switch to new position.
            //if (!trainClose || (TrackCameraLocation == WorldLocation.None))
            if (TrackCameraLocation == Vector3.One)
            {
                //var tdb = trainForwards ? new Traveller(train.FrontTDBTraveller) : new Traveller(train.RearTDBTraveller, Traveller.TravellerDirection.Backward);
                //var newLocation = GoToNewLocation(ref tdb, train, trainForwards);
                //newLocation.Normalize();

                //var newLocationElevation = Viewer.Tiles.GetElevation(newLocation);
                //cameraLocation = newLocation;
                //cameraLocation.Location.Y = Math.Max(tdb.Y, newLocationElevation) + CameraAltitude + CameraAltitudeOffset;
            }

            targetLocation.Y += TargetAltitude;
            //UpdateListener();
        }

        //protected Train PrepUpdate(out bool trainForwards)
        //{
        //    var train = attachedCar.Train;

        //    // TODO: What is this code trying to do?
        //    //if (train != Viewer.PlayerTrain && train.LeadLocomotive == null) train.ChangeToNextCab();
        //    trainForwards = true;
        //    if (train.LeadLocomotive != null)
        //        //TODO: next code line has been modified to flip trainset physics in order to get viewing direction coincident with loco direction when using rear cab.
        //        // To achieve the same result with other means, without flipping trainset physics, maybe the line should be changed
        //        trainForwards = (train.LeadLocomotive.SpeedMpS >= 0) ^ train.LeadLocomotive.Flipped ^ ((MSTSLocomotive)train.LeadLocomotive).UsingRearCab;
        //    else if (Viewer.PlayerLocomotive != null && train.IsActualPlayerTrain)
        //        trainForwards = (Viewer.PlayerLocomotive.SpeedMpS >= 0) ^ Viewer.PlayerLocomotive.Flipped ^ ((MSTSLocomotive)Viewer.PlayerLocomotive).UsingRearCab;

        //    targetLocation = attachedCar.WorldPosition.WorldLocation;

        //    return train;
        //}


        //protected WorldLocation GoToNewLocation(ref Traveller tdb, Train train, bool trainForwards)
        //{
        //    tdb.Move(MaximumDistance * 0.75f);
        //    var newLocation = tdb.WorldLocation;
        //    TrackCameraLocation = new WorldLocation(newLocation);
        //    var directionForward = WorldLocation.GetDistance((trainForwards ? train.FirstCar : train.LastCar).WorldPosition.WorldLocation, newLocation);
        //    if (Viewer.Random.Next(2) == 0)
        //    {
        //        newLocation.Location.X += -directionForward.Z / SidewaysScale; // Use swapped -X and Z to move to the left of the track.
        //        newLocation.Location.Z += directionForward.X / SidewaysScale;
        //    }
        //    else
        //    {
        //        newLocation.Location.X += directionForward.Z / SidewaysScale; // Use swapped X and -Z to move to the right of the track.
        //        newLocation.Location.Z += -directionForward.X / SidewaysScale;
        //    }
        //    return newLocation;
        //}

        protected virtual void PanRight(float speed)
        {
            var movement = new Vector3(0, 0, 0);
            movement.X += speed;
            XRadians += movement.X;
            MoveCamera(movement);
        }

        protected override void ZoomIn(float speed)
        {
            var movement = new Vector3(0, 0, 0);
            movement.Z += speed;
            ZRadians += movement.Z;
            MoveCamera(movement);
        }

    }



    public abstract class AttachedCamera : RotatingCamera
    {
        protected TrainCar mvarAttachedCar;
        public override TrainCar AttachedCar => mvarAttachedCar;
        public bool tiltingLand;
        protected Vector3 mvarAttachedLocation;
        protected WorldPosition LookedAtPosition = new WorldPosition();

        protected AttachedCamera(Viewer viewer) : base(viewer) { }
        protected override void OnActivate(bool sameCamera)
        {
            if (null == AttachedCar)
            {
                SetCameraCar(Viewer.microSim.PlayerTrain.FirstCar);
            }
        }
        protected void SetCameraCar(TrainCar rhs) { mvarAttachedCar = rhs; }
        protected virtual bool IsCameraFlipped() { return false; }
        public void UpdateLocation(WorldPosition position)
        {
            if (null != position)
            {
                //cameraLocation.TileX = position.TileX;
                //cameraLocation.TileZ = position.TileZ;
                cameraLocation.Y = mvarAttachedLocation.Y;
                if (IsCameraFlipped())
                {
                    cameraLocation.X = -mvarAttachedLocation.X;
                    cameraLocation.Z = -mvarAttachedLocation.Z;
                }
                else
                {
                    cameraLocation.X = mvarAttachedLocation.X;
                    cameraLocation.Z = mvarAttachedLocation.Z;
                }
                cameraLocation.Z *= -1;
                cameraLocation = Vector3.Transform(cameraLocation, position.XNAMatrix);
                cameraLocation.Z *= -1;
            }
        }
        protected override Matrix GetCameraView()
        {
            bool flipped = IsCameraFlipped();
            Vector3 lookAtPosition = Vector3.UnitZ;
            lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationX(RotationXRadians));
            lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationY(RotationYRadians + (flipped ? MathHelper.Pi : 0)));
            if (flipped)
            {
                lookAtPosition.X -= mvarAttachedLocation.X;
                lookAtPosition.Y += mvarAttachedLocation.Y;
                lookAtPosition.Z -= mvarAttachedLocation.Z;
            }
            else
            {
                lookAtPosition.X += mvarAttachedLocation.X;
                lookAtPosition.Y += mvarAttachedLocation.Y;
                lookAtPosition.Z += mvarAttachedLocation.Z;
            }
            lookAtPosition.Z *= -1;
            lookAtPosition = Vector3.Transform(lookAtPosition, Viewer.Camera is TrackingCamera ? LookedAtPosition.XNAMatrix : mvarAttachedCar.position.XNAMatrix);
            //Rotamos el vector vertical para que la cámara ruede con nosotros
            Vector3 up;
            if (Viewer.Camera is TrackingCamera)
            {
                up = Vector3.Up;
            }
            else
            {
                Matrix upRotation = mvarAttachedCar.position.XNAMatrix;
                upRotation.Translation = Vector3.Zero;
                up = Vector3.Transform(Vector3.Up, upRotation);
            }
            return Matrix.CreateLookAt(XnaLocation(cameraLocation), lookAtPosition, up);
        }
        public override void Update(long elapsedTime)
        {
            if (null != mvarAttachedCar)
            {
                //cameraLocation.TileX = mvarAttachedCar.position.TileX;
                //cameraLocation.TileZ = mvarAttachedCar.position.TileZ;
                cameraLocation.Y = mvarAttachedLocation.Y;
                if (IsCameraFlipped())
                {
                    cameraLocation.X = -mvarAttachedLocation.X;
                    cameraLocation.Z = -mvarAttachedLocation.Z;
                }
                else
                {
                    cameraLocation.X = mvarAttachedLocation.X;
                    cameraLocation.Z = mvarAttachedLocation.Z;
                }
                cameraLocation.Z *= -1;
                cameraLocation = Vector3.Transform(cameraLocation, mvarAttachedCar.position.XNAMatrix);
                cameraLocation.Z *= -1;
            }
            UpdateRotation(elapsedTime);
        }

        protected virtual List<TrainCar> GetCameraCars()
        {
            return Viewer.microSim.PlayerTrain.Cars;
        }
        public virtual void NextCar()
        {
            List<TrainCar> coches = GetCameraCars();
            SetCameraCar(mvarAttachedCar == coches.First() ? mvarAttachedCar : coches[coches.IndexOf(mvarAttachedCar) - 1]);
        }
        public virtual void PreviousCar()
        {
            List<TrainCar> coches = GetCameraCars();
            SetCameraCar(mvarAttachedCar == coches.Last() ? mvarAttachedCar : coches[coches.IndexOf(mvarAttachedCar) + 1]);
        }
        public virtual void FirstCar()
        {
            List<TrainCar> coches = GetCameraCars();
            SetCameraCar(coches.First());
        }
        public virtual void LastCar()
        {
            List<TrainCar> coches = GetCameraCars();
            SetCameraCar(coches.Last());
        }

    }

    public class FreeRoamCamera : RotatingCamera
    {
        const float maxCameraHeight = 1000f;
        const float ZoomFactor = 2f;

        public override string Name { get => "Free"; }

        public FreeRoamCamera(Viewer viewer, Camera previousCamera)
            : base(viewer, previousCamera)
        {
        }

        public void SetLocation(Vector3 location)
        {
            cameraLocation = location;
        }

        public override void Reset()
        {
            // Intentionally do nothing at all.
        }

        public override void HandleUserInput(long elapsedTime)
        {
            if (UserInput.IsDown(UserCommand.CameraZoomIn) || UserInput.IsDown(UserCommand.CameraZoomOut))
            {
                var elevation = Viewer.Elevation;
                if (cameraLocation.Y < elevation)
                    axisZSpeedBoost = 1;
                else
                {
                    cameraLocation.Y = MathHelper.Min(cameraLocation.Y, elevation + maxCameraHeight);
                    float cameraRelativeHeight = cameraLocation.Y - elevation;
                    axisZSpeedBoost = ((cameraRelativeHeight / maxCameraHeight) * 50) + 1;
                }
            }

            var speed = GetSpeed(elapsedTime);

            // Pan and zoom camera
            if (UserInput.IsDown(UserCommand.CameraPanRight)) PanRight(speed);
            if (UserInput.IsDown(UserCommand.CameraPanLeft)) PanRight(-speed);
            if (UserInput.IsDown(UserCommand.CameraPanUp)) PanUp(speed);
            if (UserInput.IsDown(UserCommand.CameraPanDown)) PanUp(-speed);
            if (UserInput.IsDown(UserCommand.CameraZoomIn)) ZoomIn(speed * ZoomFactor);
            if (UserInput.IsDown(UserCommand.CameraZoomOut)) ZoomIn(-speed * ZoomFactor);
            ZoomByMouseWheel(speed);

            if (UserInput.IsPressed(UserCommand.CameraPanRight) || UserInput.IsPressed(UserCommand.CameraPanLeft))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.microSim.ClockTime.Ticks/10000;
            }
            if (UserInput.IsReleased(UserCommand.CameraPanRight) || UserInput.IsReleased(UserCommand.CameraPanLeft))
                new CameraXCommand(Viewer.Log, CommandStartTime, Viewer.microSim.ClockTime.Ticks / 10000, XRadians);

            if (UserInput.IsPressed(UserCommand.CameraPanUp) || UserInput.IsPressed(UserCommand.CameraPanDown))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.microSim.ClockTime.Ticks/10000;
            }
            if (UserInput.IsReleased(UserCommand.CameraPanUp) || UserInput.IsReleased(UserCommand.CameraPanDown))
                new CameraYCommand(Viewer.Log, CommandStartTime, Viewer.microSim.ClockTime.Ticks/10000, YRadians);

            if (UserInput.IsPressed(UserCommand.CameraZoomIn) || UserInput.IsPressed(UserCommand.CameraZoomOut))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.microSim.ClockTime.Ticks / 10000;
            }
            if (UserInput.IsReleased(UserCommand.CameraZoomIn) || UserInput.IsReleased(UserCommand.CameraZoomOut))
                new CameraZCommand(Viewer.Log, CommandStartTime, Viewer.microSim.ClockTime.Ticks/10000, ZRadians);

            speed *= SpeedAdjustmentForRotation;
            RotateByMouse();

            // Rotate camera
            if (UserInput.IsDown(UserCommand.CameraRotateUp)) RotateDown(-speed);
            if (UserInput.IsDown(UserCommand.CameraRotateDown)) RotateDown(speed);
            if (UserInput.IsDown(UserCommand.CameraRotateLeft)) RotateRight(-speed);
            if (UserInput.IsDown(UserCommand.CameraRotateRight)) RotateRight(speed);

            // Support for replaying camera rotation movements
            if (UserInput.IsPressed(UserCommand.CameraRotateUp) || UserInput.IsPressed(UserCommand.CameraRotateDown))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.microSim.ClockTime.Ticks / 10000;
            }
            if (UserInput.IsReleased(UserCommand.CameraRotateUp) || UserInput.IsReleased(UserCommand.CameraRotateDown))
                new CameraRotateUpDownCommand(Viewer.Log, CommandStartTime, Viewer.microSim.ClockTime.Ticks/10000, RotationXRadians);

            if (UserInput.IsPressed(UserCommand.CameraRotateLeft) || UserInput.IsPressed(UserCommand.CameraRotateRight))
            {
                Viewer.CheckReplaying();
                CommandStartTime = Viewer.microSim.ClockTime.Ticks / 10000;
            }
            if (UserInput.IsReleased(UserCommand.CameraRotateLeft) || UserInput.IsReleased(UserCommand.CameraRotateRight))
                new CameraRotateLeftRightCommand(Viewer.Log, CommandStartTime, Viewer.microSim.ClockTime.Ticks / 10000, RotationYRadians);
        }

        public override void Update(long elapsedTime)
        {
            UpdateRotation(elapsedTime);

            var replayRemainingS = EndTime - elapsedTime;
            if (replayRemainingS > 0)
            {
                var replayFraction = elapsedTime / replayRemainingS;
                // Panning
                if (XTargetRadians != null)
                {
                    var replayRemainingX = XTargetRadians - XRadians;
                    var replaySpeedX = Math.Abs((float)(replayRemainingX * replayFraction));
                    if (IsCloseEnough(XRadians, XTargetRadians, replaySpeedX))
                    {
                        XTargetRadians = null;
                    }
                    else
                    {
                        PanRight(replaySpeedX);
                    }
                }
                if (YTargetRadians != null)
                {
                    var replayRemainingY = YTargetRadians - YRadians;
                    var replaySpeedY = Math.Abs((float)(replayRemainingY * replayFraction));
                    if (IsCloseEnough(YRadians, YTargetRadians, replaySpeedY))
                    {
                        YTargetRadians = null;
                    }
                    else
                    {
                        PanUp(replaySpeedY);
                    }
                }
                // Zooming
                if (ZTargetRadians != null)
                {
                    var replayRemainingZ = ZTargetRadians - ZRadians;
                    var replaySpeedZ = Math.Abs((float)(replayRemainingZ * replayFraction));
                    if (IsCloseEnough(ZRadians, ZTargetRadians, replaySpeedZ))
                    {
                        ZTargetRadians = null;
                    }
                    else
                    {
                        ZoomIn(replaySpeedZ);
                    }
                }
            }
            //UpdateListener();
        }

        protected virtual void PanRight(float speed)
        {
            var movement = new Vector3(0, 0, 0);
            movement.X += speed;
            XRadians += movement.X;
            MoveCamera(movement);
        }

        protected virtual void PanUp(float speed)
        {
            var movement = new Vector3(0, 0, 0);
            movement.Y += speed;
            movement.Y = VerticalClamper.Clamp(movement.Y);    // Only the vertical needs to be clamped
            YRadians += movement.Y;
            MoveCamera(movement);
        }

        protected override void ZoomIn(float speed)
        {
            var movement = new Vector3(0, 0, 0);
            movement.Z += speed;
            ZRadians += movement.Z;
            MoveCamera(movement);
        }
    }


    public class SpecialTracksideCamera : TracksideCamera
    {
        const int MaximumSpecialPointDistance = 300;
        const float PlatformOffsetM = 3.3f;
        protected bool SpecialPointFound = false;
        const float CheckIntervalM = 50f; // every 50 meters it is checked wheter there is a near special point
        protected float DistanceRunM = 0f; // distance run since last check interval
        protected bool FirstUpdateLoop = true; // first update loop

        const float MaxDistFromRoadCarM = 100.0f; // maximum distance of train traveller to spawned roadcar
        //protected RoadCar NearRoadCar;
        protected bool RoadCarFound;

        public SpecialTracksideCamera(Viewer viewer)
            : base(viewer)
        {
        }

        protected override void OnActivate(bool sameCamera)
        {
            DistanceRunM = 0;
            base.OnActivate(sameCamera);
            //FirstUpdateLoop = Math.Abs(AttachedCar.Train.SpeedMpS) <= 0.2f || sameCamera;
            if (sameCamera)
            {
                SpecialPointFound = false;
                TrackCameraLocation = Vector3.Zero;//  WorldLocation.None;
                RoadCarFound = false;
                //NearRoadCar = null;
            }
        }

        public override void Update(long elapsedTime)
        {
            bool trainForwards;
            //var train = PrepUpdate(out trainForwards);

            if (RoadCarFound)
            {
                // camera location is always behind the near road car, at a distance which increases at increased speed
                //if (NearRoadCar != null && NearRoadCar.Travelled < NearRoadCar.Spawner.Length - 10f)
                //{
                //    var traveller = new Traveller(NearRoadCar.FrontTraveller);
                //    traveller.Move(-2.5f - 0.15f * NearRoadCar.Length - NearRoadCar.Speed * 0.5f);
                //    cameraLocation = TrackCameraLocation = new WorldLocation(traveller.WorldLocation);
                //    cameraLocation.Location.Y += 1.8f;
                //}
                //else NearRoadCar = null;
            }

            // Train is close enough if the last car we used is part of the same train and still close enough.
            //var trainClose = (LastCheckCar != null) && (LastCheckCar.Train == train) && (WorldLocation.GetDistance2D(LastCheckCar.WorldPosition.WorldLocation, cameraLocation).Length() < (SpecialPointFound ? MaximumSpecialPointDistance * 0.8f : MaximumDistance));

            // Otherwise, let's check out every car and remember which is the first one close enough for next time.
            //if (!trainClose)
            //{
                // if camera is not close to LastCheckCar, verify if it is still close to another car of the train
                //foreach (var car in train.Cars)
                //{
                //    if (LastCheckCar != null && car == LastCheckCar &&
                //        WorldLocation.GetDistance2D(car.WorldPosition.WorldLocation, cameraLocation).Length() < (SpecialPointFound ? MaximumSpecialPointDistance * 0.8f : MaximumDistance))
                //    {
                //        trainClose = true;
                //        break;
                //    }
                //    else if (WorldLocation.GetDistance2D(car.WorldPosition.WorldLocation, cameraLocation).Length() <
                //        (SpecialPointFound && NearRoadCar != null && train.SpeedMpS > NearRoadCar.Speed + 10 ? MaximumSpecialPointDistance * 0.8f : MaximumDistance))
                //    {
                //        LastCheckCar = car;
                //        trainClose = true;
                //        break;
                //    }
                //}
                //if (!trainClose)
                //    LastCheckCar = null;
            //}
            //if (RoadCarFound && NearRoadCar == null)
            //{
            //    RoadCarFound = false;
            //    SpecialPointFound = false;
            //    trainClose = false;
            //}
            var trySpecial = false;
            //DistanceRunM += elapsedTime.ClockSeconds * train.SpeedMpS;
            // when camera not at a special point, try every CheckIntervalM meters if there is a new special point nearby
            if (Math.Abs(DistanceRunM) >= CheckIntervalM)
            {
                DistanceRunM = 0;
                //if (!SpecialPointFound && trainClose) trySpecial = true;
            }
            // Switch to new position.
            //if (!trainClose || (TrackCameraLocation == WorldLocation.None) || trySpecial)
            //{
            //    SpecialPointFound = false;
            //    bool platformFound = false;
            //    NearRoadCar = null;
            //    RoadCarFound = false;
            //    Traveller tdb;
            //    // At first update loop camera location may be also behind train front (e.g. platform at start of activity)
            //    if (FirstUpdateLoop)
            //        tdb = trainForwards ? new Traveller(train.RearTDBTraveller) : new Traveller(train.FrontTDBTraveller, Traveller.TravellerDirection.Backward);
            //    else
            //        tdb = trainForwards ? new Traveller(train.FrontTDBTraveller) : new Traveller(train.RearTDBTraveller, Traveller.TravellerDirection.Backward);
            //    var newLocation = WorldLocation.None;

            //    int tcSectionIndex;
            //    int routeIndex;
            //    Train.TCSubpathRoute thisRoute = null;
            //    // search for near platform in fast way, using TCSection data
            //    if (trainForwards && train.ValidRoute[0] != null)
            //    {
            //        thisRoute = train.ValidRoute[0];
            //    }
            //    else if (!trainForwards && train.ValidRoute[1] != null)
            //    {
            //        thisRoute = train.ValidRoute[1];
            //    }

            //    // Search for platform
            //    if (thisRoute != null)
            //    {
            //        if (FirstUpdateLoop)
            //        {
            //            tcSectionIndex = trainForwards ? train.PresentPosition[1].TCSectionIndex : train.PresentPosition[0].TCSectionIndex;
            //            routeIndex = trainForwards ? train.PresentPosition[1].RouteListIndex : train.PresentPosition[0].RouteListIndex;
            //        }
            //        else
            //        {
            //            tcSectionIndex = trainForwards ? train.PresentPosition[0].TCSectionIndex : train.PresentPosition[1].TCSectionIndex;
            //            routeIndex = trainForwards ? train.PresentPosition[0].RouteListIndex : train.PresentPosition[1].RouteListIndex;
            //        }
            //        if (routeIndex != -1)
            //        {
            //            float distanceToViewingPoint = 0;
            //            TrackCircuitSection TCSection = train.signalRef.TrackCircuitList[tcSectionIndex];
            //            float distanceToAdd = TCSection.Length;
            //            float incrDistance;
            //            if (FirstUpdateLoop)
            //                incrDistance = trainForwards ? -train.PresentPosition[1].TCOffset : -TCSection.Length + train.PresentPosition[0].TCOffset;
            //            else
            //                incrDistance = trainForwards ? -train.PresentPosition[0].TCOffset : -TCSection.Length + train.PresentPosition[1].TCOffset;
            //            // scanning route in direction of train, searching for a platform
            //            while (incrDistance < MaximumSpecialPointDistance * 0.7f)
            //            {
            //                foreach (int platformIndex in TCSection.PlatformIndex)
            //                {
            //                    PlatformDetails thisPlatform = train.signalRef.PlatformDetailsList[platformIndex];
            //                    if (thisPlatform.TCOffset[0, thisRoute[routeIndex].Direction] + incrDistance < MaximumSpecialPointDistance * 0.7f
            //                        && (thisPlatform.TCOffset[0, thisRoute[routeIndex].Direction] + incrDistance > 0 || FirstUpdateLoop))
            //                    {
            //                        // platform found, compute distance to viewing point
            //                        distanceToViewingPoint = Math.Min(MaximumSpecialPointDistance * 0.7f,
            //                            incrDistance + thisPlatform.TCOffset[0, thisRoute[routeIndex].Direction] + thisPlatform.Length * 0.7f);
            //                        if (FirstUpdateLoop && Math.Abs(train.SpeedMpS) <= 0.2f) distanceToViewingPoint =
            //                                Math.Min(distanceToViewingPoint, train.Length * 0.95f);
            //                        tdb.Move(distanceToViewingPoint);
            //                        newLocation = tdb.WorldLocation;
            //                        // shortTrav is used to state directions, to correctly identify in which direction (left or right) to move
            //                        //the camera from center of track to the platform at its side
            //                        Traveller shortTrav;
            //                        PlatformItem platformItem = Viewer.Simulator.TDB.TrackDB.TrItemTable[thisPlatform.PlatformFrontUiD] as PlatformItem;
            //                        if (platformItem == null) continue;
            //                        shortTrav = new Traveller(Viewer.Simulator.TSectionDat, Viewer.Simulator.TDB.TrackDB.TrackNodes, platformItem.TileX,
            //                            platformItem.TileZ, platformItem.X, platformItem.Z, Traveller.TravellerDirection.Forward);
            //                        var distanceToViewingPoint1 = shortTrav.DistanceTo(newLocation.TileX, newLocation.TileZ,
            //                            newLocation.Location.X, newLocation.Location.Y, newLocation.Location.Z, thisPlatform.Length);
            //                        if (distanceToViewingPoint1 == -1) //try other direction
            //                        {
            //                            shortTrav.ReverseDirection();
            //                            distanceToViewingPoint1 = shortTrav.DistanceTo(newLocation.TileX, newLocation.TileZ,
            //                            newLocation.Location.X, newLocation.Location.Y, newLocation.Location.Z, thisPlatform.Length);
            //                            if (distanceToViewingPoint1 == -1) continue;
            //                        }
            //                        platformFound = true;
            //                        SpecialPointFound = true;
            //                        trainClose = false;
            //                        LastCheckCar = FirstUpdateLoop ^ trainForwards ? train.Cars.First() : train.Cars.Last();
            //                        shortTrav.Move(distanceToViewingPoint1);
            //                        // moving newLocation to platform at side of track
            //                        newLocation.Location.X += (PlatformOffsetM + Viewer.Simulator.SuperElevationGauge / 2) * (float)Math.Cos(shortTrav.RotY) *
            //                            (thisPlatform.PlatformSide[1] ? 1 : -1);
            //                        newLocation.Location.Z += -(PlatformOffsetM + Viewer.Simulator.SuperElevationGauge / 2) * (float)Math.Sin(shortTrav.RotY) *
            //                            (thisPlatform.PlatformSide[1] ? 1 : -1);
            //                        TrackCameraLocation = new WorldLocation(newLocation);
            //                        break;
            //                    }
            //                }
            //                if (platformFound) break;
            //                if (routeIndex < thisRoute.Count - 1)
            //                {
            //                    incrDistance += distanceToAdd;
            //                    routeIndex++;
            //                    TCSection = train.signalRef.TrackCircuitList[thisRoute[routeIndex].TCSectionIndex];
            //                    distanceToAdd = TCSection.Length;
            //                }
            //                else break;
            //            }
            //        }
            //    }

                //if (!SpecialPointFound)
                //{

                //    // Search for near visible spawned car
                //    var minDistanceM = 10000.0f;
                //    NearRoadCar = null;
                //    foreach (RoadCar visibleCar in Viewer.World.RoadCars.VisibleCars)
                //    {
                //        // check for direction
                //        if (Math.Abs(visibleCar.FrontTraveller.RotY - train.FrontTDBTraveller.RotY) < 0.5f)
                //        {
                //            var traveller = visibleCar.Speed > Math.Abs(train.SpeedMpS) ^ trainForwards ?
                //                train.RearTDBTraveller : train.FrontTDBTraveller;
                //            var distanceTo = WorldLocation.GetDistance2D(visibleCar.FrontTraveller.WorldLocation, traveller.WorldLocation).Length();
                //            if (distanceTo < MaxDistFromRoadCarM && Math.Abs(visibleCar.FrontTraveller.WorldLocation.Location.Y - traveller.WorldLocation.Location.Y) < 30.0f)
                //            {
                //                if (visibleCar.Travelled < visibleCar.Spawner.Length - 30)
                //                {
                //                    minDistanceM = distanceTo;
                //                    NearRoadCar = visibleCar;
                //                    break;
                //                }
                //            }
                //        }
                //    }
                //    if (NearRoadCar != null)
                //    // readcar found
                //    {
                //        SpecialPointFound = true;
                //        RoadCarFound = true;
                //        // CarriesCamera needed to increase distance of following car
                //        NearRoadCar.CarriesCamera = true;
                //        var traveller = new Traveller(NearRoadCar.FrontTraveller);
                //        traveller.Move(-2.5f - 0.15f * NearRoadCar.Length);
                //        TrackCameraLocation = newLocation = new WorldLocation(traveller.WorldLocation);
                //        LastCheckCar = trainForwards ? train.Cars.First() : train.Cars.Last();
                //    }
                //}

                //if (!SpecialPointFound)
                //{
                //    // try to find near level crossing then
                //    LevelCrossingItem newLevelCrossingItem = LevelCrossingItem.None;
                //    float FrontDist = -1;
                //    newLevelCrossingItem = Viewer.Simulator.LevelCrossings.SearchNearLevelCrossing(train, MaximumSpecialPointDistance * 0.7f, trainForwards, out FrontDist);
                //    if (newLevelCrossingItem != LevelCrossingItem.None)
                //    {
                //        SpecialPointFound = true;
                //        trainClose = false;
                //        LastCheckCar = trainForwards ? train.Cars.First() : train.Cars.Last();
                //        newLocation = newLevelCrossingItem.Location;
                //        TrackCameraLocation = new WorldLocation(newLocation);
                //        Traveller roadTraveller;
                //        // decide randomly at which side of the level crossing the camera will be located
                //        if (Viewer.Random.Next(2) == 0)
                //        {
                //            roadTraveller = new Traveller(Viewer.Simulator.TSectionDat, Viewer.Simulator.RDB.RoadTrackDB.TrackNodes, Viewer.Simulator.RDB.RoadTrackDB.TrackNodes[newLevelCrossingItem.TrackIndex],
                //                newLocation.TileX, newLocation.TileZ, newLocation.Location.X, newLocation.Location.Z, Traveller.TravellerDirection.Forward);
                //        }
                //        else
                //        {
                //            roadTraveller = new Traveller(Viewer.Simulator.TSectionDat, Viewer.Simulator.RDB.RoadTrackDB.TrackNodes, Viewer.Simulator.RDB.RoadTrackDB.TrackNodes[newLevelCrossingItem.TrackIndex],
                //                newLocation.TileX, newLocation.TileZ, newLocation.Location.X, newLocation.Location.Z, Traveller.TravellerDirection.Backward);
                //        }
                //        roadTraveller.Move(12.5f);
                //        tdb.Move(FrontDist);
                //        newLocation = roadTraveller.WorldLocation;
                //    }
                //}
                //if (!SpecialPointFound && !trainClose)
                //{
                //    tdb = trainForwards ? new Traveller(train.FrontTDBTraveller) : new Traveller(train.RearTDBTraveller, Traveller.TravellerDirection.Backward); // return to standard
                //    newLocation = GoToNewLocation(ref tdb, train, trainForwards);
                //}

                //if (newLocation != WorldLocation.None && !trainClose)
                //{
                //    newLocation.Normalize();
                //    cameraLocation = newLocation;
                //    if (!RoadCarFound)
                //    {
                //        var newLocationElevation = Viewer.Tiles.GetElevation(newLocation);
                //        cameraLocation.Location.Y = newLocationElevation;
                //        TrackCameraLocation = new WorldLocation(cameraLocation);
                //        cameraLocation.Location.Y = Math.Max(tdb.Y, newLocationElevation) + CameraAltitude + CameraAltitudeOffset + (platformFound ? 0.35f : 0.0f);
                //    }
                //    else
                //    {
                //        TrackCameraLocation = new WorldLocation(cameraLocation);
                //        cameraLocation.Location.Y += 1.8f;
                //    }
                //    DistanceRunM = 0f;
                //}
            //}

            //targetLocation.Location.Y += TargetAltitude;
            //FirstUpdateLoop = false;
            //UpdateListener();
        }

        protected override void ZoomIn(float speed)
        {
            if (!RoadCarFound)
            {
                var movement = new Vector3(0, 0, 0);
                movement.Z += speed;
                ZRadians += movement.Z;
                MoveCamera(movement);
            }
            else
            {
                //NearRoadCar.ChangeSpeed(speed);
            }
        }
    }




    public class TrackingCamera : AttachedCamera
    {
        const float StartPositionDistance = 20;
        const float StartPositionXRadians = 0.399f;
        const float StartPositionYRadians = 0.387f;

        protected readonly bool Front;
        public enum AttachedTo { Front, Rear }
        const float ZoomFactor = 0.1f;

        protected float PositionDistance = StartPositionDistance;
        protected float PositionXRadians = StartPositionXRadians;
        protected float PositionYRadians = StartPositionYRadians;
        public float? PositionDistanceTargetMetres;
        public float? PositionXTargetRadians;
        public float? PositionYTargetRadians;

        protected bool BrowseBackwards;
        protected bool BrowseForwards;
        const float BrowseSpeedMpS = 4;
        protected float ZDistanceM; // used to browse train;
        protected Traveller browsedTraveller;
        protected float BrowseDistance = 20;
        public bool BrowseMode = false;
        protected float LowWagonOffsetLimit;
        protected float HighWagonOffsetLimit;

        public override string Name { get { return Front ? "Outside Front" :"Outside Rear"; } }

        public TrackingCamera(Viewer viewer, AttachedTo attachedTo)
            : base(viewer)
        {
            Front = attachedTo == AttachedTo.Front;
            PositionYRadians = StartPositionYRadians + (Front ? 0 : MathHelper.Pi);
            RotationXRadians = PositionXRadians;
            RotationYRadians = PositionYRadians - MathHelper.Pi;
        }

        protected internal override void Save(BinaryWriter outf)
        {
            base.Save(outf);
            outf.Write(PositionDistance);
            outf.Write(PositionXRadians);
            outf.Write(PositionYRadians);
            outf.Write(BrowseMode);
            outf.Write(BrowseForwards);
            outf.Write(BrowseBackwards);
            outf.Write(ZDistanceM);
        }

        protected internal override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            PositionDistance = inf.ReadSingle();
            PositionXRadians = inf.ReadSingle();
            PositionYRadians = inf.ReadSingle();
            BrowseMode = inf.ReadBoolean();
            BrowseForwards = inf.ReadBoolean();
            BrowseBackwards = inf.ReadBoolean();
            ZDistanceM = inf.ReadSingle();
            if (mvarAttachedCar != null && mvarAttachedCar.Train == Viewer.microSim.PlayerTrain)
            {
                var trainCars = GetCameraCars();
                BrowseDistance = mvarAttachedCar.CarLengthM * 0.5f;
                if (Front)
                {
                    browsedTraveller = new Traveller(mvarAttachedCar.Train.FrontTDBTraveller);
                    browsedTraveller.Move(-mvarAttachedCar.CarLengthM * 0.5f + ZDistanceM);
                }
                else
                {
                    browsedTraveller = new Traveller(mvarAttachedCar.Train.RearTDBTraveller);
                    browsedTraveller.Move((mvarAttachedCar.CarLengthM - trainCars.First().CarLengthM - trainCars.Last().CarLengthM) * 0.5f + mvarAttachedCar.Train.Length - ZDistanceM);
                }
                //               LookedAtPosition = new WorldPosition(attachedCar.WorldPosition);
                ComputeCarOffsets(this);
            }
        }

        public override void Reset()
        {
            base.Reset();
            PositionDistance = StartPositionDistance;
            PositionXRadians = StartPositionXRadians;
            PositionYRadians = StartPositionYRadians + (Front ? 0 : MathHelper.Pi);
            RotationXRadians = PositionXRadians;
            RotationYRadians = PositionYRadians - MathHelper.Pi;
        }

        protected override void OnActivate(bool sameCamera)
        {
            BrowseMode = BrowseForwards = BrowseBackwards = false;
            if (mvarAttachedCar == null || mvarAttachedCar.Train != Viewer.microSim.PlayerTrain)
            {
                if (Front)
                {
                    SetCameraCar(GetCameraCars().First());
                    browsedTraveller = new Traveller(mvarAttachedCar.Train.FrontTDBTraveller);
                    ZDistanceM = -mvarAttachedCar.CarLengthM / 2;
                    HighWagonOffsetLimit = 0;
                    LowWagonOffsetLimit = -mvarAttachedCar.CarLengthM;
                }
                else
                {
                    var trainCars = GetCameraCars();
                    SetCameraCar(trainCars.Last());
                    browsedTraveller = new Traveller(mvarAttachedCar.Train.RearTDBTraveller);
                    ZDistanceM = -mvarAttachedCar.Train.Length + (trainCars.First().CarLengthM + trainCars.Last().CarLengthM) * 0.5f + mvarAttachedCar.CarLengthM / 2;
                    LowWagonOffsetLimit = -mvarAttachedCar.Train.Length + trainCars.First().CarLengthM * 0.5f;
                    HighWagonOffsetLimit = LowWagonOffsetLimit + mvarAttachedCar.CarLengthM;
                }
                BrowseDistance = mvarAttachedCar.CarLengthM * 0.5f;
            }
            base.OnActivate(sameCamera);
        }

        protected override bool IsCameraFlipped()
        {
            return BrowseMode ? false : mvarAttachedCar.Flipped;
        }


        public override void Update(long elapsedTime)
        {
            var replayRemainingS = EndTime - elapsedTime;
            if (replayRemainingS > 0)
            {
                var replayFraction = elapsedTime/1000 / replayRemainingS;
                // Panning
                if (PositionXTargetRadians != null)
                {
                    var replayRemainingX = PositionXTargetRadians - PositionXRadians;
                    var replaySpeedX = (float)(replayRemainingX * replayFraction);
                    if (IsCloseEnough(PositionXRadians, PositionXTargetRadians, replaySpeedX))
                    {
                        PositionXTargetRadians = null;
                    }
                    else
                    {
                        PanUp(replaySpeedX);
                    }
                }
                if (PositionYTargetRadians != null)
                {
                    var replayRemainingY = PositionYTargetRadians - PositionYRadians;
                    var replaySpeedY = (float)(replayRemainingY * replayFraction);
                    if (IsCloseEnough(PositionYRadians, PositionYTargetRadians, replaySpeedY))
                    {
                        PositionYTargetRadians = null;
                    }
                    else
                    {
                        PanRight(replaySpeedY);
                    }
                }
                // Zooming
                if (PositionDistanceTargetMetres != null)
                {
                    var replayRemainingZ = PositionDistanceTargetMetres - PositionDistance;
                    var replaySpeedZ = (float)(replayRemainingZ * replayFraction);
                    if (IsCloseEnough(PositionDistance, PositionDistanceTargetMetres, replaySpeedZ))
                    {
                        PositionDistanceTargetMetres = null;
                    }
                    else
                    {
                        ZoomIn(replaySpeedZ / PositionDistance);
                    }
                }
            }

            // Rotation
            UpdateRotation(elapsedTime);

            // Update location of attachment
            mvarAttachedLocation.X = 0;
            mvarAttachedLocation.Y = 2;
            mvarAttachedLocation.Z = PositionDistance;
            mvarAttachedLocation = Vector3.Transform(mvarAttachedLocation, Matrix.CreateRotationX(-PositionXRadians));
            mvarAttachedLocation = Vector3.Transform(mvarAttachedLocation, Matrix.CreateRotationY(PositionYRadians));

            // Update location of camera
            if (BrowseMode)
            {
                UpdateTrainBrowsing(elapsedTime);
                mvarAttachedLocation.Z += BrowseDistance * (Front ? 1 : -1);
                LookedAtPosition.XNAMatrix = Matrix.CreateFromYawPitchRoll(-browsedTraveller.RotY, 0, 0);
                LookedAtPosition.XNAMatrix.M41 = browsedTraveller.X;
                LookedAtPosition.XNAMatrix.M42 = browsedTraveller.Y;
                LookedAtPosition.XNAMatrix.M43 = browsedTraveller.Z;
                //LookedAtPosition.TileX = browsedTraveller.TileX;
                //LookedAtPosition.TileZ = browsedTraveller.TileZ;
                LookedAtPosition.XNAMatrix.M43 *= -1;
            }
            else if (mvarAttachedCar != null)
            {
                LookedAtPosition = new WorldPosition(mvarAttachedCar.position);
            }
            UpdateLocation(LookedAtPosition);
        }

        protected void UpdateTrainBrowsing(long elapsedTime)
        {
            var trainCars = GetCameraCars();
            if (BrowseBackwards)
            {
                var ZIncrM = -BrowseSpeedMpS * (float)elapsedTime/1000;
                ZDistanceM += ZIncrM;
                if (-ZDistanceM >= mvarAttachedCar.Train.Length - (trainCars.First().CarLengthM + trainCars.Last().CarLengthM) * 0.5f)
                {
                    ZIncrM = -mvarAttachedCar.Train.Length + (trainCars.First().CarLengthM + trainCars.Last().CarLengthM) * 0.5f - (ZDistanceM - ZIncrM);
                    ZDistanceM = -mvarAttachedCar.Train.Length + (trainCars.First().CarLengthM + trainCars.Last().CarLengthM) * 0.5f;
                    BrowseBackwards = false;
                }
                else if (ZDistanceM < LowWagonOffsetLimit)
                {
                    base.PreviousCar();
                    HighWagonOffsetLimit = LowWagonOffsetLimit;
                    LowWagonOffsetLimit -= mvarAttachedCar.CarLengthM;
                }
                browsedTraveller.Move((float)elapsedTime/1000 * mvarAttachedCar.Train.SpeedMpS + ZIncrM);
            }
            else if (BrowseForwards)
            {
                var ZIncrM = BrowseSpeedMpS * (float)elapsedTime/1000;
                ZDistanceM += ZIncrM;
                if (ZDistanceM >= 0)
                {
                    ZIncrM = ZIncrM - ZDistanceM;
                    ZDistanceM = 0;
                    BrowseForwards = false;
                }
                else if (ZDistanceM > HighWagonOffsetLimit)
                {
                    base.NextCar();
                    LowWagonOffsetLimit = HighWagonOffsetLimit;
                    HighWagonOffsetLimit += mvarAttachedCar.CarLengthM;
                }
                browsedTraveller.Move((float)elapsedTime/1000 * mvarAttachedCar.Train.SpeedMpS + ZIncrM);
            }
            else browsedTraveller.Move((float)elapsedTime / 1000 * mvarAttachedCar.Train.SpeedMpS);
        }

        protected void ComputeCarOffsets(TrackingCamera camera)
        {
            var trainCars = camera.GetCameraCars();
            camera.HighWagonOffsetLimit = trainCars.First().CarLengthM * 0.5f;
            foreach (TrainCar trainCar in trainCars)
            {
                camera.LowWagonOffsetLimit = camera.HighWagonOffsetLimit - trainCar.CarLengthM;
                if (ZDistanceM > LowWagonOffsetLimit) break;
                else camera.HighWagonOffsetLimit = camera.LowWagonOffsetLimit;
            }
        }

        protected void PanUp(float speed)
        {
            PositionXRadians += speed;
            PositionXRadians = VerticalClamper.Clamp(PositionXRadians);
            RotationXRadians += speed;
            RotationXRadians = VerticalClamper.Clamp(RotationXRadians);
        }

        protected void PanRight(float speed)
        {
            PositionYRadians += speed;
            RotationYRadians += speed;
        }

        protected override void ZoomIn(float speed)
        {
            // Speed depends on distance, slows down when zooming in, speeds up zooming out.
            PositionDistance += speed * PositionDistance;
            PositionDistance = MathHelper.Clamp(PositionDistance, 1, 100);
        }

        /// <summary>
        /// Swaps front and rear tracking camera after reversal point, to avoid abrupt change of picture
        /// </summary>

        public void SwapCameras()
        {
            if (Front)
            {
                //SwapParams(this, Viewer.BackCamera);
                //Viewer.BackCamera.Activate();
            }
            else
            {
                //SwapParams(this, Viewer.FrontCamera);
                //Viewer.FrontCamera.Activate();
            }
        }


        /// <summary>
        /// Swaps parameters of Front and Back Camera
        /// </summary>
        /// 
        protected void SwapParams(TrackingCamera oldCamera, TrackingCamera newCamera)
        {
            TrainCar swapCar = newCamera.mvarAttachedCar;
            newCamera.mvarAttachedCar = oldCamera.mvarAttachedCar;
            oldCamera.mvarAttachedCar = swapCar;
            float swapFloat = newCamera.PositionDistance;
            newCamera.PositionDistance = oldCamera.PositionDistance;
            oldCamera.PositionDistance = swapFloat;
            swapFloat = newCamera.PositionXRadians;
            newCamera.PositionXRadians = oldCamera.PositionXRadians;
            oldCamera.PositionXRadians = swapFloat;
            swapFloat = newCamera.PositionYRadians;
            newCamera.PositionYRadians = oldCamera.PositionYRadians + MathHelper.Pi * (Front ? 1 : -1);
            oldCamera.PositionYRadians = swapFloat - MathHelper.Pi * (Front ? 1 : -1);
            swapFloat = newCamera.RotationXRadians;
            newCamera.RotationXRadians = oldCamera.RotationXRadians;
            oldCamera.RotationXRadians = swapFloat;
            swapFloat = newCamera.RotationYRadians;
            newCamera.RotationYRadians = oldCamera.RotationYRadians - MathHelper.Pi * (Front ? 1 : -1);
            oldCamera.RotationYRadians = swapFloat + MathHelper.Pi * (Front ? 1 : -1);

            // adjust and swap data for camera browsing

            newCamera.BrowseForwards = newCamera.BrowseBackwards = false;
            var trainCars = newCamera.GetCameraCars();
            newCamera.ZDistanceM = -newCamera.mvarAttachedCar.Train.Length + (trainCars.First().CarLengthM + trainCars.Last().CarLengthM) * 0.5f - oldCamera.ZDistanceM;
            ComputeCarOffsets(newCamera);
            // Todo travellers
        }


        public override void NextCar()
        {
            BrowseBackwards = false;
            BrowseForwards = false;
            BrowseMode = false;
            var trainCars = GetCameraCars();
            var wasFirstCar = mvarAttachedCar == trainCars.First();
            base.NextCar();
            if (!wasFirstCar)
            {
                LowWagonOffsetLimit = HighWagonOffsetLimit;
                HighWagonOffsetLimit += mvarAttachedCar.CarLengthM;
                ZDistanceM = LowWagonOffsetLimit + mvarAttachedCar.CarLengthM * 0.5f;
            }
            //           LookedAtPosition = new WorldPosition(attachedCar.WorldPosition);
        }

        public override void PreviousCar()
        {
            BrowseBackwards = false;
            BrowseForwards = false;
            BrowseMode = false;
            var trainCars = GetCameraCars();
            var wasLastCar = mvarAttachedCar == trainCars.Last();
            base.PreviousCar();
            if (!wasLastCar)
            {
                HighWagonOffsetLimit = LowWagonOffsetLimit;
                LowWagonOffsetLimit -= mvarAttachedCar.CarLengthM;
                ZDistanceM = LowWagonOffsetLimit + mvarAttachedCar.CarLengthM * 0.5f;
            }
            //           LookedAtPosition = new WorldPosition(attachedCar.WorldPosition);
        }

        public override void FirstCar()
        {
            BrowseBackwards = false;
            BrowseForwards = false;
            BrowseMode = false;
            base.FirstCar();
            ZDistanceM = 0;
            HighWagonOffsetLimit = mvarAttachedCar.CarLengthM * 0.5f;
            LowWagonOffsetLimit = -mvarAttachedCar.CarLengthM * 0.5f;
            //            LookedAtPosition = new WorldPosition(attachedCar.WorldPosition);
        }

        public override void LastCar()
        {
            BrowseBackwards = false;
            BrowseForwards = false;
            BrowseMode = false;
            base.LastCar();
            var trainCars = GetCameraCars();
            ZDistanceM = -mvarAttachedCar.Train.Length + (trainCars.First().CarLengthM + trainCars.Last().CarLengthM) * 0.5f;
            LowWagonOffsetLimit = -mvarAttachedCar.Train.Length + trainCars.First().CarLengthM * 0.5f;
            HighWagonOffsetLimit = LowWagonOffsetLimit + mvarAttachedCar.CarLengthM;
            //            LookedAtPosition = new WorldPosition(attachedCar.WorldPosition);
        }

        public void ToggleBrowseBackwards()
        {
            BrowseBackwards = !BrowseBackwards;
            if (BrowseBackwards)
            {
                if (!BrowseMode)
                {
                    //                    LookedAtPosition = new WorldPosition(attachedCar.WorldPosition);
                    browsedTraveller = new Traveller(mvarAttachedCar.Train.FrontTDBTraveller);
                    browsedTraveller.Move(-mvarAttachedCar.CarLengthM * 0.5f + ZDistanceM);
                    BrowseDistance = mvarAttachedCar.CarLengthM * 0.5f;
                    BrowseMode = true;
                }
            }
            BrowseForwards = false;
        }

        public void ToggleBrowseForwards()
        {
            BrowseForwards = !BrowseForwards;
            if (BrowseForwards)
            {
                if (!BrowseMode)
                {
                    //                    LookedAtPosition = new WorldPosition(attachedCar.WorldPosition);
                    browsedTraveller = new Traveller(mvarAttachedCar.Train.RearTDBTraveller);
                    var trainCars = GetCameraCars();
                    browsedTraveller.Move((mvarAttachedCar.CarLengthM - trainCars.First().CarLengthM - trainCars.Last().CarLengthM) * 0.5f + mvarAttachedCar.Train.Length + ZDistanceM);
                    BrowseDistance = mvarAttachedCar.CarLengthM * 0.5f;
                    BrowseMode = true;
                }
            }
            BrowseBackwards = false;
        }
    }
}

