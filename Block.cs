using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace GunBond;

public class Block
{
    public Vector2 Position;
    public int Width;
    public int Height;
    public Vector2 Velocity;
    public int MaxHealth;
    public int Health;
    public bool Destroyed;
    public Color BlockColor;
    public bool IsStatic;
    public float Mass;

    private const float Restitution = 0.35f;
    private const float Friction = 0.75f;
    private const float AirDrag = 0.992f;
    private const float MinImpactSpeed = 120f;
    private const float RestThreshold = 12f;
    private const int MaxCraterRadius = 15;
    private const float SlopeSlideThreshold = 0.35f;
    private const float BlockDamageSpeed = 250f;

    public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Width, Height);
    public Vector2 Center => Position + new Vector2(Width / 2f, Height / 2f);

    public Block(int x, int y, int width, int height, Color color, int health = 2)
    {
        Position = new Vector2(x, y);
        Width = width;
        Height = height;
        BlockColor = color;
        MaxHealth = health;
        Health = health;
        IsStatic = true;
        Mass = width * height / 400f; // normalized mass based on area
    }

    public void ApplyForce(Vector2 force)
    {
        Velocity += force / Mass; // lighter blocks fly further
        IsStatic = false;
    }

    public void Update(GameTime gameTime, Terrain terrain, List<Block> allBlocks)
    {
        if (Destroyed) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (IsStatic)
        {
            if (!HasSupportBelow(terrain, allBlocks))
                IsStatic = false;
            else
            {
                // Slope slide even when static
                ApplySlopeSlide(terrain, dt);
                return;
            }
        }

        // Gravity
        Velocity.Y += GameConstants.Gravity * dt;

        // Air drag
        Velocity.X *= AirDrag;
        Velocity.Y *= AirDrag;

        // Sub-step movement
        Vector2 movement = Velocity * dt;
        float speed = Velocity.Length();
        int steps = Math.Max(1, (int)(movement.Length() / 2f));
        Vector2 stepMove = movement / steps;

        for (int s = 0; s < steps; s++)
        {
            Position += stepMove;

            // Screen bounds
            if (Position.X < 0) { Position.X = 0; Velocity.X = MathF.Abs(Velocity.X) * Restitution; }
            if (Position.X + Width > GameConstants.ScreenWidth) { Position.X = GameConstants.ScreenWidth - Width; Velocity.X = -MathF.Abs(Velocity.X) * Restitution; }
            if (Position.Y + Height >= GameConstants.ScreenHeight)
            {
                Position.Y = GameConstants.ScreenHeight - Height;
                OnTerrainImpact(terrain, speed);
                break;
            }

            // Bottom collision
            if (CheckTerrainCollisionBottom(terrain))
            {
                SnapToTerrainSurface(terrain);
                OnTerrainImpact(terrain, speed);
                break;
            }

            // Side collisions with terrain
            if (Velocity.X > 0 && CheckTerrainCollisionRight(terrain))
            {
                PushOutOfTerrainRight(terrain);
                Velocity.X = -MathF.Abs(Velocity.X) * Restitution;
            }
            else if (Velocity.X < 0 && CheckTerrainCollisionLeft(terrain))
            {
                PushOutOfTerrainLeft(terrain);
                Velocity.X = MathF.Abs(Velocity.X) * Restitution;
            }

            // Top collision with terrain
            if (Velocity.Y < 0 && CheckTerrainCollisionTop(terrain))
            {
                Velocity.Y = MathF.Abs(Velocity.Y) * Restitution;
            }

            // Block-block collisions
            foreach (var other in allBlocks)
            {
                if (other == this || other.Destroyed) continue;
                if (Bounds.Intersects(other.Bounds))
                {
                    ResolveBlockCollision(other, terrain);
                }
            }
        }

        // Come to rest
        if (HasSupportBelow(terrain, allBlocks) && Velocity.Length() < RestThreshold)
        {
            Velocity = Vector2.Zero;
            IsStatic = true;
        }
    }

    private void OnTerrainImpact(Terrain terrain, float impactSpeed)
    {
        // Create crater proportional to impact speed and mass
        if (impactSpeed > MinImpactSpeed)
        {
            float intensity = Math.Min((impactSpeed - MinImpactSpeed) / 350f, 1f);
            int craterRadius = (int)(MaxCraterRadius * intensity * Mass);
            craterRadius = Math.Min(craterRadius, MaxCraterRadius);
            if (craterRadius >= 3)
            {
                int cx = (int)(Position.X + Width / 2f);
                int cy = (int)(Position.Y + Height);
                terrain.CreateCrater(cx, cy, craterRadius);
            }

            // Self-damage from hard impacts
            if (impactSpeed > 280f)
            {
                Health--;
                if (Health <= 0)
                {
                    Destroyed = true;
                    terrain.CreateCrater((int)Center.X, (int)Center.Y, (int)(Width / 2.5f));
                    return;
                }
            }
        }

        // Bounce vertically
        Velocity.Y = -Velocity.Y * Restitution;
        Velocity.X *= Friction;

        if (MathF.Abs(Velocity.Y) < 15f)
            Velocity.Y = 0;
    }

    private void ApplySlopeSlide(Terrain terrain, float dt)
    {
        int cx = (int)(Position.X + Width / 2f);
        int leftY = GetTerrainSurfaceAt(terrain, (int)Position.X);
        int rightY = GetTerrainSurfaceAt(terrain, (int)(Position.X + Width));
        float slope = (rightY - leftY) / (float)Width;

        if (MathF.Abs(slope) > SlopeSlideThreshold)
        {
            Velocity.X += slope * GameConstants.Gravity * dt * 0.6f;
            IsStatic = false;
        }
    }

    private int GetTerrainSurfaceAt(Terrain terrain, int x)
    {
        x = Math.Clamp(x, 0, GameConstants.ScreenWidth - 1);
        for (int y = (int)Position.Y + Height; y >= (int)Position.Y; y--)
        {
            if (!terrain.IsSolid(x, y)) return y + 1;
        }
        return (int)Position.Y;
    }

    private void ResolveBlockCollision(Block other, Terrain terrain)
    {
        Vector2 myCenter = Center;
        Vector2 otherCenter = other.Center;
        Vector2 diff = myCenter - otherCenter;

        float overlapX = (Width + other.Width) / 2f - MathF.Abs(diff.X);
        float overlapY = (Height + other.Height) / 2f - MathF.Abs(diff.Y);

        if (overlapX <= 0 || overlapY <= 0) return;

        float relativeSpeed = (Velocity - other.Velocity).Length();

        // Damage both blocks on high-speed collision
        if (relativeSpeed > BlockDamageSpeed)
        {
            Health--;
            other.Health--;
            if (Health <= 0) { Destroyed = true; terrain.CreateCrater((int)myCenter.X, (int)myCenter.Y, Width / 3); }
            if (other.Health <= 0) { other.Destroyed = true; terrain.CreateCrater((int)otherCenter.X, (int)otherCenter.Y, other.Width / 3); }
            if (Destroyed || other.Destroyed) return;
        }

        // Mass ratio for momentum transfer
        float totalMass = Mass + other.Mass;
        float myRatio = other.Mass / totalMass;   // heavier other = I move more
        float otherRatio = Mass / totalMass;       // heavier me = other moves more

        if (overlapX < overlapY)
        {
            // Horizontal resolution
            float sign = MathF.Sign(diff.X);
            Position.X += sign * overlapX * myRatio;
            if (!other.IsStatic)
                other.Position.X -= sign * overlapX * otherRatio;

            // Elastic-ish collision
            float v1 = Velocity.X;
            float v2 = other.Velocity.X;
            Velocity.X = (v1 * (Mass - other.Mass * Restitution) + v2 * other.Mass * (1 + Restitution)) / totalMass;
            if (!other.IsStatic || MathF.Abs(v1) > 40f)
            {
                other.Velocity.X = (v2 * (other.Mass - Mass * Restitution) + v1 * Mass * (1 + Restitution)) / totalMass;
                other.IsStatic = false;
            }
        }
        else
        {
            // Vertical resolution
            float sign = MathF.Sign(diff.Y);
            Position.Y += sign * overlapY * myRatio;
            if (!other.IsStatic)
                other.Position.Y -= sign * overlapY * otherRatio;

            float v1 = Velocity.Y;
            float v2 = other.Velocity.Y;
            Velocity.Y = (v1 * (Mass - other.Mass * Restitution) + v2 * other.Mass * (1 + Restitution)) / totalMass;
            Velocity.X *= Friction;

            if (!other.IsStatic || MathF.Abs(v1) > 40f)
            {
                other.Velocity.Y = (v2 * (other.Mass - Mass * Restitution) + v1 * Mass * (1 + Restitution)) / totalMass;
                other.IsStatic = false;
            }

            // Lateral scatter on vertical collision
            if (relativeSpeed > 80f)
            {
                float scatter = relativeSpeed * 0.15f;
                Velocity.X += (diff.X > 0 ? scatter : -scatter);
                if (!other.IsStatic)
                    other.Velocity.X -= (diff.X > 0 ? scatter : -scatter);
            }
        }
    }

    private bool CheckTerrainCollisionBottom(Terrain terrain)
    {
        int bottomY = (int)(Position.Y + Height);
        if (bottomY < 0 || bottomY >= GameConstants.ScreenHeight) return false;
        for (int x = (int)Position.X + 1; x < (int)Position.X + Width - 1; x += 3)
        {
            if (terrain.IsSolid(x, bottomY)) return true;
        }
        return false;
    }

    private bool CheckTerrainCollisionTop(Terrain terrain)
    {
        int topY = (int)Position.Y;
        if (topY < 0 || topY >= GameConstants.ScreenHeight) return false;
        for (int x = (int)Position.X + 1; x < (int)Position.X + Width - 1; x += 3)
        {
            if (terrain.IsSolid(x, topY)) return true;
        }
        return false;
    }

    private bool CheckTerrainCollisionRight(Terrain terrain)
    {
        int rightX = (int)(Position.X + Width);
        if (rightX < 0 || rightX >= GameConstants.ScreenWidth) return false;
        for (int y = (int)Position.Y + 2; y < (int)Position.Y + Height - 2; y += 3)
        {
            if (terrain.IsSolid(rightX, y)) return true;
        }
        return false;
    }

    private bool CheckTerrainCollisionLeft(Terrain terrain)
    {
        int leftX = (int)Position.X;
        if (leftX < 0 || leftX >= GameConstants.ScreenWidth) return false;
        for (int y = (int)Position.Y + 2; y < (int)Position.Y + Height - 2; y += 3)
        {
            if (terrain.IsSolid(leftX, y)) return true;
        }
        return false;
    }

    private void PushOutOfTerrainRight(Terrain terrain)
    {
        for (int i = 0; i < 10; i++)
        {
            Position.X -= 1;
            if (!CheckTerrainCollisionRight(terrain)) break;
        }
    }

    private void PushOutOfTerrainLeft(Terrain terrain)
    {
        for (int i = 0; i < 10; i++)
        {
            Position.X += 1;
            if (!CheckTerrainCollisionLeft(terrain)) break;
        }
    }

    private void SnapToTerrainSurface(Terrain terrain)
    {
        for (int i = 0; i < 40; i++)
        {
            Position.Y -= 1;
            if (!CheckTerrainCollisionBottom(terrain)) break;
        }
    }

    private bool HasSupportBelow(Terrain terrain, List<Block> allBlocks)
    {
        int bottomY = (int)(Position.Y + Height) + 1;
        if (bottomY >= GameConstants.ScreenHeight) return true;

        for (int x = (int)Position.X + 1; x < (int)Position.X + Width - 1; x += 3)
        {
            if (terrain.IsSolid(x, bottomY)) return true;
        }

        Rectangle belowRect = new Rectangle((int)Position.X, (int)(Position.Y + Height), Width, 2);
        foreach (var other in allBlocks)
        {
            if (other == this || other.Destroyed) continue;
            if (other.Bounds.Intersects(belowRect))
                return true;
        }

        return false;
    }

    public bool ContainsPoint(Vector2 point)
    {
        return Bounds.Contains(point.ToPoint());
    }

    public void Draw(SpriteBatch sb, Texture2D pixel)
    {
        if (Destroyed) return;

        var bounds = Bounds;

        // Damage tint - blocks get darker/redder as they lose health
        Color drawColor = BlockColor;
        if (Health < MaxHealth)
        {
            float damageFactor = (float)Health / MaxHealth;
            drawColor = Color.Lerp(new Color(180, 60, 60), BlockColor, damageFactor);
        }

        // Fill
        DrawHelper.DrawRect(sb, pixel, bounds, drawColor);

        // Crack lines for damaged blocks
        if (Health < MaxHealth)
        {
            int cx = bounds.Center.X;
            int cy = bounds.Center.Y;
            DrawHelper.DrawLine(sb, pixel,
                new Vector2(cx - 5, cy - 4), new Vector2(cx + 3, cy + 5),
                new Color(0, 0, 0, 100), 1f);
            DrawHelper.DrawLine(sb, pixel,
                new Vector2(cx + 2, cy - 3), new Vector2(cx - 4, cy + 4),
                new Color(0, 0, 0, 100), 1f);
        }

        // Border
        DrawHelper.DrawRect(sb, pixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), Color.Black);
        DrawHelper.DrawRect(sb, pixel, new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1), Color.Black);
        DrawHelper.DrawRect(sb, pixel, new Rectangle(bounds.X, bounds.Y, 1, bounds.Height), Color.Black);
        DrawHelper.DrawRect(sb, pixel, new Rectangle(bounds.Right - 1, bounds.Y, 1, bounds.Height), Color.Black);
    }
}
