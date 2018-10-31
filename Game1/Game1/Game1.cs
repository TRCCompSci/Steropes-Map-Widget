using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
using System;
using Squared.Tiled;
using System.Collections.Generic;
using Steropes.UI;
using Steropes.UI.Components;
using Steropes.UI.Input;
using Steropes.UI.Styles;
using Steropes.UI.Util;
using Steropes.UI.Widgets;
using Steropes.UI.Widgets.Container;
using Steropes.UI.Widgets.TextWidgets;

namespace Game1
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        MapWidget map;
        Vector2 viewportPosition;

        IUIManager uiManager;

        public enum GameState
        {
            MainMenu,
            Playing
        }

        private GameState state = GameState.MainMenu;

        public GameState State
        {
            get { return state; }
            set
            {
                state = value;
                //switchstate
            }
        }

        //Screens
        MainMenu main;
        Playing play;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1024;
            graphics.PreferredBackBufferHeight = 600;
            graphics.ApplyChanges();
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            //setup Steropes interface
            uiManager = UIManagerComponent.CreateAndInit(this, new InputManager(this), "Content").Manager;            
            var styleSystem = uiManager.UIStyle;
            var styles = styleSystem.LoadStyles("Content/UI/Metro/style.xml", "UI/Metro", GraphicsDevice);
            styleSystem.StyleResolver.StyleRules.AddRange(styles);

            //my UI panels
            main = new MainMenu(styleSystem, this);
            play = new Playing(styleSystem, this, graphics);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            //init mapwidget in playing menu
            play.mapWidget.Init("Map2.tmx", this, graphics, "3objects","player");

            //link mapwidget in playing menu to map variable in here
            map = play.mapWidget;

            //access objects and add texture
            map.CurrentMap.ObjectGroups["3objects"].Objects["player"].Texture = Content.Load<Texture2D>("hero");
            map.CurrentMap.ObjectGroups["3objects"].Objects["coin_1"].Texture = Content.Load<Texture2D>("coin");

        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            switch (state)
            {
                case GameState.MainMenu:
                    uiManager.Root.Content = main;
                    break;
                case GameState.Playing:
                    uiManager.Root.Content = play;
                    viewportPosition = new Vector2(map.CurrentMap.ObjectGroups["3objects"].Objects["player"].X - (graphics.PreferredBackBufferWidth / 2), map.CurrentMap.ObjectGroups["3objects"].Objects["player"].Y - (graphics.PreferredBackBufferHeight / 2));
                    break;
            }

            base.Update(gameTime);
        }


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    
    }
}
