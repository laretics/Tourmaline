using Microsoft.Xna.Framework.Graphics;
namespace Tourmaline.Viewer3D.Popups
{
    public abstract class LayeredWindow : Window
    {
        public LayeredWindow(WindowManager owner, int width, int height, string caption)
            : base(owner, width, height, caption)
        {
        }

        protected override ControlLayout Layout(ControlLayout layout)
        {
            return layout;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            // Don't draw the normal window stuff here.
        }
    }
}
