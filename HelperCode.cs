﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldGenMod
{
    public struct Rectangle2P
    {
        private int x0;
        private int y0;
        private int x1;
        private int y1;
        private int xdiff;
        private int ydiff;
        private int xTiles;
        private int yTiles;

        /// <summary>
        /// Initializes a new instance of the Rectangle4Point structure with the specified values and size.
        /// <br/>It it a special rectangular, adapted to Terraria worlds, so that Width and Height will give back the amount of tiles on that side.
        /// <br/>For mathematical purposes there are xDiff and yDiff who give back the correct substraction values from X0 to X1 and Y0 to Y1.
        /// <br/>
        /// <br/>Example: From X0=100 to (including!) X1=101 there are 2 tiles and xDiff = 1
        /// <br/>
        /// <br/>Attention: don't use xTiles = 0 or yTiles = 0!
        /// </summary>
        /// <param name="xTopLeft">The x-coordinate of the upper-left corner of the rectangular region defined by this Rectangle4Point</param>
        /// <param name="yTopLeft">The y-coordinate of the upper-left corner of the rectangular region defined by this Rectangle4Point</param>
        /// <param name="xTiles">The amount of tiles on the x side of the rectangular region defined by this Rectangle4Point</param>
        /// <param name="yTiles">The amount of tiles on the y side of the rectangular region defined by this Rectangle4Point</param>
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
        }

        /// <summary>
        /// Initializes a new instance of the Rectangle4Point structure with the specified values and size.
        /// <br/>It it a special rectangular, adapted to Terraria worlds, so that Width and Height will give back the amount of tiles on that side.
        /// <br/>For mathematical purposes there are xDiff and yDiff who give back the correct substraction values from X0 to X1 and Y0 to Y1.
        /// <br/>
        /// <br/>Example: From X0=100 to (including!) X1=101 there are 2 tiles and xDiff = 1
        /// </summary>
        /// <param name="xTopLeft">The x-coordinate of the upper-left corner of the rectangular region defined by this Rectangle4Point</param>
        /// <param name="yTopLeft">The y-coordinate of the upper-left corner of the rectangular region defined by this Rectangle4Point</param>
        /// <param name="xBottomRight">The x-coordinate of the bottom-right corner of the rectangular region defined by this Rectangle4Point</param>
        /// <param name="yBottomRight">The y-coordinate of the bottom-right corner of the rectangular region defined by this Rectangle4Point</param>
        /// <param name="dummy">Just a dummy to have a second Constructor</param>
        public Rectangle2P(int xTopLeft, int yTopLeft, int xBottomRight, int yBottomRight, int dummy=0)
        {
            this.x0 = xTopLeft;
            this.y0 = yTopLeft;

            this.x1 = xBottomRight;
            this.y1 = yBottomRight;

            this.xdiff = x1 - x0;
            this.ydiff = y1 - y0;

            this.xTiles = xdiff + 1;
            this.yTiles = ydiff +1;
        }

        /// <summary>
        /// Initializes a new instance of the Rectangle4Point structure with the specified values and size.
        /// <br/>It it a special rectangular, adapted to Terraria worlds, so that Width and Height will give back the amount of tiles on that side.
        /// <br/>For mathematical purposes there are xDiff and yDiff who give back the correct substraction values from X0 to X1 and Y0 to Y1.
        /// <br/>
        /// <br/>Example: From X0=100 to (including!) X1=101 there are 2 tiles and xDiff = 1
        /// </summary>
        /// <param name="xTopLeft">The x-coordinate of the upper-left corner of the rectangular region defined by this Rectangle4Point</param>
        /// <param name="yTopLeft">The y-coordinate of the upper-left corner of the rectangular region defined by this Rectangle4Point</param>
        /// <param name="xMath">The x-coordinate of the bottom-right corner of the rectangular region defined by this Rectangle4Point</param>
        /// <param name="yMath">The y-coordinate of the bottom-right corner of the rectangular region defined by this Rectangle4Point</param>
        /// <param name="dummy">Just a dummy to have a second Constructor</param>
        public Rectangle2P(int xTopLeft, int yTopLeft, int xMath, int yMath, float dummy = 0f)
        {
            this.x0 = xTopLeft;
            this.y0 = yTopLeft;

            this.xdiff = xMath;
            this.ydiff = yMath;

            this.x1 = xTopLeft + xMath;
            this.y1 = yTopLeft + yMath;



            this.xTiles = xdiff + 1;
            this.yTiles = ydiff + 1;
        }

        /// <summary>
        /// Gets or sets the x-coordinate of the upper-left corner of the rectangular region defined by this Rectangle4Point.
        /// </summary>
        public readonly int X0
        {
            get => x0;
        }

        /// <summary>
        /// Gets or sets the y-coordinate of the upper-left corner of the rectangular region defined by this Rectangle4Point.
        /// </summary>
        public readonly int Y0
        {
            get => y0;
        }

        /// <summary>
        /// Gets the mathematical length of the x side (= X1 - X0) of the rectangular region defined by this Rectangle4Point.
        /// </summary>
        public readonly int X1
        {
            get => x1;
        }

        /// <summary>
        /// Gets the mathematical length of the y side (= Y1 - Y0) of the rectangular region defined by this Rectangle4Point.
        /// </summary>
        public readonly int Y1
        {
            get => y1;
        }

        /// <summary>
        /// Gets or sets the x-coordinate of the bottom-right corner of the rectangular region defined by this Rectangle4Point.
        /// </summary>
        public readonly int XDiff
        {
            get => xdiff;
        }

        /// <summary>
        /// Gets or sets the y-coordinate of the bottom-right corner of the rectangular region defined by this Rectangle4Point.
        /// </summary>
        public readonly int YDiff
        {
            get => ydiff;
        }

        /// <summary>
        /// Gets the amount of tiles on the x side of the rectangular region defined by this Rectangle4Point.
        /// </summary>
        public readonly int XTiles
        {
            get => xTiles;
        }

        /// <summary>
        /// Gets the amount of tiles on the y side of the rectangular region defined by this Rectangle4Point.
        /// </summary>
        public readonly int YTiles
        {
            get => yTiles;
        }
    }

}
