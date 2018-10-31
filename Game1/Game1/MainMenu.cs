using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
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
    class Playing:LayeredPane
    {
        public MapWidget mapWidget;

        public Playing(IUIStyle s, Game1 parent, GraphicsDeviceManager man) : base(s)
        {
            mapWidget = new MapWidget(s);
            var exit = new Button(s, "Exit")
            {
                Anchor = AnchoredRect.CreateFixed(0, 0, 100, 60),
                Color = Color.Aquamarine,
                OnActionPerformed = (se, a) =>
                {
                    parent.State = Game1.GameState.MainMenu;
                }
            };
            this.Add(mapWidget);
            this.Add(exit);
        }
    }

    class MainMenu:LayeredPane
    {
        public MainMenu(IUIStyle s, Game1 parent) : base(s)
        {
            var lab = new Label(s, "Welcome to the game")
            {
                Anchor = AnchoredRect.CreateCentered()
            };

            var play = new Button(s, "Play")
            {
                Anchor = AnchoredRect.CreateFixed(0, 0, 100, 60),
                Color = Color.Aquamarine,
                OnActionPerformed = (se, a) =>
                {
                    parent.State = Game1.GameState.Playing;
                }
            };

            this.Add(lab);
            this.Add(play);
        }
    }
}
