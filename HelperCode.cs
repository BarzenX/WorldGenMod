﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;

namespace WorldGenMod
{
    public struct Rectangle2P
    {
        private int x0; //The x-coordinate of the upper-left corner of the rectangular region defined by this Rectangle2Point
        private int y0; //The y-coordinate of the upper-left corner of the rectangular region defined by this Rectangle2Point
        private int x1; //The x-coordinate of the bottom-right corner of the rectangular region defined by this Rectangle2Point
        private int y1; //The y-coordinate of the bottom-right corner of the rectangular region defined by this Rectangle2Point
        private int xdiff; //The mathematical x-difference of the rectangular region defined by this Rectangle2Point
        private int ydiff; //The mathematical y-difference of the rectangular region defined by this Rectangle2Point
        private int xTiles; //The amount of tiles on the x side of the rectangular region defined by this Rectangle2Point
        private int yTiles; //The amount of tiles on the y side of the rectangular region defined by this Rectangle2Point
        private int xCenter; //The x-coordinate of the center point of the rectangular region defined by this Rectangle2Point (if xTiles is even, there is no real middle, and the lower x-coordinate of the "double tile center" will be returned)
        private int yCenter; //The y-coordinate of the center point of the rectangular region defined by this Rectangle2Point (if yTiles is even, there is no real middle, and the lower y-coordinate of the "double tile center" will be returned)

        /// <summary>
        /// Initializes a new instance of the Rectangle2Point structure with the specified values. 
        /// <br/>It is a special rectangular, adapted to Terraria worlds (based on discrete tiles), so that xTiles and yTiles will give back the amount of tiles on that side.
        /// <br/>For mathematical purposes there are xDiff and yDiff who give back the correct substraction values from X0 to X1 and Y0 to Y1.
        /// <br/>
        /// <br/>Example: From X0=100 to (including!) X1=101 there are 2 tiles and xDiff = 1
        /// <br/>
        /// <br/>Attention: don't use xTiles or yTiles smaller than 1!
        /// </summary>
        /// <param name="xTopLeft">The x-coordinate of the upper-left corner of the rectangular region defined by this Rectangle2Point</param>
        /// <param name="yTopLeft">The y-coordinate of the upper-left corner of the rectangular region defined by this Rectangle2Point</param>
        /// <param name="xTiles">The amount of tiles on the x side of the rectangular region defined by this Rectangle2Point</param>
        /// <param name="yTiles">The amount of tiles on the y side of the rectangular region defined by this Rectangle2Point</param>
        public Rectangle2P(int xTopLeft, int yTopLeft, int xTiles, int yTiles)
        {
            this.x0 = xTopLeft;
            this.y0 = yTopLeft;
            this.xTiles = xTiles;
            this.yTiles = yTiles;

            this.xdiff = this.xTiles - 1;
            this.ydiff = this.yTiles - 1;

            this.x1 = this.x0 + this.xdiff;
            this.y1 = this.y0 + this.ydiff;

            this.xCenter = x0 + this.xdiff / 2;
            this.yCenter = y0 + this.ydiff / 2;
        }

        /// <summary>
        /// Initializes a new instance of the Rectangle2Point structure with the specified values.
        /// <br/>It is a special rectangular, adapted to Terraria worlds (based on discrete tiles), so that xTiles and yTiles will give back the amount of tiles on that side.
        /// <br/>For mathematical purposes there are xDiff and yDiff who give back the correct substraction values from X0 to X1 and Y0 to Y1.
        /// <br/>
        /// <br/>Example: From X0=100 to (including!) X1=101 there are 2 tiles and xDiff = 1
        /// </summary>
        /// <param name="xTopLeft">The x-coordinate of the upper-left corner of the rectangular region defined by this Rectangle2Point</param>
        /// <param name="yTopLeft">The y-coordinate of the upper-left corner of the rectangular region defined by this Rectangle2Point</param>
        /// <param name="xBottomRight">The x-coordinate of the bottom-right corner of the rectangular region defined by this Rectangle2Point</param>
        /// <param name="yBottomRight">The y-coordinate of the bottom-right corner of the rectangular region defined by this Rectangle2Point</param>
        /// <param name="dummy">Just a dummy to have another Constructor</param>
        public Rectangle2P(int xTopLeft, int yTopLeft, int xBottomRight, int yBottomRight, String dummy)
        {
            this.x0 = xTopLeft;
            this.y0 = yTopLeft;

            this.x1 = xBottomRight;
            this.y1 = yBottomRight;

            this.xdiff = x1 - x0;
            this.ydiff = y1 - y0;

            this.xTiles = xdiff + 1;
            this.yTiles = ydiff + 1;

            this.xCenter = x0 + this.xdiff / 2;
            this.yCenter = y0 + this.ydiff / 2;
        }

        /// <summary>
        /// Initializes a new instance of the Rectangle2Point structure with the specified values.
        /// <br/>It is a special rectangular, adapted to Terraria worlds (based on discrete tiles), so that xTiles and yTiles will give back the amount of tiles on that side.
        /// <br/>For mathematical purposes there are xDiff and yDiff who give back the correct substraction values from X0 to X1 and Y0 to Y1.
        /// <br/>
        /// <br/>Example: From X0=100 to (including!) X1=101 there are 2 tiles and xDiff = 1
        /// <br/>
        /// <br/>Attention: don't use xMath or yMath smaller than 0!
        /// </summary>
        /// <param name="xTopLeft">The x-coordinate of the upper-left corner of the rectangular region defined by this Rectangle2Point</param>
        /// <param name="yTopLeft">The y-coordinate of the upper-left corner of the rectangular region defined by this Rectangle2Point</param>
        /// <param name="xMath">The mathematical x-difference of the rectangular region defined by this Rectangle2Point</param>
        /// <param name="yMath">The mathematical y-difference of the rectangular region defined by this Rectangle2Point</param>
        /// <param name="dummy">Just a dummy to have another Constructor</param>
        public Rectangle2P(int xTopLeft, int yTopLeft, int xMath, int yMath, char dummy)
        {
            this.x0 = xTopLeft;
            this.y0 = yTopLeft;

            this.xdiff = xMath;
            this.ydiff = yMath;

            this.x1 = xTopLeft + xMath;
            this.y1 = yTopLeft + yMath;

            this.xTiles = xdiff + 1;
            this.yTiles = ydiff + 1;

            this.xCenter = x0 + this.xdiff / 2;
            this.yCenter = y0 + this.ydiff / 2;
        }

        /// <summary>
        /// Gets or sets the x-coordinate of the upper-left corner of the rectangular region defined by this Rectangle2Point.
        /// </summary>
        public int X0
        {
            readonly get => x0;
            set
            {
                this.x0 = value;
                this.xdiff = this.x1 - this.x0;
                this.xTiles = xdiff + 1;
            }
        }

        /// <summary>
        /// Gets or sets the y-coordinate of the upper-left corner of the rectangular region defined by this Rectangle2Point.
        /// </summary>
        public int Y0
        {
            readonly get => y0;
            set
            {
                this.y0 = value;
                this.ydiff = this.y1 - this.y0;
                this.yTiles = ydiff + 1;
            }
        }

        /// <summary>
        /// Gets or sets the x-coordinate of the bottom-right corner of the rectangular region defined by this Rectangle2Point.
        /// </summary>
        public int X1
        {
            readonly get => x1;
            set
            {
                this.x1 = value;
                this.xdiff = this.x1 - this.x0;
                this.xTiles = xdiff + 1;
            }
        }

        /// <summary>
        /// Gets or sets the y-coordinate of the bottom-right corner of the rectangular region defined by this Rectangle2Point.
        /// </summary>
        public int Y1
        {
            readonly get => y1;
            set
            {
                this.y1 = value;
                this.ydiff = this.y1 - this.y0;
                this.yTiles = ydiff + 1;
            }
        }

        /// <summary>
        /// Gets the mathematical length of the x side (= X1 - X0) of the rectangular region defined by this Rectangle2Point.
        /// </summary>
        public readonly int XDiff
        {
            get => xdiff;
        }

        /// <summary>
        /// Gets the mathematical length of the y side (= Y1 - Y0) of the rectangular region defined by this Rectangle2Point.
        /// </summary>
        public readonly int YDiff
        {
            get => ydiff;
        }

        /// <summary>
        /// Gets the amount of tiles on the x side of the rectangular region defined by this Rectangle2Point.
        /// </summary>
        public readonly int XTiles
        {
            get => xTiles;
        }

        /// <summary>
        /// Gets the amount of tiles on the y side of the rectangular region defined by this Rectangle2Point.
        /// </summary>
        public readonly int YTiles
        {
            get => yTiles;
        }

        /// <summary>
        /// Gets the x-coordinate of the center point of the rectangular region defined by this Rectangle2Point.
        /// <br/> If xTiles is even, there is no real middle, and the lower x-coordinate of the "double tile center" will be returned
        /// </summary>
        public readonly int XCenter
        {
            get => xCenter;
        }

        /// <summary>
        /// Gets the y-coordinate of the center point of the rectangular region defined by this Rectangle2Point.
        /// <br/> If yTiles is even, there is no real middle, and the lower y-coordinate of the "double tile center" will be returned
        /// </summary>
        public readonly int YCenter
        {
            get => yCenter;
        }

        /// <summary>
        /// Adjusts the location of this rectangle by the specified amount.
        /// </summary>
        public void Move(int x, int y)
        {
            unchecked
            {
                x0 += x;
                x1 += x;

                y0 += y;
                y1 += y;
            }
        }

        /// <summary>
        /// Determines if the specified point is contained (including the frame) within the rectangular region defined by this Rectangle2Point.
        /// </summary>
        public readonly bool Contains(int x, int y) => (X0 <= x) && (x <= X1) && (Y0 <= y) && (y <= Y1);

        /// <summary>
        /// Converts the attributes of this Rectangle2Point to a human readable string.
        /// </summary>
        public override readonly string ToString() => $"{{X0={x0}, Y0={y0}, X1={x1}, Y1={y1}, xdiff={xdiff}, ydiff={ydiff}}}";
    }

    public struct Ellipse
    {
        private int x0; // The x-coordinate of the center point of this Ellipse
        private int y0; // The y-coordinate of the center point of this Ellipse
        private int xRadius; // The amount of tiles this Ellipse advances in the x-direction (and -x direction)
        private int yRadius; // The amount of tiles this Ellipse advances in the y-direction (and -y direction)
        private int xTiles; // The amount of tiles along the x diameter of the region defined by this Ellipse
        private int yTiles; // The amount of tiles along the y diameter of the region defined by this Ellipse

        private bool xForm; // Defines the appearance of the Ellipse: true = long side of the ellipse is along x-direction


        /// <summary>
        /// Initializes a new instance of the Ellipse structure with the specified values. 
        /// <br/>It is a special ellipse, adapted to Terraria worlds (based on discrete tiles).
        /// <br/>The Ellipse includes all tiles that have a (float) radius length equal and lower than xRadius and yRadius
        /// <br/>
        /// <br/><b>Example1</b>: <i>xCenter = 100</i> and <i>xRadius=1</i> includes the x-tiles <i>99,100,101</i>
        /// <br/><b>Example2</b>: <i>xCenter = yCenter = 100</i>  and <i>xRadius  = yRadius = 1</i>  includes the tiles <i>(100,101), (99,100), (100,100), (101,100), (100,101)</i>
        /// <br/>
        /// <br/><b>Attention</b>: <i>xRadius = 0</i>  or <i>yRadius = 0</i>  will reduce the Ellipse to a line of tiles
        /// </summary>
        /// <param name="xCenter">The x-coordinate of the upper-left corner of the rectangular region defined by this Rectangle2Point</param>
        /// <param name="yCenter">The y-coordinate of the upper-left corner of the rectangular region defined by this Rectangle2Point</param>
        /// <param name="xRadius">The amount of tiles on the x side of the rectangular region defined by this Rectangle2Point</param>
        /// <param name="yRadius">The amount of tiles on the y side of the rectangular region defined by this Rectangle2Point</param>
        public Ellipse(int xCenter, int yCenter, int xRadius, int yRadius)
        {
            this.x0 = xCenter;
            this.y0 = yCenter;
            this.xRadius = xRadius;
            this.yRadius = yRadius;

            this.xTiles = 2 * xRadius + 1;
            this.yTiles = 2 * yRadius + 1;

            xForm = xRadius >= yRadius;
        }

        /// <summary>
        /// Gets or sets the x-coordinate of the upper-left corner of the rectangular region defined by this Rectangle2Point.
        /// </summary>
        public int X0
        {
            readonly get => x0;
            set { this.x0 = value; }
        }

        /// <summary>
        /// Gets or sets the y-coordinate of the upper-left corner of the rectangular region defined by this Rectangle2Point.
        /// </summary>
        public int Y0
        {
            readonly get => y0;
            set { this.y0 = value; }
        }

        /// <summary>
        /// Gets or sets the radius in x-direction of the Ellipse.
        /// </summary>
        public int XRad
        {
            readonly get => xRadius;
            set { this.xRadius = value; }
        }

        /// <summary>
        /// Gets or sets the radius in y-direction of the Ellipse.
        /// </summary>
        public int YRad
        {
            readonly get => yRadius;
            set { this.yRadius = value; }
        }

        /// <summary>
        /// Gets the amount of tiles on the x side of the rectangular region defined by this Rectangle2Point.
        /// </summary>
        public readonly int XTiles
        {
            get => xTiles;
        }

        /// <summary>
        /// Gets the amount of tiles on the y side of the rectangular region defined by this Rectangle2Point.
        /// </summary>
        public readonly int YTiles
        {
            get => yTiles;
        }

        /// <summary>
        /// Adjusts the location of this rectangle by the specified amount.
        /// </summary>
        public void Move(int x, int y)
        {
            unchecked
            {
                this.x0 += x;
                this.y0 += y;
            }
        }

        /// <summary>
        /// Determines if the specified point (x/y) (written in global coordinates) is contained within the Ellipse.
        /// </summary>
        public readonly bool Contains(int x, int y, bool includeBorder = false)
        {
            // x², y², xRadius², yRadius²
            long x2 = (x-x0) * (x-x0); // x²
            long y2 = (y-y0) * (y-y0); // y²
            long xR2 = xRadius * xRadius;
            long yR2 = yRadius * yRadius;

            if (includeBorder)
            {
                if (xForm) return yR2 * x2 + xR2 * y2 <= xR2 * yR2;
                else return xR2 * x2 + yR2 * y2 <= xR2 * yR2;
            }
            else
            {
                if (xForm) return yR2 * x2 + xR2 * y2 < xR2 * yR2;
                else return xR2 * x2 + yR2 * y2 < xR2 * yR2;
            }
        }

        /// <summary>
        /// Calculates the normalized distance from the Ellipse center point to the specified point (x/y) (written in global coordinates).
        /// </summary>
        public readonly float Distance(int x, int y)
        {
            // x², y², xRadius², yRadius²
            long x2 = (x - x0) * (x - x0); // x²
            long y2 = (y - y0) * (y - y0); // y²
            long xR2 = xRadius * xRadius;
            long yR2 = yRadius * yRadius;

            if (xForm) return (float)(yR2 * x2 + xR2 * y2) / (float)(xR2 * yR2);
            else return       (float)(xR2 * x2 + yR2 * y2) / (float)(xR2 * yR2);
        }

        /// <summary>
        /// Calculates the normalized distance from the Ellipse center point to the specified point (x/y) (written in global coordinates) and if it's contained within the Ellipse
        /// <br/>
        /// </summary>
        public readonly (float, bool) Distance_Contains(int x, int y, bool includeBorder = false)
        {
            // x², y², xRadius², yRadius²
            long x2 = (x - x0) * (x - x0); // x²
            long y2 = (y - y0) * (y - y0); // y²
            long xR2 = xRadius * xRadius;
            long yR2 = yRadius * yRadius;

            float num1, num2;

            if (xForm)
            {
                num1 = yR2 * x2 + xR2 * y2;
                num2 = xR2 * yR2;

                if (includeBorder)   return (num1 / num2, num1 <= num2);
                else                 return (num1 / num2, num1 <  num2);
            }
            else
            {
                num1 = xR2 * x2 + yR2 * y2;
                num2 = xR2 * yR2;

                if (includeBorder) return (num1 / num2, num1 <= num2);
                else               return (num1 / num2, num1 < num2);
            }
        }

        /// <summary>
        /// Converts the attributes of this Ellipse to a human readable string.
        /// </summary>
        public override readonly string ToString() => $"{{X0={x0}, Y0={y0}, xRadius={xRadius}, yRadius={yRadius}, xTiles={xTiles}, yTiles={yTiles}, xForm={xForm}}}";
    }

    class Func
    {
        /// <summary>
        /// Turns a chandelier from it's lit appearance (standard appearance after placing) to it's unlit appearance
        /// </summary>
        /// <param name="x">The x-coordinate used for placing the chandelier</param>
        /// <param name="y">The y-coordinate used for placing the chandelier</param>
        public static void UnlightChandelier(int x, int y)
        {
            if (Main.tile[x, y].TileFrameX < 54) //chandelier is lit
            {
                for (int i = x - 1; i <= x + 1; i++)
                {
                    for (int j = y; j <= y + 2; j++)
                    {
                        Main.tile[i, j].TileFrameX += 54; // make the chandelier unlit
                        // Explanation:
                        // A chandelier is a 3x3 multitile. Each tile consists of 18 pixels. The unlit appearance is just at the right of the lit one on the Tile Spritesheet.
                        // So each of the 3x3 unlit tiles has an offset of 3*18 pixels to is lit appearance.
                    }
                }
            }
            //TODO: expand method for other light sources
        }

        /// <summary>
        /// Turns a lantern from it's lit appearance (standard appearance after placing) to it's unlit appearance
        /// </summary>
        /// <param name="x">The x-coordinate used for placing the lantern</param>
        /// <param name="y">The y-coordinate used for placing the lantern</param>
        public static void UnlightLantern(int x, int y)
        {
            if (Main.tile[x, y].TileFrameX < 18) //lantern is lit
            {
                for (int j = y; j <= y + 1; j++)
                {
                    Main.tile[x, j].TileFrameX += 18; // make the lantern unlit
                }
            }
        }

        /// <summary>
        /// Works like WorldGen.PlaceSmallPile, but for large piles (186 or 187)
        /// </summary>
        /// <param name="xPlace">x-coordinate of world placement position</param>
        /// <param name="yPlace">y-coordinate of world placement position</param>
        /// <param name="XSprite">Horizontal count of chosen sprite, counting starts at 0 (f.ex. "Broken Chandelier covered in CobWeb" is 25)</param>
        /// <param name="YSprite">Vertical count of chosen sprite, counting starts at 0 (type 186 only has Y=0) </param>
        /// <param name="type">TileID</param>
        public static void PlaceLargePile(int xPlace, int yPlace, int XSprite, int YSprite, ushort type = (ushort)186.187)
        {
            if (type < 186 || type > 187) return;

            WorldGen.PlaceTile(xPlace, yPlace, type);

            for (int x = xPlace - 1; x <= xPlace + 1; x++)
            {
                for (int y = yPlace - 1; y <= yPlace; y++)
                {
                    Main.tile[x, y].TileFrameX += (short)(XSprite * 18 * 3);
                    Main.tile[x, y].TileFrameY += (short)(YSprite * 18 * 2);
                }
            }
        }

        /// <summary>
        /// Places a specific SubID of a 1x1 tile 
        /// </summary>
        /// <param name="xPlace">x-coordinate (in world coordinates) of the placement position</param>
        /// <param name="yPlace">y-coordinate (in world coordinates) of the placement position</param>
        /// <param name="XSprite">Horizontal count of chosen sprite, counting starts at 0 (f.ex. "Mug" in Tile-ID#13 is 4)</param>
        /// <param name="YSprite">Vertical count of chosen sprite, counting starts at 0</param>
        /// <param name="type">TileID</param>
        public static void Place1x1SubID(int xPlace, int yPlace, ushort type, int XSprite, int YSprite)
        {
            WorldGen.PlaceTile(xPlace, yPlace, type);
            Main.tile[xPlace, yPlace].TileFrameX += (short)(XSprite * 18);
            Main.tile[xPlace, yPlace].TileFrameY += (short)(YSprite * 18);
        }

        /// <summary>
        /// Tries to place a tile repeated times in a given space (a straight line), each time variating the placement position
        /// </summary>
        /// <param name="xPlace">x-coordinate (in world coordinates) of the placement position</param>
        /// <param name="yPlace">y-coordinate (in world coordinates) of the placement position</param>
        /// <param name="XSprite">Horizontal count of chosen sprite, counting starts at 0 (f.ex. "Mug" in Tile-ID#13 is 4)</param>
        /// <param name="YSprite">Vertical count of chosen sprite, counting starts at 0</param>
        /// <param name="type">TileID</param>
        public static void TryPlaceTile(Rectangle2P area, int yPlace, ushort type, int XSprite, int YSprite)
        {
            //WorldGen.PlaceTile(xPlace, yPlace, type);
            //Main.tile[xPlace, yPlace].TileFrameX += (short)(XSprite * 18);
            //Main.tile[xPlace, yPlace].TileFrameY += (short)(YSprite * 18);
            //TODO:
        }
    }

}
