using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace GunBond;

public class Terrain
{
    private bool[] _solid;
    private Texture2D _texture;
    private Color[] _pixels;

    // Dirty region tracking - only rebuild what changed
    private bool _fullRebuildNeeded;
    private bool _hasDirtyRegion;
    private int _dirtyMinX, _dirtyMinY, _dirtyMaxX, _dirtyMaxY;

    // Buffer for partial texture updates
    private Color[] _regionBuffer;

    public int Width { get; }
    public int Height { get; }

    private static readonly Color GrassColor = new Color(76, 153, 0);
    private static readonly Color BrownColor = new Color(139, 90, 43);
    private static readonly Color DarkBrownColor = new Color(110, 70, 30);

    public Terrain(GraphicsDevice device, int width, int height)
    {
        Width = width;
        Height = height;
        _solid = new bool[width * height];
        _pixels = new Color[width * height];
        _texture = new Texture2D(device, width, height);
    }

    public void Generate()
    {
        int baseHeight = Height - 200;

        for (int x = 0; x < Width; x++)
        {
            float surfaceY = baseHeight
                - 40f * MathF.Sin(x / 120f)
                - 25f * MathF.Sin(x / 60f + 1.5f)
                - 15f * MathF.Sin(x / 30f + 3.0f);

            int sy = (int)surfaceY;

            for (int y = 0; y < Height; y++)
            {
                _solid[y * Width + x] = y >= sy;
            }
        }

        _fullRebuildNeeded = true;
    }

    public bool IsSolid(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
        return _solid[y * Width + x];
    }

    public void CreateCrater(int cx, int cy, int radius)
    {
        int r2 = radius * radius;
        int minY = Math.Max(0, cy - radius);
        int maxY = Math.Min(Height - 1, cy + radius);
        int minX = Math.Max(0, cx - radius);
        int maxX = Math.Min(Width - 1, cx + radius);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int dx = x - cx;
                int dy = y - cy;
                if (dx * dx + dy * dy <= r2)
                {
                    _solid[y * Width + x] = false;
                }
            }
        }

        // Expand dirty region slightly to catch new grass edges
        MarkDirtyRegion(minX, Math.Max(0, minY - 3), maxX, maxY);
    }

    public int GetSurfaceY(int x)
    {
        if (x < 0 || x >= Width) return Height;
        for (int y = 0; y < Height; y++)
        {
            if (_solid[y * Width + x]) return y;
        }
        return Height;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_fullRebuildNeeded)
        {
            FullRebuildTexture();
            _fullRebuildNeeded = false;
            _hasDirtyRegion = false;
        }
        else if (_hasDirtyRegion)
        {
            PartialRebuildTexture();
            _hasDirtyRegion = false;
        }

        spriteBatch.Draw(_texture, Vector2.Zero, Color.White);
    }

    private void MarkDirtyRegion(int minX, int minY, int maxX, int maxY)
    {
        if (!_hasDirtyRegion)
        {
            _dirtyMinX = minX;
            _dirtyMinY = minY;
            _dirtyMaxX = maxX;
            _dirtyMaxY = maxY;
            _hasDirtyRegion = true;
        }
        else
        {
            _dirtyMinX = Math.Min(_dirtyMinX, minX);
            _dirtyMinY = Math.Min(_dirtyMinY, minY);
            _dirtyMaxX = Math.Max(_dirtyMaxX, maxX);
            _dirtyMaxY = Math.Max(_dirtyMaxY, maxY);
        }
    }

    private void PartialRebuildTexture()
    {
        int regionW = _dirtyMaxX - _dirtyMinX + 1;
        int regionH = _dirtyMaxY - _dirtyMinY + 1;
        int regionSize = regionW * regionH;

        if (_regionBuffer == null || _regionBuffer.Length < regionSize)
            _regionBuffer = new Color[regionSize];

        for (int ry = 0; ry < regionH; ry++)
        {
            int y = _dirtyMinY + ry;
            for (int rx = 0; rx < regionW; rx++)
            {
                int x = _dirtyMinX + rx;
                int idx = y * Width + x;
                Color color;

                if (_solid[idx])
                {
                    // Check if pixel above is air -> grass
                    bool isGrass = y == 0 || !_solid[(y - 1) * Width + x]
                                          || (y >= 2 && !_solid[(y - 2) * Width + x])
                                          || (y >= 3 && !_solid[(y - 3) * Width + x]);
                    if (isGrass)
                        color = GrassColor;
                    else if (y < 3 || !_solid[(y - 20) * Width + x] || y - GetLocalSurface(x, y) < 20)
                        color = BrownColor;
                    else
                        color = DarkBrownColor;
                }
                else
                {
                    color = Color.Transparent;
                }

                _regionBuffer[ry * regionW + rx] = color;
                _pixels[idx] = color;
            }
        }

        var rect = new Rectangle(_dirtyMinX, _dirtyMinY, regionW, regionH);
        _texture.SetData(0, rect, _regionBuffer, 0, regionSize);
    }

    private int GetLocalSurface(int x, int y)
    {
        // Walk upward from y to find the surface (first air pixel)
        for (int sy = y; sy >= 0; sy--)
        {
            if (!_solid[sy * Width + x]) return sy + 1;
        }
        return 0;
    }

    private void FullRebuildTexture()
    {
        for (int x = 0; x < Width; x++)
        {
            int surfaceY = -1;
            for (int y = 0; y < Height; y++)
            {
                int idx = y * Width + x;
                if (_solid[idx])
                {
                    if (surfaceY < 0) surfaceY = y;
                    int depth = y - surfaceY;
                    if (depth < 3)
                        _pixels[idx] = GrassColor;
                    else if (depth < 20)
                        _pixels[idx] = BrownColor;
                    else
                        _pixels[idx] = DarkBrownColor;
                }
                else
                {
                    _pixels[idx] = Color.Transparent;
                    surfaceY = -1;
                }
            }
        }

        _texture.SetData(_pixels);
    }
}
