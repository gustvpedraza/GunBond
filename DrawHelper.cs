using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GunBond;

public static class DrawHelper
{
    public static void DrawRect(SpriteBatch sb, Texture2D pixel, Rectangle rect, Color color)
    {
        sb.Draw(pixel, rect, color);
    }

    public static void DrawLine(SpriteBatch sb, Texture2D pixel, Vector2 a, Vector2 b, Color color, float thickness = 2f)
    {
        Vector2 delta = b - a;
        float length = delta.Length();
        float angle = MathF.Atan2(delta.Y, delta.X);

        sb.Draw(pixel,
            a,
            null,
            color,
            angle,
            Vector2.Zero,
            new Vector2(length, thickness),
            SpriteEffects.None,
            0f);
    }

    public static void DrawCircleFilled(SpriteBatch sb, Texture2D pixel, Vector2 center, float radius, Color color)
    {
        int r = (int)radius;
        for (int y = -r; y <= r; y++)
        {
            int halfWidth = (int)MathF.Sqrt(r * r - y * y);
            sb.Draw(pixel,
                new Rectangle((int)(center.X - halfWidth), (int)(center.Y + y), halfWidth * 2, 1),
                color);
        }
    }
}
