﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
using Squared.Tiled;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steropes.UI;
using Steropes.UI.Components;
using Steropes.UI.Input;
using Steropes.UI.Platform;
using Steropes.UI.Styles;
using Steropes.UI.Util;
using Steropes.UI.Widgets;
using Steropes.UI.Widgets.Container;
using Steropes.UI.Widgets.TextWidgets;

using System.Collections;

using System.Xml;
using System.Xml.Schema;
using Microsoft.Xna.Framework.Content;
using System.IO.Compression;
using System.Globalization;


//From here is the Tiled-XNA repository from github, it required several changes to draw to the interface and not the spritebatch
//I have also created a list of tile textures which is created for each layer at the moment on first update, it unfortunately does this
//for each layer, i will change this so that the tileset stores the individual images instead of the layer
//
//Actual map widget starts on line 1108

namespace Squared.Tiled
{
    public class Isometric
    {
        public static Vector2 TwoDToIso(Vector2 pt)
        {
            Vector2 tempPt = new Vector2(0, 0);
            tempPt.X = pt.X - pt.Y;
            tempPt.Y = (pt.X + pt.Y) / 2;
            return (tempPt);
        }

        /**
         * convert a 2d point to specific tile row/column
         * */
        public static Vector2 GetTileCoordinates(Vector2 pt, int tileHeight)
        {
            Vector2 tempPt = new Vector2(0, 0);
            tempPt.X = (int)Math.Floor(pt.X / tileHeight);
            tempPt.Y = (int)Math.Floor(pt.Y / tileHeight);
            return (tempPt);
        }

        /**
         * convert specific tile row/column to 2d point
         * */
        public static Vector2 Get2dFromTileCoordinates(Vector2 pt, int tileHeight)
        {
            Vector2 tempPt = new Vector2(0, 0);
            tempPt.X = pt.X * tileHeight;
            tempPt.Y = pt.Y * tileHeight;
            return (tempPt);
        }
    }
    public class Tileset
    {
        public class TilePropertyList : Dictionary<string, string>
        {
        }

        public Texture2D[] tiles = null;

        public string Name;
        public int FirstTileID;
        public int TileWidth;
        public int TileHeight;
        public int Spacing;
        public int Margin;
        public Dictionary<int, TilePropertyList> TileProperties = new Dictionary<int, TilePropertyList>();
        public string Image;
        protected Texture2D _Texture;
        protected int _TexWidth, _TexHeight;

        internal static Tileset Load(XmlReader reader)
        {
            var result = new Tileset();

            result.Name = reader.GetAttribute("name");
            result.FirstTileID = int.Parse(reader.GetAttribute("firstgid"));
            result.TileWidth = int.Parse(reader.GetAttribute("tilewidth"));
            result.TileHeight = int.Parse(reader.GetAttribute("tileheight"));
            int.TryParse(reader.GetAttribute("margin"), out result.Margin);
            int.TryParse(reader.GetAttribute("spacing"), out result.Spacing);

            int currentTileId = -1;

            while (reader.Read())
            {
                var name = reader.Name;

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (name)
                        {
                            case "image":
                                result.Image = reader.GetAttribute("source");
                                break;
                            case "tile":
                                currentTileId = int.Parse(reader.GetAttribute("id"));
                                break;
                            case "property":
                                {
                                    TilePropertyList props;
                                    if (!result.TileProperties.TryGetValue(currentTileId, out props))
                                    {
                                        props = new TilePropertyList();
                                        result.TileProperties[currentTileId] = props;
                                    }

                                    props[reader.GetAttribute("name")] = reader.GetAttribute("value");
                                }
                                break;
                        }

                        break;
                    case XmlNodeType.EndElement:
                        break;
                }
            }

            return result;
        }

        public TilePropertyList GetTileProperties(int index)
        {
            index -= FirstTileID;

            if (index < 0)
                return null;

            TilePropertyList result = null;
            TileProperties.TryGetValue(index, out result);

            return result;
        }

        public Texture2D Texture
        {
            get
            {
                return _Texture;
            }
            set
            {
                _Texture = value;
                _TexWidth = value.Width;
                _TexHeight = value.Height;
            }
        }

        internal bool MapTileToRect(int index, ref Rectangle rect)
        {
            index -= FirstTileID;

            if (index < 0)
                return false;

            int rowSize = _TexWidth / (TileWidth + Spacing);
            int row = index / rowSize;
            int numRows = _TexHeight / (TileHeight + Spacing);
            if (row >= numRows)
                return false;

            int col = index % rowSize;

            rect.X = col * TileWidth + col * Spacing + Margin;
            rect.Y = row * TileHeight + row * Spacing + Margin;
            rect.Width = TileWidth;
            rect.Height = TileHeight;
            return true;
        }
    }

    public class Layer
    {
        /*
         * High-order bits in the tile data indicate tile flipping
         */
        private const uint FlippedHorizontallyFlag = 0x80000000;
        private const uint FlippedVerticallyFlag = 0x40000000;
        private const uint FlippedDiagonallyFlag = 0x20000000;

        internal const byte HorizontalFlipDrawFlag = 1;
        internal const byte VerticalFlipDrawFlag = 2;
        internal const byte DiagonallyFlipDrawFlag = 4;

        public SortedList<string, string> Properties = new SortedList<string, string>();
        public struct TileInfo
        {
            public Texture2D Texture;
            public Rectangle Rectangle;
        }

        public string Name;
        public int Width, Height;
        public float Opacity = 1;
        public int[] Tiles;
        public byte[] FlipAndRotate;
        private TileInfo[] _TileInfoCache = null;
        //private Texture2D[] tiles = null;


        internal static Layer Load(XmlReader reader)
        {
            CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.CurrencyDecimalSeparator = ".";
            var result = new Layer();

            if (reader.GetAttribute("name") != null)
            {
                result.Name = reader.GetAttribute("name");
            }
            if (reader.GetAttribute("width") != null)
            {
                result.Width = int.Parse(reader.GetAttribute("width"));
            }
            if (reader.GetAttribute("height") != null)
            {
                result.Height = int.Parse(reader.GetAttribute("height"));
            }
            if (reader.GetAttribute("opacity") != null)
            {
                result.Opacity = float.Parse(reader.GetAttribute("opacity"), NumberStyles.Any, ci);
            }
            result.Tiles = new int[result.Width * result.Height];
            result.FlipAndRotate = new byte[result.Width * result.Height];

            while (!reader.EOF)
            {
                var name = reader.Name;

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (name)
                        {

                            case "data":
                                {
                                    if (reader.GetAttribute("encoding") != null)
                                    {
                                        var encoding = reader.GetAttribute("encoding");
                                        var compressor = reader.GetAttribute("compression");
                                        switch (encoding)
                                        {
                                            case "csv":
                                                string csvdata = "";
                                                if (reader.Value != null)
                                                {
                                                    csvdata = (string)reader.ReadInnerXml();
                                                    int total = result.Tiles.Length;
                                                    var dump = csvdata.Split(',');

                                                    for (int i = 0; i < total; i++)
                                                    {
                                                        if (dump[i] != null)
                                                            result.Tiles[i] = int.Parse(dump[i]);
                                                        else
                                                            result.Tiles[i] = 0;
                                                    }
                                                }
                                                else
                                                    Console.WriteLine("no value");
                                                break;

                                            case "base64":
                                                {
                                                    int dataSize = (result.Width * result.Height * 4) + 1024;
                                                    var buffer = new byte[dataSize];
                                                    reader.ReadElementContentAsBase64(buffer, 0, dataSize);

                                                    Stream stream = new MemoryStream(buffer, false);
                                                    if (compressor == "gzip")
                                                        stream = new GZipStream(stream, CompressionMode.Decompress, false);
                                                    else if (compressor == "zlib")
                                                    {
                                                        var length = buffer.Length - 6;
                                                        byte[] data = new byte[length];
                                                        Array.Copy(buffer, 2, data, 0, length);
                                                        var compressedstream = new MemoryStream(data, false);
                                                        stream = new DeflateStream(compressedstream, CompressionMode.Decompress);
                                                    }

                                                    using (stream)
                                                    using (var br = new BinaryReader(stream))
                                                    {
                                                        for (int i = 0; i < result.Tiles.Length; i++)
                                                        {
                                                            uint tileData = br.ReadUInt32();

                                                            // The data contain flip information as well as the tileset index
                                                            byte flipAndRotateFlags = 0;
                                                            if ((tileData & FlippedHorizontallyFlag) != 0)
                                                            {
                                                                flipAndRotateFlags |= HorizontalFlipDrawFlag;
                                                            }
                                                            if ((tileData & FlippedVerticallyFlag) != 0)
                                                            {
                                                                flipAndRotateFlags |= VerticalFlipDrawFlag;
                                                            }
                                                            if ((tileData & FlippedDiagonallyFlag) != 0)
                                                            {
                                                                flipAndRotateFlags |= DiagonallyFlipDrawFlag;
                                                            }
                                                            result.FlipAndRotate[i] = flipAndRotateFlags;

                                                            // Clear the flip bits before storing the tile data
                                                            tileData &= ~(FlippedHorizontallyFlag |
                                                                          FlippedVerticallyFlag |
                                                                          FlippedDiagonallyFlag);
                                                            result.Tiles[i] = (int)tileData;
                                                        }
                                                    }

                                                    continue;
                                                };

                                            default:
                                                throw new Exception("Unrecognized encoding.");
                                        }
                                    }
                                    else
                                    {
                                        using (var st = reader.ReadSubtree())
                                        {
                                            int i = 0;
                                            while (!st.EOF)
                                            {
                                                switch (st.NodeType)
                                                {
                                                    case XmlNodeType.Element:
                                                        if (st.Name == "tile")
                                                        {
                                                            if (i < result.Tiles.Length)
                                                            {
                                                                if (st.AttributeCount > 0)
                                                                {
                                                                    result.Tiles[i] = int.Parse(st.GetAttribute("gid"));
                                                                }
                                                                else result.Tiles[i] = 0;

                                                                i++;
                                                            }
                                                        }

                                                        break;
                                                    case XmlNodeType.EndElement:
                                                        break;
                                                }

                                                st.Read();
                                            }
                                        }
                                    }
                                    Console.WriteLine("It made it!");
                                }
                                break;
                            case "properties":
                                {
                                    using (var st = reader.ReadSubtree())
                                    {
                                        while (!st.EOF)
                                        {
                                            switch (st.NodeType)
                                            {
                                                case XmlNodeType.Element:
                                                    if (st.Name == "property")
                                                    {
                                                        if (st.GetAttribute("name") != null)
                                                        {
                                                            result.Properties.Add(st.GetAttribute("name"), st.GetAttribute("value"));
                                                        }
                                                    }

                                                    break;
                                                case XmlNodeType.EndElement:
                                                    break;
                                            }

                                            st.Read();
                                        }
                                    }
                                }
                                break;
                        }

                        break;
                    case XmlNodeType.EndElement:
                        break;
                }

                reader.Read();
            }

            return result;
        }

        public int GetTile(int x, int y)
        {
            if ((x < 0) || (y < 0) || (x >= Width) || (y >= Height))
                throw new InvalidOperationException();

            int index = (y * Width) + x;
            return Tiles[index];
        }

        public Texture2D GetTileTexture(int tileNo, Tileset tileset, Texture2D tile)
        {
            Texture2D tilesheet = tileset.Texture;
            Color[] TileTextureData = new Color[tilesheet.Width * tilesheet.Height];

            Rectangle tilehit = new Rectangle(0, 0, 0, 0);
            tileset.MapTileToRect(tileNo, ref tilehit);

            tilesheet.GetData(TileTextureData);

            Color[] test = new Color[tile.Width * tile.Height];

            int count = 0;
            for (int c = tilehit.Top; c < tilehit.Bottom; c++)
            {
                for (int r = tilehit.Left; r < tilehit.Right; r++)
                {
                    Color colorA = TileTextureData[r + (c * tilesheet.Width)];
                    test[count] = colorA;
                    count++;
                }
            }

            tile.SetData(test);

            return tile;
        }

        public Color[] GetTextureData(Texture2D tile)
        {
            Color[] data = new Color[tile.Width * tile.Height];
            tile.GetData(data);
            return data;
        }

        protected void BuildTileInfoCache(IList<Tileset> tilesets)
        {
            Rectangle rect = new Rectangle();
            var cache = new List<TileInfo>();
            int i = 1;

        next:
            for (int t = 0; t < tilesets.Count; t++)
            {
                if (tilesets[t].MapTileToRect(i, ref rect))
                {
                    cache.Add(new TileInfo
                    {
                        Texture = tilesets[t].Texture,
                        Rectangle = rect
                    });
                    i += 1;
                    goto next;
                }
            }

            _TileInfoCache = new TileInfo[cache.Count];
            _TileInfoCache = cache.ToArray();
        }

        public void DrawIso(IBatchedDrawingService d, IList<Tileset> tilesets, Rectangle rectangle, Vector2 viewportPosition, int tileWidth, int tileHeight)
        {

            int i = 0;
            Vector2 destPos = new Vector2(0, 0);

            Vector2 centTile = new Vector2((int)viewportPosition.X / tileHeight, (int)viewportPosition.Y / tileHeight);

            Console.WriteLine(centTile + " " + viewportPosition);

            TileInfo info = new TileInfo();
            if (_TileInfoCache == null)
                BuildTileInfoCache(tilesets);

            foreach (Tileset tileset in tilesets)
            {
                if (tileset.tiles == null)
                {
                    Texture2D[] tiles = new Texture2D[_TileInfoCache.Length];
                    for (int k = 0; k < _TileInfoCache.Length; k++)
                    {
                        Texture2D test = new Texture2D(d.GraphicsDevice, tileWidth, tileHeight);
                        test = GetTileTexture(k + 1, tileset, test);
                        tiles[k] = test;
                    }

                    tileset.tiles = tiles;
                    Console.WriteLine("tiles added");
                }
            }

            for (int y = (int)centTile.Y - (Height / 2); y <= (int)centTile.Y + (Height / 2); y++)
            {
                for (int x = (int)centTile.X - (Width / 2); x <= (int)centTile.X + (Width / 2); x++)
                {

                    destPos.X = (x - centTile.X) * tileHeight;
                    destPos.Y = (y - centTile.Y) * tileHeight;
                    destPos = Isometric.TwoDToIso(destPos);
                    destPos.X += (rectangle.Width) / 2;
                    destPos.Y += (rectangle.Height) / 2;

                    if (destPos.X > (0 - tileWidth) && destPos.X < (rectangle.Width + tileWidth) && destPos.Y > (0 - tileWidth) && destPos.Y < (rectangle.Height + tileHeight))
                    {

                        Vector2 testtile = new Vector2(x, y);

                        if (testtile.X >= 0 && testtile.X < Width && testtile.Y >= 0 && testtile.Y < Height)
                        {

                            i = ((int)testtile.Y * Width) + (int)testtile.X;

                            int index = Tiles[i] - 1;

                            if ((index >= 0) && (index < _TileInfoCache.Length))
                            {
                                d.Draw(new UITexture(tilesets[0].tiles[index]), destPos, null,
                                           Color.White * this.Opacity, 0f, new Vector2(tileWidth / 2f, tileHeight / 2f),
                                           1f, 0f, 0);
                            }
                        }
                    }
                }
            }
        }

        public void Draw(IBatchedDrawingService d, IList<Tileset> tilesets, Rectangle rectangle, Vector2 viewportPosition, int tileWidth, int tileHeight, float scale)
        {
            int i = 0;
            Vector2 destPos = new Vector2(rectangle.Left, rectangle.Top);
            Vector2 viewPos = viewportPosition;

            int minX = (int)Math.Floor(viewportPosition.X / tileWidth);
            int minY = (int)Math.Floor(viewportPosition.Y / tileHeight);
            int maxX = (int)Math.Ceiling((rectangle.Width + viewportPosition.X) / tileWidth);
            int maxY = (int)Math.Ceiling((rectangle.Height + viewportPosition.Y) / tileHeight);

            if (minX < 0)
                minX = 0;
            if (minY < 0)
                minY = 0;
            if (maxX >= Width)
                maxX = Width - 1;
            if (maxY >= Height)
                maxY = Height - 1;

            if (viewPos.X > 0)
            {
                viewPos.X = ((int)Math.Floor(viewPos.X)) % tileWidth;
            }
            else
            {
                viewPos.X = (float)Math.Floor(viewPos.X);
            }

            if (viewPos.Y > 0)
            {
                viewPos.Y = ((int)Math.Floor(viewPos.Y)) % tileHeight;
            }
            else
            {
                viewPos.Y = (float)Math.Floor(viewPos.Y);
            }

            TileInfo info = new TileInfo();
            if (_TileInfoCache == null)
                BuildTileInfoCache(tilesets);

            foreach (Tileset tileset in tilesets)
            {
                if (tileset.tiles == null)
                {
                    Texture2D[] tiles = new Texture2D[_TileInfoCache.Length];
                    for (int k = 0; k < _TileInfoCache.Length; k++)
                    {
                        Texture2D test = new Texture2D(d.GraphicsDevice, tileWidth, tileHeight);
                        test = GetTileTexture(k + 1, tileset, test);
                        tiles[k] = test;
                    }

                    tileset.tiles = tiles;
                    Console.WriteLine("tiles added");
                }
            }

            // We're drawing at the center of the tile, so adjust our y offset
            destPos.Y += (tileHeight) / 2f;

            for (int y = minY; y <= maxY; y++)
            {
                // We're drawing at the center of the tile, so adjust the x offset
                destPos.X = rectangle.Left + (tileWidth) / 2f;

                for (int x = minX; x <= maxX; x++)
                {
                    i = (y * Width) + x;

                    byte flipAndRotate = FlipAndRotate[i];
                    SpriteEffects flipEffect = SpriteEffects.None;
                    float rotation = 0f;

                    if ((flipAndRotate & Layer.HorizontalFlipDrawFlag) != 0)
                    {
                        flipEffect |= SpriteEffects.FlipHorizontally;
                    }
                    if ((flipAndRotate & Layer.VerticalFlipDrawFlag) != 0)
                    {
                        flipEffect |= SpriteEffects.FlipVertically;
                    }
                    if ((flipAndRotate & Layer.DiagonallyFlipDrawFlag) != 0)
                    {
                        if ((flipAndRotate & Layer.HorizontalFlipDrawFlag) != 0 &&
                             (flipAndRotate & Layer.VerticalFlipDrawFlag) != 0)
                        {
                            rotation = (float)(Math.PI / 2);
                            flipEffect ^= SpriteEffects.FlipVertically;
                        }
                        else if ((flipAndRotate & Layer.HorizontalFlipDrawFlag) != 0)
                        {
                            rotation = (float)-(Math.PI / 2);
                            flipEffect ^= SpriteEffects.FlipVertically;
                        }
                        else if ((flipAndRotate & Layer.VerticalFlipDrawFlag) != 0)
                        {
                            rotation = (float)(Math.PI / 2);
                            flipEffect ^= SpriteEffects.FlipHorizontally;
                        }
                        else
                        {
                            rotation = -(float)(Math.PI / 2);
                            flipEffect ^= SpriteEffects.FlipHorizontally;
                        }
                    }

                    int index = Tiles[i] - 1;
                    float test = (scale - 1) / 2;
                    Vector2 adj = new Vector2(rectangle.Width * test, rectangle.Height * test);
                    if ((index >= 0) && (index < _TileInfoCache.Length))
                    {
                        d.Draw(new UITexture(tilesets[0].tiles[index]), destPos - viewPos - adj, null,
                                   Color.White * this.Opacity, rotation, new Vector2(tileWidth / 2f, tileHeight / 2f),
                                   scale, flipEffect, 0);
                    }

                    destPos.X += (tileWidth * scale);
                }

                destPos.Y += (tileHeight * scale);
            }

        }
    }

    public class ObjectGroup
    {
        public SortedList<string, Object> Objects = new SortedList<string, Object>();
        public SortedList<string, string> Properties = new SortedList<string, string>();

        public string Name;
        public int Width, Height, X, Y;
        float Opacity = 1;

        internal static ObjectGroup Load(XmlReader reader)
        {
            var result = new ObjectGroup();
            CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.CurrencyDecimalSeparator = ".";

            if (reader.GetAttribute("name") != null)
                result.Name = reader.GetAttribute("name");
            if (reader.GetAttribute("width") != null)
                result.Width = int.Parse(reader.GetAttribute("width"));
            if (reader.GetAttribute("height") != null)
                result.Height = int.Parse(reader.GetAttribute("height"));
            if (reader.GetAttribute("x") != null)
                result.X = int.Parse(reader.GetAttribute("x"));
            if (reader.GetAttribute("y") != null)
                result.Y = int.Parse(reader.GetAttribute("y"));
            if (reader.GetAttribute("opacity") != null)
                result.Opacity = float.Parse(reader.GetAttribute("opacity"), NumberStyles.Any, ci);

            while (!reader.EOF)
            {
                var name = reader.Name;

                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        switch (name)
                        {
                            case "object":
                                {
                                    using (var st = reader.ReadSubtree())
                                    {
                                        st.Read();
                                        var objects = Object.Load(st);
                                        if (!result.Objects.ContainsKey(objects.Name))
                                        {
                                            result.Objects.Add(objects.Name, objects);
                                        }
                                        else
                                        {
                                            int count = result.Objects.Keys.Count((item) => item.Equals(objects.Name));
                                            result.Objects.Add(string.Format("{0}{1}", objects.Name, count), objects);
                                        }
                                    }
                                }
                                break;
                            case "properties":
                                {
                                    using (var st = reader.ReadSubtree())
                                    {
                                        while (!st.EOF)
                                        {
                                            switch (st.NodeType)
                                            {
                                                case XmlNodeType.Element:
                                                    if (st.Name == "property")
                                                    {
                                                        if (st.GetAttribute("name") != null)
                                                        {
                                                            result.Properties.Add(st.GetAttribute("name"), st.GetAttribute("value"));
                                                        }
                                                    }

                                                    break;
                                                case XmlNodeType.EndElement:
                                                    break;
                                            }

                                            st.Read();
                                        }
                                    }
                                }
                                break;
                        }

                        break;
                    case XmlNodeType.EndElement:
                        break;
                }

                reader.Read();
            }

            return result;
        }

        public void Draw(Map result, IBatchedDrawingService d, Rectangle rectangle, Vector2 viewportPosition, int tilewidth, int tileheight, float scale)
        {
            foreach (var objects in Objects.Values)
            {
                if (objects.Texture != null)
                {
                    objects.Draw(d, rectangle, new Vector2(this.X * (result.TileWidth), this.Y * result.TileHeight), viewportPosition, this.Opacity, scale);
                }
            }
        }
    }

    public class Object
    {
        public SortedList<string, string> Properties = new SortedList<string, string>();

        public string Name, Image, Points, Type;
        public int Width, Height, X, Y;

        public List<Vector2> PointsList;

        protected Texture2D _Texture;
        protected int _TexWidth, _TexHeight;

        public Texture2D Texture
        {
            get
            {
                return _Texture;
            }
            set
            {
                _Texture = value;
                _TexWidth = value.Width;
                _TexHeight = value.Height;
            }
        }

        internal static Object Load(XmlReader reader)
        {
            var result = new Object();

            result.Name = reader.GetAttribute("name");

            result.Type = reader.GetAttribute("type");

            result.X = (int)Convert.ToDouble(reader.GetAttribute("x"));
            result.Y = (int)Convert.ToDouble(reader.GetAttribute("y"));

            /*
             * Height and width are optional on objects
             */
            int width;
            if (int.TryParse(reader.GetAttribute("width"), out width))
            {
                result.Width = width;
            }

            int height;
            if (int.TryParse(reader.GetAttribute("height"), out height))
            {
                result.Height = height;
            }

            while (!reader.EOF)
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "properties")
                        {
                            using (var st = reader.ReadSubtree())
                            {
                                while (!st.EOF)
                                {
                                    switch (st.NodeType)
                                    {
                                        case XmlNodeType.Element:
                                            if (st.Name == "property")
                                            {
                                                if (st.GetAttribute("name") != null)
                                                {
                                                    result.Properties.Add(st.GetAttribute("name"), st.GetAttribute("value"));
                                                }
                                            }

                                            break;
                                        case XmlNodeType.EndElement:
                                            break;
                                    }

                                    st.Read();
                                }
                            }
                        }
                        if (reader.Name == "polygon" || reader.Name == "polyline")
                        {
                            result.Points = reader.GetAttribute("points");
                            string[] points = new string[10];
                            points = result.Points.Split(' ');
                            List<Vector2> test1 = new List<Vector2>();
                            foreach (string s in points)
                            {
                                string[] p = new string[2];
                                p = s.Split(',');
                                int x = (int)Convert.ToDouble(p[0]);
                                int y = (int)Convert.ToDouble(p[1]);
                                test1.Add(new Vector2(x, y));
                            }
                            result.PointsList = test1;
                        }
                        if (reader.Name == "image")
                        {
                            result.Image = reader.GetAttribute("source");
                        }

                        break;
                    case XmlNodeType.EndElement:
                        break;
                }

                reader.Read();
            }

            return result;
        }

        public void Draw(IBatchedDrawingService d, Rectangle rectangle, Vector2 offset, Vector2 viewportPosition, float opacity, float scale)
        {
            int minX = (int)Math.Floor(viewportPosition.X);
            int minY = (int)Math.Floor(viewportPosition.Y);
            int maxX = (int)Math.Ceiling((rectangle.Width + viewportPosition.X));
            int maxY = (int)Math.Ceiling((rectangle.Height + viewportPosition.Y));

            if (this.X + offset.X + this.Width > minX && this.X + offset.X < maxX)
                if (this.Y + offset.Y + this.Height > minY && this.Y + offset.Y < maxY)
                {
                    float test = (scale - 1) / 2;
                    int x = (int)(this.X + offset.X);
                    int y = (int)(this.Y + offset.Y);
                    float fx = (x * scale) - (viewportPosition.X * scale) - (rectangle.Width * test);
                    float fy = (y * scale) - (viewportPosition.Y * scale) - (rectangle.Height * test);
                    float fw = this.Width * scale;
                    float fh = this.Height * scale;
                    Console.WriteLine(new Vector2(x, y) + " " + new Vector2(fx, fy) + " " + test);
                    d.Draw(new UITexture(this.Texture), new Rectangle((int)fx, (int)fy, (int)fw, (int)fh), new Rectangle(0, 0, _Texture.Width, _Texture.Height),
                                  Color.White, 0f, new Vector2(fw / 2, fh / 2), SpriteEffects.None, 0);
                }
        }
    }

    public class Map
    {
        public SortedList<string, Tileset> Tilesets = new SortedList<string, Tileset>();
        public SortedList<string, Layer> Layers = new SortedList<string, Layer>();
        public SortedList<string, ObjectGroup> ObjectGroups = new SortedList<string, ObjectGroup>();
        public SortedList<string, string> Properties = new SortedList<string, string>();
        //added the combined list to store a list of all objectgroups and layers
        //this is so that they can be drawn all in the correct order within the layers.
        public SortedList<string, bool> Combined;
        public int Width, Height;
        public int TileWidth, TileHeight;
        public float scale = 1f;

        public string Orientation, RenderOrder;

        public static Map Load(string filename, ContentManager content)
        {
            var result = new Map();
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ProhibitDtd = false;

            using (var stream = System.IO.File.OpenText(filename))
            using (var reader = XmlReader.Create(stream, settings))
                while (reader.Read())
                {
                    var name = reader.Name;

                    switch (reader.NodeType)
                    {
                        case XmlNodeType.DocumentType:
                            if (name != "map")
                                throw new Exception("Invalid map format");
                            break;
                        case XmlNodeType.Element:
                            switch (name)
                            {
                                case "map":
                                    {
                                        result.Width = int.Parse(reader.GetAttribute("width"));
                                        result.Height = int.Parse(reader.GetAttribute("height"));
                                        result.TileWidth = int.Parse(reader.GetAttribute("tilewidth"));
                                        result.TileHeight = int.Parse(reader.GetAttribute("tileheight"));
                                        result.Orientation = reader.GetAttribute("orientation");
                                        result.RenderOrder = reader.GetAttribute("renderorder");
                                    }
                                    break;
                                case "tileset":
                                    {
                                        using (var st = reader.ReadSubtree())
                                        {
                                            st.Read();
                                            var tileset = Tileset.Load(st);
                                            result.Tilesets.Add(tileset.Name, tileset);
                                        }
                                    }
                                    break;
                                case "layer":
                                    {
                                        using (var st = reader.ReadSubtree())
                                        {
                                            st.Read();
                                            var layer = Layer.Load(st);
                                            if (null != layer)
                                            {
                                                result.Layers.Add(layer.Name, layer);
                                            }
                                        }
                                    }
                                    break;
                                case "objectgroup":
                                    {
                                        using (var st = reader.ReadSubtree())
                                        {
                                            st.Read();
                                            var objectgroup = ObjectGroup.Load(st);
                                            result.ObjectGroups.Add(objectgroup.Name, objectgroup);
                                        }
                                    }
                                    break;
                                case "properties":
                                    {
                                        using (var st = reader.ReadSubtree())
                                        {
                                            while (!st.EOF)
                                            {
                                                switch (st.NodeType)
                                                {
                                                    case XmlNodeType.Element:
                                                        if (st.Name == "property")
                                                        {
                                                            if (st.GetAttribute("name") != null)
                                                            {
                                                                result.Properties.Add(st.GetAttribute("name"), st.GetAttribute("value"));
                                                            }
                                                        }

                                                        break;
                                                    case XmlNodeType.EndElement:
                                                        break;
                                                }

                                                st.Read();
                                            }
                                        }
                                    }
                                    break;
                            }
                            break;
                        case XmlNodeType.EndElement:
                            break;
                        case XmlNodeType.Whitespace:
                            break;
                    }
                }


            foreach (var tileset in result.Tilesets.Values)
            {
                tileset.Texture = content.Load<Texture2D>(
                    Path.Combine(Path.GetDirectoryName(tileset.Image), Path.GetFileNameWithoutExtension(tileset.Image))
                );
            }


            foreach (var objects in result.ObjectGroups.Values)
            {
                foreach (var item in objects.Objects.Values)
                {
                    if (item.Image != null)
                    {
                        item.Texture = content.Load<Texture2D>
                        (
                            Path.Combine
                            (
                                Path.GetDirectoryName(item.Image),
                                Path.GetFileNameWithoutExtension(item.Image)
                            )
                        );
                    }
                }
            }

            return result;
        }

        //new method to generate the list of all layers and objectgroups to draw
        private SortedList<string, bool> CombinedList()
        {
            SortedList<string, bool> temp = new SortedList<string, bool>();

            //bool of true denotes a layer
            foreach (Layer layers in Layers.Values)
            {
                temp.Add(layers.Name, true);
            }

            //bool of false denotes an object group
            foreach (var objectgroups in ObjectGroups.Values)
            {
                temp.Add(objectgroups.Name, false);
            }
            Console.WriteLine("Combined List Created");
            return temp;
        }

        public void Draw(IBatchedDrawingService drawingService, Rectangle rectangle, Vector2 viewportPosition)
        {

            //check if combined list is created if not create it.

            if (Combined == null)
                Combined = CombinedList();

            //cycle through each item in combined list
            //if the value is true its a layer, false its an object group
            //the key should be the name of the layer or object group
            foreach (var layer in Combined)
            {
                if (layer.Value == true)
                {
                    if (Layers[layer.Key].Opacity != 0)
                    {
                        Layers[layer.Key].Draw(drawingService, Tilesets.Values, rectangle, viewportPosition, TileWidth, TileHeight, scale);
                    }
                }
                else
                    ObjectGroups[layer.Key].Draw(this, drawingService, rectangle, viewportPosition, TileWidth, TileHeight, scale);
            }
        }
    }
}
