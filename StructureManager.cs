using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace GunBond;

public class StructureManager
{
    private List<Block> _blocks = new();
    private const float ExplosionForce = 600f;

    public void AddBlock(Block block)
    {
        _blocks.Add(block);
    }

    public void BuildTower(int baseX, Terrain terrain, int floors, int blockWidth = 30, int blockHeight = 30)
    {
        int surfaceY = terrain.GetSurfaceY(baseX);
        Color[] colors = { new Color(180, 180, 180), new Color(200, 170, 130), new Color(160, 140, 120) };

        for (int i = 0; i < floors; i++)
        {
            int y = surfaceY - (i + 1) * blockHeight;
            var block = new Block(baseX - blockWidth / 2, y, blockWidth, blockHeight, colors[i % colors.Length], 2);
            _blocks.Add(block);
        }
    }

    public void BuildWall(int baseX, Terrain terrain, int columns, int rows, int blockWidth = 25, int blockHeight = 25)
    {
        int surfaceY = terrain.GetSurfaceY(baseX);
        Color wallColor = new Color(190, 160, 130);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                int x = baseX + col * blockWidth;
                int y = surfaceY - (row + 1) * blockHeight;
                _blocks.Add(new Block(x, y, blockWidth, blockHeight, wallColor, 2));
            }
        }
    }

    public Block CheckCollision(Vector2 point)
    {
        foreach (var block in _blocks)
        {
            if (!block.Destroyed && block.ContainsPoint(point))
                return block;
        }
        return null;
    }

    public void ApplyExplosion(Vector2 center, int radius, Terrain terrain, Tank tank)
    {
        float explosionRadius = radius * 2.5f;

        foreach (var block in _blocks)
        {
            if (block.Destroyed) continue;

            Vector2 blockCenter = block.Center;
            float closestX = Math.Clamp(center.X, block.Position.X, block.Position.X + block.Width);
            float closestY = Math.Clamp(center.Y, block.Position.Y, block.Position.Y + block.Height);
            float dist = Vector2.Distance(center, new Vector2(closestX, closestY));

            if (dist <= explosionRadius)
            {
                if (dist <= radius)
                {
                    block.Health--;
                    if (block.Health <= 0)
                    {
                        block.Destroyed = true;
                        terrain.CreateCrater((int)blockCenter.X, (int)blockCenter.Y, block.Width / 3);
                        continue;
                    }
                }

                Vector2 direction = blockCenter - center;
                if (direction.LengthSquared() < 1f)
                    direction = new Vector2(0, -1);
                direction.Normalize();

                float intensity = 1f - (dist / explosionRadius);
                intensity *= intensity;
                Vector2 force = direction * ExplosionForce * intensity;
                force.Y -= ExplosionForce * intensity * 0.3f;

                block.ApplyForce(force);
            }
        }

        _blocks.RemoveAll(b => b.Destroyed);

        // Push tank too
        tank.ApplyExplosionForce(center, ExplosionForce, explosionRadius);
    }

    public void Update(GameTime gameTime, Terrain terrain)
    {
        // Update all blocks
        foreach (var block in _blocks)
        {
            block.Update(gameTime, terrain, _blocks);
        }

        // Remove destroyed blocks (from impact damage)
        _blocks.RemoveAll(b => b.Destroyed);
    }

    public void Draw(SpriteBatch sb, Texture2D pixel)
    {
        foreach (var block in _blocks)
        {
            block.Draw(sb, pixel);
        }
    }
}
