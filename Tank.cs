using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace GunBond;

public class Tank
{
    public Vector2 Position;  // bottom-center of body
    public Vector2 Velocity;
    public float Angle = MathF.PI / 4f;
    public bool IsGrounded;

    private const float AngleSpeed = 1.5f;
    private const float MinAngle = 0.1f;
    private const float MaxAngle = MathF.PI - 0.1f;
    private const int BodyWidth = 40;
    private const int BodyHeight = 16;
    private const float TurretRadius = 8f;
    private const float CannonLength = 30f;
    private const float CannonThickness = 4f;

    private const float MoveSpeed = 120f;
    private const float Restitution = 0.15f;
    private const float GroundFriction = 0.85f;
    private const float AirDrag = 0.995f;
    private const float MinImpactSpeed = 200f;
    private const int MaxCraterRadius = 12;
    private const float SlopeSlideThreshold = 0.4f; // slope steepness to start sliding

    public void PlaceOnTerrain(Terrain terrain, int x)
    {
        int surfaceY = terrain.GetSurfaceY(x);
        Position = new Vector2(x, surfaceY);
        Velocity = Vector2.Zero;
        IsGrounded = true;
    }

    public void Update(GameTime gameTime, KeyboardState kb, Terrain terrain)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Aiming
        if (kb.IsKeyDown(Keys.Left))
            Angle += AngleSpeed * dt;
        if (kb.IsKeyDown(Keys.Right))
            Angle -= AngleSpeed * dt;
        Angle = Math.Clamp(Angle, MinAngle, MaxAngle);

        // Horizontal movement (only when grounded)
        if (IsGrounded)
        {
            if (kb.IsKeyDown(Keys.A))
                Velocity.X -= MoveSpeed * dt * 5f;
            if (kb.IsKeyDown(Keys.D))
                Velocity.X += MoveSpeed * dt * 5f;

            // Ground friction
            Velocity.X *= GroundFriction;
        }
        else
        {
            // Air drag
            Velocity *= AirDrag;
        }

        // Gravity
        Velocity.Y += GameConstants.Gravity * dt;

        // Movement with sub-stepping
        Vector2 movement = Velocity * dt;
        int steps = Math.Max(1, (int)(movement.Length() / 2f));
        Vector2 stepMove = movement / steps;
        float preStepSpeed = Velocity.Length();

        IsGrounded = false;

        for (int s = 0; s < steps; s++)
        {
            Position += stepMove;

            // Screen bounds
            Position.X = Math.Clamp(Position.X, BodyWidth / 2f, GameConstants.ScreenWidth - BodyWidth / 2f);
            if (Position.Y >= GameConstants.ScreenHeight)
            {
                Position.Y = GameConstants.ScreenHeight;
                OnTerrainImpact(terrain, preStepSpeed);
                break;
            }

            // Terrain collision - check bottom center and edges
            if (CheckTerrainBelow(terrain))
            {
                SnapToSurface(terrain);
                OnTerrainImpact(terrain, preStepSpeed);
                break;
            }

            // Side collisions
            if (CheckTerrainSide(terrain, -1)) // left
            {
                Position.X += 2;
                Velocity.X = MathF.Abs(Velocity.X) * Restitution;
            }
            else if (CheckTerrainSide(terrain, 1)) // right
            {
                Position.X -= 2;
                Velocity.X = -MathF.Abs(Velocity.X) * Restitution;
            }

            // Head collision
            if (CheckTerrainAbove(terrain))
            {
                Position.Y += 2;
                Velocity.Y = MathF.Abs(Velocity.Y) * Restitution;
            }
        }

        // Slope sliding when grounded
        if (IsGrounded)
        {
            ApplySlopePhysics(terrain, dt);
        }
    }

    public void ApplyExplosionForce(Vector2 explosionCenter, float force, float radius)
    {
        Vector2 direction = Position - explosionCenter;
        float dist = direction.Length();
        if (dist < 1f) direction = new Vector2(0, -1);
        else direction.Normalize();

        float intensity = 1f - Math.Min(dist / radius, 1f);
        intensity *= intensity;
        Vector2 push = direction * force * intensity;
        push.Y -= force * intensity * 0.4f; // upward bias

        Velocity += push;
        IsGrounded = false;
    }

    private void OnTerrainImpact(Terrain terrain, float impactSpeed)
    {
        // Crater on hard landing
        if (impactSpeed > MinImpactSpeed)
        {
            float intensity = Math.Min((impactSpeed - MinImpactSpeed) / 500f, 1f);
            int craterRadius = (int)(MaxCraterRadius * intensity);
            if (craterRadius >= 2)
            {
                terrain.CreateCrater((int)Position.X, (int)Position.Y, craterRadius);
            }
        }

        // Bounce
        if (MathF.Abs(Velocity.Y) > 30f)
        {
            Velocity.Y = -Velocity.Y * Restitution;
            Velocity.X *= GroundFriction;
        }
        else
        {
            Velocity.Y = 0;
        }

        IsGrounded = true;
    }

    private void ApplySlopePhysics(Terrain terrain, float dt)
    {
        int cx = (int)Position.X;
        int leftY = terrain.GetSurfaceY(cx - BodyWidth / 3);
        int rightY = terrain.GetSurfaceY(cx + BodyWidth / 3);
        float slope = (rightY - leftY) / (float)(BodyWidth * 2 / 3);

        if (MathF.Abs(slope) > SlopeSlideThreshold)
        {
            Velocity.X += slope * GameConstants.Gravity * dt * 0.5f;
        }
    }

    private bool CheckTerrainBelow(Terrain terrain)
    {
        int y = (int)Position.Y;
        if (y < 0 || y >= GameConstants.ScreenHeight) return false;

        int left = (int)(Position.X - BodyWidth / 2f);
        int right = (int)(Position.X + BodyWidth / 2f);
        for (int x = left; x <= right; x += 4)
        {
            if (terrain.IsSolid(x, y)) return true;
        }
        return false;
    }

    private bool CheckTerrainSide(Terrain terrain, int dir)
    {
        int x = (int)(Position.X + dir * BodyWidth / 2f);
        int top = (int)(Position.Y - BodyHeight);
        int bottom = (int)(Position.Y - 2);
        for (int y = top; y <= bottom; y += 3)
        {
            if (terrain.IsSolid(x, y)) return true;
        }
        return false;
    }

    private bool CheckTerrainAbove(Terrain terrain)
    {
        int y = (int)(Position.Y - BodyHeight - TurretRadius * 2);
        if (y < 0) return false;
        int left = (int)(Position.X - TurretRadius);
        int right = (int)(Position.X + TurretRadius);
        for (int x = left; x <= right; x += 4)
        {
            if (terrain.IsSolid(x, y)) return true;
        }
        return false;
    }

    private void SnapToSurface(Terrain terrain)
    {
        for (int i = 0; i < 40; i++)
        {
            Position.Y -= 1;
            if (!CheckTerrainBelow(terrain)) break;
        }
    }

    public Rectangle GetBounds()
    {
        return new Rectangle(
            (int)(Position.X - BodyWidth / 2),
            (int)(Position.Y - BodyHeight - TurretRadius * 2),
            BodyWidth,
            (int)(BodyHeight + TurretRadius * 2));
    }

    public Vector2 GetCannonTip()
    {
        Vector2 turretCenter = new Vector2(Position.X, Position.Y - BodyHeight - TurretRadius);
        return turretCenter + new Vector2(MathF.Cos(Angle), -MathF.Sin(Angle)) * CannonLength;
    }

    public void Draw(SpriteBatch sb, Texture2D pixel)
    {
        // Treads/wheels (two small rectangles at bottom)
        int treadY = (int)(Position.Y - 4);
        DrawHelper.DrawRect(sb, pixel,
            new Rectangle((int)(Position.X - BodyWidth / 2), treadY, BodyWidth, 4),
            new Color(60, 60, 60));

        // Body
        Rectangle bodyRect = new Rectangle(
            (int)(Position.X - BodyWidth / 2),
            (int)(Position.Y - BodyHeight),
            BodyWidth,
            BodyHeight - 4);
        DrawHelper.DrawRect(sb, pixel, bodyRect, new Color(34, 100, 34));

        // Body highlight
        DrawHelper.DrawRect(sb, pixel,
            new Rectangle(bodyRect.X + 2, bodyRect.Y + 2, bodyRect.Width - 4, 3),
            new Color(50, 130, 50));

        // Turret circle
        Vector2 turretCenter = new Vector2(Position.X, Position.Y - BodyHeight - TurretRadius);
        DrawHelper.DrawCircleFilled(sb, pixel, turretCenter, TurretRadius, new Color(25, 80, 25));

        // Cannon line
        Vector2 cannonEnd = turretCenter + new Vector2(MathF.Cos(Angle), -MathF.Sin(Angle)) * CannonLength;
        DrawHelper.DrawLine(sb, pixel, turretCenter, cannonEnd, new Color(50, 50, 50), CannonThickness);
        // Cannon tip
        DrawHelper.DrawCircleFilled(sb, pixel, cannonEnd, 3f, new Color(70, 70, 70));
    }
}
