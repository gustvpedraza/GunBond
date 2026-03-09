using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GunBond;

public class UI
{
    public void Draw(SpriteBatch sb, Texture2D pixel, Tank tank, PowerBar powerBar)
    {
        // Angle indicator text (drawn as a simple bar showing angle proportion)
        float angleDeg = MathHelper.ToDegrees(tank.Angle);
        float angleNorm = (tank.Angle - 0.1f) / (MathF.PI - 0.2f);

        // Angle label area
        int labelX = 10;
        int labelY = 10;

        // "ANGLE" bar
        DrawHelper.DrawRect(sb, pixel, new Rectangle(labelX, labelY, 80, 16), new Color(0, 0, 0, 150));
        DrawHelper.DrawRect(sb, pixel, new Rectangle(labelX + 2, labelY + 2, (int)(76 * angleNorm), 12), Color.CornflowerBlue);

        // Small angle direction indicator
        Vector2 indicatorCenter = new Vector2(labelX + 40, labelY + 35);
        float indicatorLen = 20f;
        Vector2 indicatorEnd = indicatorCenter + new Vector2(MathF.Cos(tank.Angle), -MathF.Sin(tank.Angle)) * indicatorLen;
        DrawHelper.DrawLine(sb, pixel, indicatorCenter, indicatorEnd, Color.White, 2f);
        DrawHelper.DrawCircleFilled(sb, pixel, indicatorCenter, 3f, Color.White);

        // Wind indicator placeholder
        int windX = GameConstants.ScreenWidth / 2 - 50;
        int windY = 10;
        DrawHelper.DrawRect(sb, pixel, new Rectangle(windX, windY, 100, 20), new Color(0, 0, 0, 150));
        // Arrow pointing right (placeholder)
        DrawHelper.DrawLine(sb, pixel, new Vector2(windX + 20, windY + 10), new Vector2(windX + 80, windY + 10), Color.White, 2f);
        // Arrowhead
        DrawHelper.DrawLine(sb, pixel, new Vector2(windX + 70, windY + 5), new Vector2(windX + 80, windY + 10), Color.White, 2f);
        DrawHelper.DrawLine(sb, pixel, new Vector2(windX + 70, windY + 15), new Vector2(windX + 80, windY + 10), Color.White, 2f);
    }
}
