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
using Steropes.UI.Platform;
using Steropes.UI.Styles;
using Steropes.UI.Util;
using Steropes.UI.Widgets;
using Steropes.UI.Widgets.Container;
using Steropes.UI.Widgets.TextWidgets;

// map widget start
namespace Game1
{
    public class MapWidget : Image
    {
        private Map map;

        public Map CurrentMap
        {
            get { return map; }
            set { map = value; }
        }

        private Squared.Tiled.Object cameraObject;

        public Squared.Tiled.Object CameraObject
        {
            get { return cameraObject; }
            set { cameraObject = value; }
        }

        private Vector2 viewportPosition;

        private Game1 parent;
        private GraphicsDeviceManager g;
        
        public MapWidget(IUIStyle style) : base(style)
        {
            // constructor
        }

        public void Init(string mapname, Game1 game, GraphicsDeviceManager man, string layer, string obj)
        {
            parent = game;
            g = man;
            map = Map.Load(Path.Combine(game.Content.RootDirectory, mapname), game.Content);
            cameraObject = map.ObjectGroups[layer].Objects[obj];
        }

        public void Update(GraphicsDeviceManager graphics)
        {
            
        }

        protected override void DrawWidget(IBatchedDrawingService drawingService)
        {
            viewportPosition = new Vector2(cameraObject.X - (g.PreferredBackBufferWidth / 2), cameraObject.Y- (g.PreferredBackBufferHeight / 2));
            map.Draw(drawingService, new Rectangle(0, 0, drawingService.GraphicsDevice.Viewport.Width, drawingService.GraphicsDevice.Viewport.Height), viewportPosition);
        }

    }
}
