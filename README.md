# Steropes-Map-Widget
A map component you can add to a Steropes interface.

The MapCode.cs file is the adapted Tiled-XNA repository code. This project required several changes to the Tiled-XNA code so isn't interchangable, the original Tiled-XNA code draws to the spritebatch however this project needs to draw to the steropes UI. The MapWidget.cs is the Steropes UI Widget, it inherits Image to draw the map.

The example project includes a file called MainMenu.cs which includes classes for 2 panels. Each panel has a button to switch between the two. The last bit i added was map scaling, so keys D,F,G will scale the map to different sizes.

To initialise the MapWidget you need to pass the name of the map to load, the Game, the graphics, the object layer of the focus object, the name of the focus object.
