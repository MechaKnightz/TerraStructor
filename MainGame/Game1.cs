﻿using System;
using System.Collections.Generic;
using System.Linq;
using GeonBit.UI;
using GeonBit.UI.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Lidgren.Network;
using Lidgren;
using Library;
using Library.PopupHandler;
using MonoGame.Extended;

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
        private InputHelper _inputHelper;
        private SpriteFont _nameFont;
        private string _tempPassString;

        private World _world = new World();

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

        public Game1()
        {
            Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }
        protected override void Initialize()
        {
            UserInterface.Initialize(Content, "custom");
            UserInterface.UseRenderTarget = true;
            Paragraph.BaseSize = 1.0f;

            var form = (System.Windows.Forms.Form)System.Windows.Forms.Control.FromHandle(this.Window.Handle);
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
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            //if (!IsActive) return;
            
            switch (_state)
            {
                    case GameState.MainMenu:
                        break;
                    case GameState.MainGame:
                        GameLogic(gameTime);
                        break;
            }

            MessageHandler.Update();
            UserInterface.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            UserInterface.Draw(_spriteBatch);

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
            }
            _spriteBatch.End();

            UserInterface.DrawMainRenderTarget(_spriteBatch);

            _spriteBatch.Begin();
            MessageHandler.Draw(_spriteBatch);
            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private void DrawMainMenu(GameTime gameTime)
        {
            _spriteBatch.GraphicsDevice.Clear(Color.White);
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
            SetCamera(_camera);
            _inputManager.Update();
        }

        private void SetCamera(Camera2D camera)
        {
            var localPlayer = _netManager.World.Players.FirstOrDefault(x => x.Username == _netManager.Username);

            if (localPlayer == null) return;

            _camera.Position = new Vector2(localPlayer.X, localPlayer.Y) - _halfScreen;
        }

        private void DrawGame(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);
            foreach (var circle in _netManager.World.Circles)
            {
                DrawCircle(circle, Color.Red);
            }
            var localPlayer = _netManager.World.Players.FirstOrDefault(x => x.Username == _netManager.Username);
            foreach (var player in _netManager.World.Players)
            {
                if(player.Username == localPlayer.Username) continue;
                DrawPlayer(player, Color.LightBlue);
            }
            DrawPlayer(localPlayer, Color.Blue);
            foreach (var shot in _netManager.World.Shots)
            {

                DrawCircle(new Circle(shot.Radius, shot.X, shot.Y), Color.Pink);
            }
            foreach (var shot in _netManager.LocalWorld.Shots)
            {
                DrawCircle(new Circle(shot.Radius, shot.X, shot.Y), Color.Pink);
            }
        }
        private void OldStateChange(GameState state)
        {
            UserInterface.Clear();
        }

        private void ChangeState(GameState state)
        {
            switch (state)
            {
                case GameState.MainMenu:
                    var mainMenuPanel = new Panel(new Vector2(300, 500));
                    UserInterface.AddEntity(mainMenuPanel);

                    var mainMenuPlayButton = new Button("Play");
                    mainMenuPlayButton.ButtonParagraph.Scale = 0.5f;
                    mainMenuPlayButton.OnClick = (Entity btn) =>
                    {
                        State = GameState.ConnectMenu;
                    };
                    mainMenuPanel.AddChild(mainMenuPlayButton);

                    var mainMenuRegisterButton = new Button("Register");
                    mainMenuRegisterButton.ButtonParagraph.Scale = 0.5f;
                    mainMenuRegisterButton.OnClick = (Entity btn) =>
                    {
                        State = GameState.RegisterMenu;
                    };
                    mainMenuPanel.AddChild(mainMenuRegisterButton);
                    break;
                case GameState.ConnectMenu:
                    var connectMenuPanel = new Panel(new Vector2(500, 500));
                    UserInterface.AddEntity(connectMenuPanel);

                    var backButton = new Button("Back");
                    backButton.ButtonParagraph.Scale = 0.5f;
                    backButton.OnClick = (Entity btn) =>
                    {
                        ChangeState(GameState.MainMenu);
                    };
                    connectMenuPanel.AddChild(backButton);

                    TextInput nameText = new TextInput(false);
                    nameText.PlaceholderText = "Enter Username";
                    connectMenuPanel.AddChild(nameText);

                    TextInput passText = new TextInput(false);
                    passText.PlaceholderText = "Enter password";
                    connectMenuPanel.AddChild(passText);
                    passText.OnValueChange = entity =>
                    {
                        if (_tempPassString == null) _tempPassString = passText.Value;
                        if (_tempPassString.Length > passText.Value.Length)
                        {
                            _tempPassString = _tempPassString.Substring(0, passText.Value.Length);
                        }
                        var tempString = passText.Value;
                        passText.Value = new string('*', passText.Value.Length);
                        tempString = tempString.Replace("*", "");
                        _tempPassString += tempString;
                    };

                    TextInput ipText = new TextInput(false);
                    ipText.PlaceholderText = "Enter host IP";
                    connectMenuPanel.AddChild(ipText);

                    TextInput portText = new TextInput(false);
                    portText.PlaceholderText = "Enter host port";
                    connectMenuPanel.AddChild(portText);

                    //temp

                    nameText.Value = "mecha";
                    passText.Value = "test";
                    ipText.Value = "127.0.0.1";
                    portText.Value = "9911";

                    //temp end

                    var connectButton = new Button("Connect");
                    connectButton.ButtonParagraph.Scale = 0.5f;
                    connectButton.OnClick = (Entity btn) =>
                    {
                        if (nameText.Value != "" && ipText.Value != "" && portText.Value != "")
                        {
                            if (_tempPassString == null) _tempPassString = passText.Value;
                            string temp;
                            if (_netManager.Initialize(nameText.Value, _tempPassString, ipText.Value, int.Parse(portText.Value), out temp))
                            {
                                State = GameState.RoomConnectMenu;
                            }
                            else
                            {
                                portText.Value = temp;
                            }
                        }
                    };
                    connectMenuPanel.AddChild(connectButton);
                    break;
                case GameState.RoomConnectMenu:
                    var connectRoomMenuPanel = new Panel(new Vector2(500, 500));
                    UserInterface.AddEntity(connectRoomMenuPanel);

                    TextInput roomNameText = new TextInput(false);
                    roomNameText.PlaceholderText = "Enter room name";

                    connectRoomMenuPanel.AddChild(roomNameText);

                    var connectRoomButton = new Button("Play");
                    connectRoomButton.ButtonParagraph.Scale = 0.5f;

                    connectRoomMenuPanel.AddChild(connectRoomButton);
                    break;
                case GameState.RegisterMenu:
                    DrawRegisterMenu();
                    break;
            }
        }

        void DrawPlayer(Player player, Color color)
        {

            var tempRect = new Rectangle(
                Convert.ToInt16(player.X),
                Convert.ToInt16(player.Y),
                Convert.ToInt16(50 * 2),
                Convert.ToInt16(50 * 2));

            var origin = new Vector2(_playerTexture.Width / 2f, _playerTexture.Height / 2f);

            _spriteBatch.Draw(_playerTexture, destinationRectangle: tempRect, color: Color.Blue, rotation: player.Rotation, origin: origin);

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

            var connectMenuPanel = new Panel(new Vector2(500, 500));
            UserInterface.AddEntity(connectMenuPanel);

            var backButton = new Button("Back");
            backButton.ButtonParagraph.Scale = 0.5f;
            backButton.OnClick = (Entity btn) =>
            {
                ChangeState(GameState.MainMenu);
            };
            connectMenuPanel.AddChild(backButton);

            TextInput nameText = new TextInput(false);
            nameText.PlaceholderText = "Enter Username";
            connectMenuPanel.AddChild(nameText);

            TextInput passText = new TextInput(false);
            passText.PlaceholderText = "Enter password";
            connectMenuPanel.AddChild(passText);
            passText.OnValueChange = entity =>
            {
                if (_tempPassString == null) _tempPassString = passText.Value;
                if (_tempPassString.Length > passText.Value.Length)
                {
                    _tempPassString = _tempPassString.Substring(0, passText.Value.Length);
                }
                var tempString = passText.Value;
                passText.Value = new string('*', passText.Value.Length);
                tempString = tempString.Replace("*", "");
                _tempPassString += tempString;
            };

            TextInput ipText = new TextInput(false);
            ipText.PlaceholderText = "Enter host IP";
            connectMenuPanel.AddChild(ipText);

            TextInput portText = new TextInput(false);
            portText.PlaceholderText = "Enter host port";
            connectMenuPanel.AddChild(portText);

            var connectButton = new Button("Register");
            connectButton.ButtonParagraph.Scale = 0.5f;
            connectButton.OnClick = (Entity btn) =>
            {
                MessageHandler.CreateMessage("LUL");
                if (nameText.Value != "" && ipText.Value != "" && portText.Value != "")
                {
                    if (_tempPassString == null) _tempPassString = passText.Value;
                    string temp;
                    if (_netManager.Register(nameText.Value, _tempPassString, ipText.Value, int.Parse(portText.Value),
                        out temp))
                        MessageHandler.CreateMessage(temp);
                }
            };
            connectMenuPanel.AddChild(connectButton);

            //temp
            ipText.Value = "127.0.0.1";
            portText.Value = "9911";
            //temp end
        }
    }
}
