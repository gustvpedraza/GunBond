using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace GunBond;

public class PowerBar
{
    public float Power { get; private set; }
    public float MaxSpeed = 800f;
    public bool IsCharging { get; private set; }
    public bool JustFired { get; private set; }

    private const float ChargeSpeed = 1.0f;
    private bool _wasSpaceDown;

    public void Update(GameTime gameTime, KeyboardState kb)
    {
        JustFired = false;
        bool spaceDown = kb.IsKeyDown(Keys.Space);

        if (spaceDown)
        {
            IsCharging = true;
            Power = Math.Min(Power + ChargeSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds, 1f);
        }
        else if (_wasSpaceDown)
        {
            JustFired = true;
            IsCharging = false;
        }

        if (!spaceDown && !JustFired)
            Power = 0f;

        _wasSpaceDown = spaceDown;
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, Vector2 position)
    {
        int barWidth = 200;
        int barHeight = 20;

        // Background
        DrawHelper.DrawRect(sb, pixel,
            new Rectangle((int)position.X - 1, (int)position.Y - 1, barWidth + 2, barHeight + 2),
            Color.DarkGray);

        // Fill
        int fillWidth = (int)(Power * barWidth);
        Color fillColor;
        if (Power < 0.5f)
            fillColor = Color.Lerp(Color.Green, Color.Yellow, Power * 2f);
        else
            fillColor = Color.Lerp(Color.Yellow, Color.Red, (Power - 0.5f) * 2f);

        if (fillWidth > 0)
        {
            DrawHelper.DrawRect(sb, pixel,
                new Rectangle((int)position.X, (int)position.Y, fillWidth, barHeight),
                fillColor);
        }
    }
}
