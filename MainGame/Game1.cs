﻿using System;
using System.Collections.Generic;
using System.Linq;
using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Generated;
using EmptyKeys.UserInterface.Mvvm;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Library;
using Library.MessageHook;
using Library.Messenger;
using Library.PopupHandler;
using MonoGame.Extended;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using TextBox = Library.MessageHook.TextBox;
using Microsoft.Xna.Framework.Content;

namespace MainGame
{
    public class Game1 : Game
    {
        GraphicsDeviceManager Graphics;
        SpriteBatch _spriteBatch;
        private Texture2D _circleTexture, _playerTexture;
        private NetManager _netManager;
        private InputManager _inputManager;
        private Camera2D _camera;
        private Matrix _viewMatrix;
        private Vector2 _halfScreen;
        private SpriteFont _nameFont;
        private string _tempPassString;
        private Texture2D _tileset;
        private Texture2D _redPixel;
        private Vector2 _chatPadding = new Vector2(5, 5);
        private SpriteFont _chatFont;
        private KeyboardDispatcher _keyboardDispatcher;
        private TextBox _chatTextBox;

        float timer = 15;
        const float TIMER = 15;

        private GameState _state;
        private GameState State
        {
            set
            {
                OldStateChange(_state);
                _state = value;
                ChangeState(_state);
            }
            get { return _state; }
        }

        private int nativeScreenWidth;
        private int nativeScreenHeight;

        private UIRoot root;

        public Game1() : base()
        {
            Graphics = new GraphicsDeviceManager(this);

            Graphics.PreferredBackBufferWidth = 1920;
            Graphics.PreferredBackBufferHeight = 1080;
            Graphics.DeviceCreated += graphics_DeviceCreated;
            Graphics.PreparingDeviceSettings += graphics_PreparingDeviceSettings;

            Content.RootDirectory = "Content";

            //ServiceManager.Instance.AddService<IGameService>(new GameService(this));
        }

        void graphics_DeviceCreated(object sender, EventArgs e)
        {
            Engine engine = new MonoGameEngine(GraphicsDevice, nativeScreenWidth, nativeScreenHeight);
        }

        private void graphics_PreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            nativeScreenWidth = Graphics.PreferredBackBufferWidth;
            nativeScreenHeight = Graphics.PreferredBackBufferHeight;

            Graphics.PreferMultiSampling = true;
            Graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Graphics.SynchronizeWithVerticalRetrace = true;
            Graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
            e.GraphicsDeviceInformation.PresentationParameters.MultiSampleCount = 8;
        }

        protected override void Initialize()
        {
            IsMouseVisible = true;

            _keyboardDispatcher = new KeyboardDispatcher(Window);

            var form = (System.Windows.Forms.Form)System.Windows.Forms.Control.FromHandle(Window.Handle);
            form.Location = new System.Drawing.Point(0, 0);

            Graphics.PreferredBackBufferWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
            Graphics.PreferredBackBufferHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
            Graphics.IsFullScreen = false;
            Graphics.ApplyChanges();

            _netManager = new NetManager();
            _inputManager = new InputManager(_netManager);

            MessageHandler.Initialize(
                Content.Load<Texture2D>("BoxTexture"),
                Content.Load<SpriteFont>("BoxFont"),
                Color.Black);

            _camera = new Camera2D(GraphicsDevice);
            _viewMatrix = _camera.GetViewMatrix();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _halfScreen = new Vector2(Graphics.PreferredBackBufferWidth / 2f, Graphics.PreferredBackBufferHeight / 2f);


            State = GameState.MainMenu;
            _circleTexture = Content.Load<Texture2D>("Circle");
            _playerTexture = Content.Load<Texture2D>("PlayerCircle");
            _nameFont = Content.Load<SpriteFont>("BoxFont");
            _chatFont = Content.Load<SpriteFont>("BoxFont");
            _tileset = Content.Load<Texture2D>("Tileset");
            _redPixel = Content.Load<Texture2D>("RedPixel");
            _chatTextBox = new TextBox(Content.Load<Texture2D>("TextBox"), Content.Load<Texture2D>("Caret"), _chatFont)
            {
                X = 0,
                Y = 0,
                Width = 300
            };

            SpriteFont font = Content.Load<SpriteFont>("Segoe_UI_15_Bold");
            FontManager.DefaultFont = Engine.Instance.Renderer.CreateFont(font);
            root = new Root();

            FontManager.Instance.LoadFonts(Content);
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            if (!IsActive) return;
            _inputManager.KeyState = Keyboard.GetState();

            switch (_state)
            {

                case GameState.MainMenu:
                    break;
                case GameState.MainGame:
                    GameLogic(gameTime);
                    break;
                case GameState.ChatOpen:
                    ChatLogic(gameTime);
                    break;
            }
            
            root.UpdateInput(gameTime.ElapsedGameTime.TotalMilliseconds);
            root.UpdateLayout(gameTime.ElapsedGameTime.TotalMilliseconds);
            _inputManager.OldKeyState = Keyboard.GetState();
            MessageHandler.Update();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {

            _viewMatrix = _camera.GetViewMatrix();
            _spriteBatch.Begin(transformMatrix: _viewMatrix);

            switch (_state)
            {
                case GameState.MainMenu:
                    DrawMainMenu(gameTime);
                    break;
                case GameState.MainGame:
                    DrawGame(gameTime);
                    break;
                case GameState.ChatOpen:
                    DrawGame(gameTime);
                    DrawChatOpen(gameTime);
                    break;
            }
            _spriteBatch.End();

            root.Draw(gameTime.ElapsedGameTime.TotalMilliseconds);
            
            _spriteBatch.Begin();
            MessageHandler.Draw(_spriteBatch);
            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private void DrawMainMenu(GameTime gameTime)
        {
            _spriteBatch.GraphicsDevice.Clear(Color.Red);
        }

        private void GameLogic(GameTime gameTime)
        {
            var elapsed = (float)gameTime.ElapsedGameTime.Milliseconds;
            timer -= elapsed;
            if (timer < 0)
            {
                _netManager.CheckServerMessages(gameTime);
                timer = TIMER;
            }
            if (_inputManager.IsKeyClicked(Keys.Enter))
                State = GameState.ChatOpen;
            SetCamera(_camera);
            _inputManager.Update(_camera);

            InputPredictionUpdate(gameTime);
        }

        private void InputPredictionUpdate(GameTime gameTime)
        {
            var player = _netManager.CurrentRoom.Players.FirstOrDefault(x => x.Username == _netManager.Username);

            if (player == null) return;

            InputHandler.MoveWithAdjust(new Vector2(player.VelocityX, 0), player, _netManager.CurrentRoom.Map);

            if (player.VelocityX >= player.GravityX) player.VelocityX -= player.GravityX;
            else if (player.VelocityX > 0) player.VelocityX = 0;
            else if (player.VelocityX <= -player.GravityX) player.VelocityX += player.GravityX;
            else if (player.VelocityX < 0) player.VelocityX = 0;
            //player.VelocityX = 0;


            player.VelocityY += player.Gravity;

            if (!InputHandler.MoveWithAdjust(new Vector2(0, player.VelocityY), player, _netManager.CurrentRoom.Map) && player.VelocityY > 0)
            {
                player.VelocityY = 0;
                player.OnGround = true;
                player.IsJumping = false;
            }
            else if (!InputHandler.MoveWithAdjust(new Vector2(0, player.VelocityY), player, _netManager.CurrentRoom.Map) && player.VelocityY < 0)
            {
                player.VelocityY = 0;
            }
            else player.OnGround = false;
        }

        private void SetCamera(Camera2D camera)
        {
            var localPlayer = _netManager.CurrentRoom.Players.FirstOrDefault(x => x.Username == _netManager.Username);

            if (localPlayer == null) return;

            var tempPos = new Vector2(localPlayer.X, localPlayer.Y) - _halfScreen;

            tempPos.X = UsefulMethods.Clamp(tempPos.X,
                0,
                (float)_netManager.CurrentRoom.Map.MapSize * Map.TileSize - _halfScreen.X * 2);

            tempPos.Y = UsefulMethods.Clamp(tempPos.Y,
                0,
                (float) _netManager.CurrentRoom.Map.MapSize * Map.TileSize - _halfScreen.Y * 2);

            _camera.Position = tempPos;
        }

        private void DrawGame(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            DrawMap(gameTime);
            Player localPlayer = new Player();
            for (int i = 0; i < _netManager.CurrentRoom.Players.Count; i++)
            {
                DrawPlayer(_netManager.CurrentRoom.Players[i], Color.Red);
                if (_netManager.CurrentRoom.Players[i].Username == _netManager.Username)
                    localPlayer = _netManager.CurrentRoom.Players[i];
            }

            _spriteBatch.DrawString(_nameFont, new Vector2(localPlayer.X, localPlayer.Y).ToString(), _camera.ScreenToWorld(0, 0), Color.White);
            _spriteBatch.DrawString(_nameFont, gameTime.TotalGameTime.ToString(), _camera.ScreenToWorld(0, 50), Color.White);
            _spriteBatch.DrawString(_nameFont, _netManager.CurrentRoom.Name, _camera.ScreenToWorld(0, 100), Color.White);

            DrawMessages(_spriteBatch, _netManager.ChatMessages, _camera);
        }

        private void DrawMap(GameTime gameTime)
        {
            _netManager.CurrentRoom.Map.Draw(_spriteBatch, _tileset, _nameFont);
        }

        private void OldStateChange(GameState state)
        {
        }

        private void ChatLogic(GameTime gameTime)
        {
            _chatTextBox.Update(gameTime);
            var elapsed = (float)gameTime.ElapsedGameTime.Milliseconds;
            timer -= elapsed;
            if (timer < 0)
            {
                _netManager.CheckServerMessages(gameTime);
                timer = TIMER;
            }
            SetCamera(_camera);
            if (_inputManager.IsKeyClicked(Keys.Escape))
                State = GameState.MainGame;
            if (_inputManager.IsKeyClicked(Keys.Enter))
            {
                ChatMessage();
            }
            _chatTextBox.X = Convert.ToInt16(_camera.ScreenToWorld(_chatPadding.X, 0).X);
            var value = _halfScreen.Y * 2 - _chatPadding.Y - _chatFont.MeasureString("T").Y;
            _chatTextBox.Y = Convert.ToInt16(_camera.ScreenToWorld(0, value).Y);
        }

        private void DrawChatOpen(GameTime gameTime)
        {
            _chatTextBox.Draw(_spriteBatch, gameTime);
        }

        void DrawPlayer(Player player, Color color)
        {

            var tempRect = new Rectangle(
                Convert.ToInt16(player.X),
                Convert.ToInt16(player.Y),
                Convert.ToInt16(Player.Width),
                Convert.ToInt16(Player.Height));

            _spriteBatch.Draw(_redPixel, tempRect, color);

            _spriteBatch.DrawString(_nameFont,
                player.Username,
                new Vector2(player.X - _nameFont.MeasureString(player.Username).X / 2f,
                player.Y - _nameFont.MeasureString(player.Username).Y / 2f),
                Color.White);

            _spriteBatch.DrawString(_nameFont,
                player.Health.ToString(),
                new Vector2(player.X - _nameFont.MeasureString(player.Health.ToString()).X / 2f,
                    player.Y - _nameFont.MeasureString(player.Health.ToString()).Y / 2f - 50),
                Color.White);
        }

        public void DrawCircle(Circle circle, Color color)
        {
            if (circle.Radius < 1) return;

            var diameter = circle.Radius * 2;

            var tempRect = new Rectangle(
                Convert.ToInt16(circle.X - circle.Radius),
                Convert.ToInt16(circle.Y - circle.Radius),
                Convert.ToInt16(diameter),
                Convert.ToInt16(diameter));

            _spriteBatch.Draw(_circleTexture, tempRect, color);
        }

        public void DrawRegisterMenu()
        { 
        }

        private void ChangeState(GameState state)
        {
        }

        private void DrawMessages(SpriteBatch spriteBatch, List<Library.Messenger.Message> messages, Camera2D camera)
        {
            var length = messages.Count - 10;
            if (messages.Count <= 10)
                length = 0;
            string tempMeasureString = "T";

            if (State == GameState.ChatOpen)
            {
                int j = 1;
                for (int i = messages.Count - 1; i >= length; i--)
                {
                    var drawPos =
                        camera.ScreenToWorld(new Vector2(_chatPadding.X, _halfScreen.Y * 2) -
                                             new Vector2(0,
                                                 (j + 1) *
                                                 (_chatPadding.Y + _chatFont.MeasureString(tempMeasureString).Y)));
                    var message = messages[i].GetMessage();

                    spriteBatch.DrawString(_chatFont, message, drawPos + Vector2.One, Color.Black);

                    spriteBatch.DrawString(
                        _chatFont,
                        message,
                        drawPos,
                        Color.White);

                    j++;
                }
            }
            else
            {
                int j = 0;
                for (int i = messages.Count - 1; i >= length; i--)
                {
                    if (messages[i].Timestamp + TimeSpan.FromSeconds(5) < DateTime.Now) break;
                    var drawPos =
                        camera.ScreenToWorld(new Vector2(_chatPadding.X, _halfScreen.Y * 2) -
                                             new Vector2(0,
                                                 (j + 1) *
                                                 (_chatPadding.Y + _chatFont.MeasureString(tempMeasureString).Y)));
                    var message = messages[i].GetMessage();

                    spriteBatch.DrawString(_chatFont, message, drawPos + Vector2.One, Color.Black);

                    spriteBatch.DrawString(
                        _chatFont,
                        message,
                        drawPos,
                        Color.White);

                    j++;
                }
            }
        }

        private void ChatMessage()
        {
            if (_chatTextBox.Text == "") return;
            _netManager.SendMessage(_chatTextBox.Text);
            State = GameState.MainGame;
        }
    }
}
