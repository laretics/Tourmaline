using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tourmaline.Common;
using Tourmaline.Viewer3D.Processes;
using Tourmaline.Viewer3D.TvForms;
using TOURMALINE.Common;
using TOURMALINE.Common.Input;

namespace Tourmaline.Viewer3D.Materials
{
    internal abstract class _3dBaseCamera
    {
        protected long CommandStartTime;
        // 2.1 sets the limit at just under a right angle as get unwanted swivel at the full right angle.
        protected static CameraAngleClamper VerticalClamper = new CameraAngleClamper(-MathHelper.Pi / 2.1f, MathHelper.Pi / 2.1f);

        protected readonly GraphicsDeviceControl mvarControl;

        protected Vector3 cameraLocation = new Vector3();
        internal Vector3 Location { get => cameraLocation; }
        protected internal float FieldOfView;

        protected Matrix xnaView;
        internal Matrix XnaView { get => xnaView; }

        Matrix xnaProjection;
        internal Matrix XnaProjection { get => xnaProjection; }

        Vector3 frustumRightProjected;
        Vector3 frustumLeft;
        Vector3 frustumRight;

        internal virtual float NearPlane { get { return 1.0f; } }

        const int SpeedFactorFastSlow = 8;  // Use by GetSpeed

        protected _3dBaseCamera(GraphicsDeviceControl myControl)
        {
            mvarControl = myControl;
            FieldOfView = FirstLoadProcess.Instance.ViewingFOV;
        }

        /// <summary>
        /// Resets a camera's position, location and attachment information.
        /// </summary>
        internal virtual void Reset()
        {
            FieldOfView = FirstLoadProcess.Instance.ViewingFOV;
            ScreenChanged();
        }

        internal void Activate()
        {
            ScreenChanged();

            OnActivate(mvarControl.mvarViewer.Camera == this);
            mvarControl.mvarViewer.Camera = this;
            Update(0);
            xnaView = GetCameraView();
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
            float aspectRatio = mvarControl.Size.Width / mvarControl.Size.Height;
            //var farPlaneDistance = SkyConstants.skyRadius + 100;  // so far the sky is the biggest object in view
            var fovWidthRadians = MathHelper.ToRadians(FieldOfView);
            xnaProjection = Matrix.CreatePerspectiveFieldOfView(fovWidthRadians, aspectRatio, NearPlane,FirstLoadProcess.Instance.ViewingDistance);

            //XNASkyProjection = Matrix.CreatePerspectiveFieldOfView(fovWidthRadians, aspectRatio, NearPlane, farPlaneDistance);    // TODO remove? 
            frustumRightProjected.X = (float)Math.Cos(fovWidthRadians / 2 * aspectRatio);  // Precompute the right edge of the view frustrum.
            frustumRightProjected.Z = (float)Math.Sin(fovWidthRadians / 2 * aspectRatio);
        }

        /// <summary>
        /// Updates view and projection from this camera's data.
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="elapsedTime"></param>
        internal void PrepareFrame(RenderFrame frame, long elapsedTime)
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
        internal bool InFov(Vector3 mstsObjectCenter, float objectRadius)
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
        internal bool InRange(Vector3 mstsObjectCenter, float objectRadius, float objectViewingDistance)
        {
            mstsObjectCenter.X -= cameraLocation.X;
            mstsObjectCenter.Z -= cameraLocation.Z;

            float distancia = FirstLoadProcess.Instance.ViewingDistance;
            // An object cannot be visible further away than the viewing distance.
            if (objectViewingDistance > distancia)
                objectViewingDistance = distancia;

            var distanceSquared = mstsObjectCenter.X * mstsObjectCenter.X + mstsObjectCenter.Z * mstsObjectCenter.Z;

            return distanceSquared < (objectRadius + objectViewingDistance) * (objectRadius + objectViewingDistance);
        }

        /// <summary>
        /// If the nearest part of the object is within camera viewing distance
        /// and is within the object's defined viewing distance then
        /// we can see it.   The objectViewingDistance allows a small object
        /// to specify a cutoff beyond which the object can't be seen.
        /// </summary>
        internal bool CanSee(Vector3 mstsObjectCenter, float objectRadius, float objectViewingDistance)
        {
            if (!InRange(mstsObjectCenter, objectRadius, objectViewingDistance))
                return false;

            if (!InFov(mstsObjectCenter, objectRadius))
                return false;

            return true;
        }

        protected static float GetSpeed(long elapsed)
        {
            var speed = 5 * (float)elapsed / 1000;
            if (UserInput.IsDown(UserCommand.CameraMoveFast))
                speed *= SpeedFactorFastSlow;
            if (UserInput.IsDown(UserCommand.CameraMoveSlow))
                speed /= SpeedFactorFastSlow;
            return speed;
        }

        protected virtual void ZoomIn(float speed)
        {
        }

        /// <summary>
        /// Returns a position in XNA space relative to the camera's tile
        /// </summary>
        /// <param name="worldLocation"></param>
        /// <returns></returns>
        internal Vector3 XnaLocation(Vector3 worldLocation)
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
    }

    internal class RotatingCamera : _3dBaseCamera
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

        protected RotatingCamera(GraphicsDeviceControl myControl)
            : base(myControl)
        { }

        internal override void Reset()
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

    internal class LookAtCamera:RotatingCamera
    {
        protected Vector3 targetLocation = new Vector3();
        protected LookAtCamera(GraphicsDeviceControl myControl)
            : base(myControl) { }

        protected override Matrix GetCameraView()
        {
            return Matrix.CreateLookAt(XnaLocation(cameraLocation), XnaLocation(targetLocation), Vector3.UnitY);
        }
    }

    internal class _3dCamera:LookAtCamera
    {
        protected Vector3 mvarLocation;
        internal _3dCamera(GraphicsDeviceControl myControl):base(myControl)
        {
            mvarLocation = new Vector3(80, 10, 0);
            RotationYRadians = (float)(-Math.PI / 2);          
        }
        internal void UpdateLocation(WorldPosition position)
        {
            cameraLocation = mvarLocation;
            cameraLocation.Z *= -1;
            cameraLocation = Vector3.Transform(cameraLocation, position.XNAMatrix);
            cameraLocation.Z *= -1;
        }
        protected override Matrix GetCameraView()
        {
            Vector3 lookAtPosition = Vector3.UnitZ;
            lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationX(RotationXRadians));
            lookAtPosition = Vector3.Transform(lookAtPosition, Matrix.CreateRotationY(RotationYRadians));
            lookAtPosition.X += mvarLocation.X;
            lookAtPosition.Y += mvarLocation.Y;
            lookAtPosition.Z += mvarLocation.Z;
            lookAtPosition.Z *= -1;
            return Matrix.CreateLookAt(XnaLocation(cameraLocation), lookAtPosition,Vector3.UnitZ);
        }
        public override void Update(long elapsedTime)
        {
            cameraLocation.X = mvarLocation.X;
            cameraLocation.Y = mvarLocation.Y;
            cameraLocation.Z = mvarLocation.Z;
            UpdateRotation(elapsedTime);    
        }
        public override void HandleUserInput(long elapsedTime)
        {
            RotationYRadians = -TOURMALINEMath.MatrixToYAngle(xnaView);
            float speed = GetSpeed(elapsedTime);
            if (UserInput.IsDown(UserCommand.CameraPanUp))
            {
                mvarLocation.Y += speed;
            }
            if (UserInput.IsDown(UserCommand.CameraPanDown))
            {
                mvarLocation.Y -= speed;
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
            mvarLocation.X += speed;
        }
        protected override void ZoomIn(float speed)
        {
            mvarLocation.Z += speed;
        }
    }


}
