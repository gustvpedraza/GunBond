using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GunBond;

public class Projectile
{
    public Vector2 Position;
    public Vector2 Velocity;
    public bool Active;
    public float Radius = 4f;
    public int ExplosionRadius = 30;

    public void Fire(Vector2 startPos, float angle, float speed)
    {
        Position = startPos;
        Velocity = new Vector2(MathF.Cos(angle), -MathF.Sin(angle)) * speed;
        Active = true;
    }

    /// <summary>Returns true if the projectile just impacted this frame.</summary>
    public bool Update(GameTime gameTime, Terrain terrain, StructureManager structures)
    {
        if (!Active) return false;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Velocity.Y += GameConstants.Gravity * dt;
        Position += Velocity * dt;

        // Off-screen
        if (Position.X < 0 || Position.X >= terrain.Width || Position.Y >= terrain.Height)
        {
            Active = false;
            return false;
        }

        // Terrain collision
        int px = (int)Position.X;
        int py = (int)Position.Y;
        if (py >= 0 && terrain.IsSolid(px, py))
        {
            Active = false;
            return true;
        }

        // Block collision
        if (structures.CheckCollision(Position) != null)
        {
            Active = false;
            return true;
        }

        return false;
    }

    public void Draw(SpriteBatch sb, Texture2D pixel)
    {
        if (!Active) return;
        DrawHelper.DrawCircleFilled(sb, pixel, Position, Radius, Color.White);
    }
}
