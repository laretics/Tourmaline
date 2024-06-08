using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TOURMALINE.Common;

namespace Tourmaline.Viewer3D.Popups
{
    public class LabelPrimitive : RenderPrimitive
    {
        readonly Label3DMaterial Material;

        public WorldPosition Position;
        public string Text;
        public Color Color;
        public Color Outline;

        readonly Viewer Viewer;
        readonly float OffsetY;

        public LabelPrimitive(Label3DMaterial material, Color color, Color outline, float offsetY)
        {
            Material = material;
            Viewer = material.Viewer;
            Color = color;
            Outline = outline;
            OffsetY = offsetY;
        }

        public override void Draw(GraphicsDevice graphicsDevice)
        {
            var lineLocation3D = Position.XNAMatrix.Translation;
            //lineLocation3D.X += (Position.TileX - Viewer.Camera.TileX) * 2048;
            lineLocation3D.Y += OffsetY;
            //lineLocation3D.Z += (Viewer.Camera.TileZ - Position.TileZ) * 2048;

            var lineLocation2DStart = Viewer.GraphicsDevice.Viewport.Project(lineLocation3D, Viewer.Camera.XnaProjection, Viewer.Camera.XnaView, Matrix.Identity);
            if (lineLocation2DStart.Z > 1 || lineLocation2DStart.Z < 0)
                return; // Out of range or behind the camera

            lineLocation3D.Y += 10;
            var lineLocation2DEndY = Viewer.GraphicsDevice.Viewport.Project(lineLocation3D, Viewer.Camera.XnaProjection, Viewer.Camera.XnaView, Matrix.Identity).Y;

            var labelLocation2D = Material.GetTextLocation((int)lineLocation2DStart.X, (int)lineLocation2DEndY - Material.Font.Height, Text);
            lineLocation2DEndY = labelLocation2D.Y + Material.Font.Height;

            Material.Font.Draw(Material.SpriteBatch, labelLocation2D, Text, Color, Outline);
            Material.SpriteBatch.Draw(Material.Texture, new Vector2(lineLocation2DStart.X - 1, lineLocation2DEndY), null, Outline, 0, Vector2.Zero, new Vector2(4, lineLocation2DStart.Y - lineLocation2DEndY), SpriteEffects.None, lineLocation2DStart.Z);
            Material.SpriteBatch.Draw(Material.Texture, new Vector2(lineLocation2DStart.X, lineLocation2DEndY), null, Color, 0, Vector2.Zero, new Vector2(2, lineLocation2DStart.Y - lineLocation2DEndY), SpriteEffects.None, lineLocation2DStart.Z);
        }
    }
}
