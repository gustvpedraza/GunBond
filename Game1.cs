using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GunBond;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _pixel;

    private Terrain _terrain;
    private Tank _tank;
    private PowerBar _powerBar;
    private Projectile _projectile;
    private StructureManager _structures;
    private UI _ui;

    private KeyboardState _prevKb;
    private MouseState _prevMouse;
    private Rectangle _resetButtonRect = new Rectangle(GameConstants.ScreenWidth - 110, 8, 100, 28);

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = GameConstants.ScreenWidth;
        _graphics.PreferredBackBufferHeight = GameConstants.ScreenHeight;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _ui = new UI();
        LoadLevel();
    }

    private void LoadLevel()
    {
        _terrain = new Terrain(GraphicsDevice, GameConstants.TerrainWidth, GameConstants.TerrainHeight);
        _terrain.Generate();

        _tank = new Tank();
        _tank.PlaceOnTerrain(_terrain, 200);

        _powerBar = new PowerBar();
        _projectile = new Projectile();

        _structures = new StructureManager();
        _structures.BuildTower(800, _terrain, 4);
        _structures.BuildTower(900, _terrain, 3);
        _structures.BuildWall(1000, _terrain, 3, 3);
        _structures.BuildTower(600, _terrain, 10);
    }

    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();

        if (kb.IsKeyDown(Keys.Escape))
            Exit();

        // Reset: R key or click button
        bool rPressed = kb.IsKeyDown(Keys.R) && !_prevKb.IsKeyDown(Keys.R);
        bool buttonClicked = mouse.LeftButton == ButtonState.Pressed
            && _prevMouse.LeftButton == ButtonState.Released
            && _resetButtonRect.Contains(mouse.Position);

        if (rPressed || buttonClicked)
        {
            LoadLevel();
            _prevKb = kb;
            _prevMouse = mouse;
            return;
        }

        _tank.Update(gameTime, kb, _terrain);
        _powerBar.Update(gameTime, kb);

        if (_powerBar.JustFired && !_projectile.Active)
        {
            float speed = _powerBar.Power * _powerBar.MaxSpeed;
            _projectile.Fire(_tank.GetCannonTip(), _tank.Angle, speed);
        }

        bool impacted = _projectile.Update(gameTime, _terrain, _structures);
        if (impacted)
        {
            _terrain.CreateCrater((int)_projectile.Position.X, (int)_projectile.Position.Y, _projectile.ExplosionRadius);
            _structures.ApplyExplosion(_projectile.Position, _projectile.ExplosionRadius, _terrain, _tank);
        }

        _structures.Update(gameTime, _terrain);

        _prevKb = kb;
        _prevMouse = mouse;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(135, 206, 235));

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

        _terrain.Draw(_spriteBatch);
        _structures.Draw(_spriteBatch, _pixel);
        _tank.Draw(_spriteBatch, _pixel);
        _projectile.Draw(_spriteBatch, _pixel);
        _powerBar.Draw(_spriteBatch, _pixel, new Vector2(10, 690));
        _ui.Draw(_spriteBatch, _pixel, _tank, _powerBar);

        // Reset button
        var mouse = Mouse.GetState();
        bool hovering = _resetButtonRect.Contains(mouse.Position);
        Color btnColor = hovering ? new Color(80, 80, 80, 200) : new Color(50, 50, 50, 180);
        DrawHelper.DrawRect(_spriteBatch, _pixel, _resetButtonRect, btnColor);
        DrawHelper.DrawRect(_spriteBatch, _pixel,
            new Rectangle(_resetButtonRect.X, _resetButtonRect.Y, _resetButtonRect.Width, 1), Color.Gray);
        DrawHelper.DrawRect(_spriteBatch, _pixel,
            new Rectangle(_resetButtonRect.X, _resetButtonRect.Bottom - 1, _resetButtonRect.Width, 1), Color.Gray);
        DrawHelper.DrawRect(_spriteBatch, _pixel,
            new Rectangle(_resetButtonRect.X, _resetButtonRect.Y, 1, _resetButtonRect.Height), Color.Gray);
        DrawHelper.DrawRect(_spriteBatch, _pixel,
            new Rectangle(_resetButtonRect.Right - 1, _resetButtonRect.Y, 1, _resetButtonRect.Height), Color.Gray);
        // "R" icon - circular arrow hint
        Vector2 btnCenter = new Vector2(_resetButtonRect.Center.X, _resetButtonRect.Center.Y);
        DrawHelper.DrawCircleFilled(_spriteBatch, _pixel, btnCenter - new Vector2(20, 0), 6f, Color.White);
        DrawHelper.DrawLine(_spriteBatch, _pixel,
            btnCenter - new Vector2(20, -6), btnCenter - new Vector2(14, -6), Color.White, 2f);
        DrawHelper.DrawLine(_spriteBatch, _pixel,
            btnCenter - new Vector2(20, -6), btnCenter - new Vector2(20, -1), Color.White, 2f);
        // "RESET" text as simple lines (R shape)
        DrawResetLabel(_spriteBatch, _pixel, (int)btnCenter.X - 6, (int)btnCenter.Y - 5);

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawResetLabel(SpriteBatch sb, Texture2D pixel, int x, int y)
    {
        // Simple pixel-art "RST" label
        Color c = Color.White;
        // R
        DrawHelper.DrawRect(sb, pixel, new Rectangle(x, y, 1, 10), c);
        DrawHelper.DrawRect(sb, pixel, new Rectangle(x, y, 5, 1), c);
        DrawHelper.DrawRect(sb, pixel, new Rectangle(x + 5, y, 1, 5), c);
        DrawHelper.DrawRect(sb, pixel, new Rectangle(x, y + 4, 5, 1), c);
        DrawHelper.DrawLine(sb, pixel, new Vector2(x + 2, y + 5), new Vector2(x + 6, y + 10), c, 1f);
        // S
        x += 8;
        DrawHelper.DrawRect(sb, pixel, new Rectangle(x, y, 5, 1), c);
        DrawHelper.DrawRect(sb, pixel, new Rectangle(x, y, 1, 5), c);
        DrawHelper.DrawRect(sb, pixel, new Rectangle(x, y + 4, 5, 1), c);
        DrawHelper.DrawRect(sb, pixel, new Rectangle(x + 4, y + 4, 1, 6), c);
        DrawHelper.DrawRect(sb, pixel, new Rectangle(x, y + 9, 5, 1), c);
        // T
        x += 8;
        DrawHelper.DrawRect(sb, pixel, new Rectangle(x, y, 7, 1), c);
        DrawHelper.DrawRect(sb, pixel, new Rectangle(x + 3, y, 1, 10), c);
    }
}
