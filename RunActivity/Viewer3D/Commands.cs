using Tourmaline.Viewer3D.Popups;
using Tourmaline.Viewer3D.RollingStock;
using TOURMALINE.Common;
using System;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using System.Reflection;
using Tourmaline.Common;
using Tourmaline.Viewer3D;

namespace Tourmaline.Viewer3D
{

    // Other
    [Serializable()]
    public sealed class SaveScreenshotCommand : Command
    {
        public static Viewer Receiver { get; set; }

        public SaveScreenshotCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.SaveScreenshot = true;
            // Report();
        }
    }

    [Serializable()]
    public abstract class UseCameraCommand : CameraCommand
    {
        public static Viewer Receiver { get; set; }

        public UseCameraCommand(CommandLog log)
            : base(log)
        {
        }
    }

    [Serializable()]
    public sealed class UseFrontCameraCommand : UseCameraCommand
    {

        public UseFrontCameraCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.FrontCamera.Activate();
            // Report();
        }
    }

    [Serializable()]
    public sealed class UseBackCameraCommand : UseCameraCommand
    {

        public UseBackCameraCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            Receiver.BackCamera.Activate();
            // Report();
        }
    }

    [Serializable()]
    public abstract class MoveCameraCommand : CameraCommand
    {
        public static Viewer Receiver { get; set; }
        protected long EndTime;

        public MoveCameraCommand(CommandLog log, long startTime, long endTime)
            : base(log)
        {
            Time = startTime;
            EndTime = endTime;
        }

        public override string ToString()
        {
            return base.ToString() + " - " + String.Format("{0}", FormatStrings.FormatPreciseTime(EndTime));
        }
    }

    [Serializable()]
    public sealed class CameraRotateUpDownCommand : MoveCameraCommand
    {
        float RotationXRadians;

        public CameraRotateUpDownCommand(CommandLog log, long startTime, long endTime, float rx)
            : base(log, startTime, endTime)
        {
            RotationXRadians = rx;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver.Camera is RotatingCamera)
            {
                var c = Receiver.Camera as RotatingCamera;
                c.RotationXTargetRadians = RotationXRadians;
                c.EndTime = EndTime;
            }
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", {0}", RotationXRadians);
        }
    }

    [Serializable()]
    public sealed class CameraRotateLeftRightCommand : MoveCameraCommand
    {
        float RotationYRadians;

        public CameraRotateLeftRightCommand(CommandLog log, long startTime, long endTime, float ry)
            : base(log, startTime, endTime)
        {
            RotationYRadians = ry;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver.Camera is RotatingCamera)
            {
                var c = Receiver.Camera as RotatingCamera;
                c.RotationYTargetRadians = RotationYRadians;
                c.EndTime = EndTime;
            }
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", {0}", RotationYRadians);
        }
    }

    /// <summary>
    /// Records rotations made by mouse movements.
    /// </summary>
    [Serializable()]
    public sealed class CameraMouseRotateCommand : MoveCameraCommand
    {
        float RotationXRadians;
        float RotationYRadians;

        public CameraMouseRotateCommand(CommandLog log, long startTime, long endTime, float rx, float ry)
            : base(log, startTime, endTime)
        {
            RotationXRadians = rx;
            RotationYRadians = ry;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver.Camera is RotatingCamera)
            {
                var c = Receiver.Camera as RotatingCamera;
                c.EndTime = EndTime;
                c.RotationXTargetRadians = RotationXRadians;
                c.RotationYTargetRadians = RotationYRadians;
            }
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", {0} {1} {2}", EndTime, RotationXRadians, RotationYRadians);
        }
    }

    [Serializable()]
    public sealed class CameraXCommand : MoveCameraCommand
    {
        float XRadians;

        public CameraXCommand(CommandLog log, long startTime, long endTime, float xr)
            : base(log, startTime, endTime)
        {
            XRadians = xr;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver.Camera is RotatingCamera)
            {
                var c = Receiver.Camera as RotatingCamera;
                c.XTargetRadians = XRadians;
                c.EndTime = EndTime;
            }
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", {0}", XRadians);
        }
    }

    [Serializable()]
    public sealed class CameraYCommand : MoveCameraCommand
    {
        float YRadians;

        public CameraYCommand(CommandLog log, long startTime, long endTime, float yr)
            : base(log, startTime, endTime)
        {
            YRadians = yr;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver.Camera is RotatingCamera)
            {
                var c = Receiver.Camera as RotatingCamera;
                c.YTargetRadians = YRadians;
                c.EndTime = EndTime;
            }
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", {0}", YRadians);
        }
    }

    [Serializable()]
    public sealed class CameraZCommand : MoveCameraCommand
    {
        float ZRadians;

        public CameraZCommand(CommandLog log, long startTime, long endTime, float zr)
            : base(log, startTime, endTime)
        {
            ZRadians = zr;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver.Camera is RotatingCamera)
            {
                var c = Receiver.Camera as RotatingCamera;
                c.ZTargetRadians = ZRadians;
                c.EndTime = EndTime;
            } // Report();
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", {0}", ZRadians);
        }
    }

    [Serializable()]
    public sealed class CameraMoveXYZCommand : MoveCameraCommand
    {
        float X, Y, Z;

        public CameraMoveXYZCommand(CommandLog log, long startTime, long endTime, float xr, float yr, float zr)
            : base(log, startTime, endTime)
        {
            X = xr; Y = yr; Z = zr;
            Redo();
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", {0}", X);
        }
    }

    [Serializable()]
    public sealed class TrackingCameraXCommand : MoveCameraCommand
    {
        float PositionXRadians;

        public TrackingCameraXCommand(CommandLog log, long startTime, long endTime, float rx)
            : base(log, startTime, endTime)
        {
            PositionXRadians = rx;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver.Camera is TrackingCamera)
            {
                var c = Receiver.Camera as TrackingCamera;
                c.PositionXTargetRadians = PositionXRadians;
                c.EndTime = EndTime;
            }
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", {0}", PositionXRadians);
        }
    }

    [Serializable()]
    public sealed class TrackingCameraYCommand : MoveCameraCommand
    {
        float PositionYRadians;

        public TrackingCameraYCommand(CommandLog log, long startTime, long endTime, float ry)
            : base(log, startTime, endTime)
        {
            PositionYRadians = ry;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver.Camera is TrackingCamera)
            {
                var c = Receiver.Camera as TrackingCamera;
                c.PositionYTargetRadians = PositionYRadians;
                c.EndTime = EndTime;
            }
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", {0}", PositionYRadians);
        }
    }

    [Serializable()]
    public sealed class TrackingCameraZCommand : MoveCameraCommand
    {
        float PositionDistanceMetres;

        public TrackingCameraZCommand(CommandLog log, long startTime, long endTime, float d)
            : base(log, startTime, endTime)
        {
            PositionDistanceMetres = d;
            Redo();
        }

        public override void Redo()
        {
            if (Receiver.Camera is TrackingCamera)
            {
                var c = Receiver.Camera as TrackingCamera;
                c.PositionDistanceTargetMetres = PositionDistanceMetres;
                c.EndTime = EndTime;
            }
            // Report();
        }

        public override string ToString()
        {
            return base.ToString() + String.Format(", {0}", PositionDistanceMetres);
        }
    }

    [Serializable()]
    public sealed class NextCarCommand : UseCameraCommand
    {

        public NextCarCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver.Camera is AttachedCamera)
            {
                var c = Receiver.Camera as AttachedCamera;
                c.NextCar();
            }
            // Report();
        }
    }

    [Serializable()]
    public sealed class PreviousCarCommand : UseCameraCommand
    {

        public PreviousCarCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver.Camera is AttachedCamera)
            {
                var c = Receiver.Camera as AttachedCamera;
                c.PreviousCar();
            }
            // Report();
        }
    }

    [Serializable()]
    public sealed class FirstCarCommand : UseCameraCommand
    {

        public FirstCarCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver.Camera is AttachedCamera)
            {
                var c = Receiver.Camera as AttachedCamera;
                c.FirstCar();
            }
            // Report();
        }
    }

    [Serializable()]
    public sealed class LastCarCommand : UseCameraCommand
    {

        public LastCarCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver.Camera is AttachedCamera)
            {
                var c = Receiver.Camera as AttachedCamera;
                c.LastCar();
            }
            // Report();
        }
    }

    [Serializable]
    public sealed class FieldOfViewCommand : UseCameraCommand
    {
        float FieldOfView;

        public FieldOfViewCommand(CommandLog log, float fieldOfView)
            : base(log)
        {
            FieldOfView = fieldOfView;
            Redo();
        }

        public override void Redo()
        {
            Receiver.Camera.FieldOfView = FieldOfView;
            Receiver.Camera.ScreenChanged();
        }
    }

    [Serializable()]
    public sealed class ToggleBrowseBackwardsCommand : UseCameraCommand
    {

        public ToggleBrowseBackwardsCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver.Camera is TrackingCamera)
            {
                var c = Receiver.Camera as TrackingCamera;
                c.ToggleBrowseBackwards();
            }
            // Report();
        }
    }

    [Serializable()]
    public sealed class ToggleBrowseForwardsCommand : UseCameraCommand
    {

        public ToggleBrowseForwardsCommand(CommandLog log)
            : base(log)
        {
            Redo();
        }

        public override void Redo()
        {
            if (Receiver.Camera is TrackingCamera)
            {
                var c = Receiver.Camera as TrackingCamera;
                c.ToggleBrowseForwards();
            }
            // Report();
        }
    }
}
