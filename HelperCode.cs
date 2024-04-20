using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using System.Diagnostics;
using WorldGenMod.Structures.Ice;
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;
using Microsoft.VisualBasic;

namespace WorldGenMod
{


    class Func
    {
        /// <summary>
        /// Turns a chandelier from it's lit appearance (standard appearance after placing) to it's unlit appearance
        /// </summary>
        /// <param name="x">The x-coordinate used for placing the chandelier</param>
        /// <param name="y">The y-coordinate used for placing the chandelier</param>
        public static void UnlightChandelier(int x, int y)
        {
            Tile tile = Main.tile[x, y];
            if (tile.TileType != TileID.Chandeliers) return; // check if it's really a chandelier

            if (tile.TileFrameX < 54) // chandelier is lit
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
        }

        /// <summary>
        /// Turns a fireplace from it's lit appearance (standard appearance after placing) to it's unlit appearance
        /// </summary>
        /// <param name="x">The x-coordinate used for placing the fireplace</param>
        /// <param name="y">The y-coordinate used for placing the fireplace</param>
        public static void UnlightFireplace(int x, int y)
        {
            Tile tile = Main.tile[x, y];
            if (tile.TileType != TileID.Fireplace) return; // check if it's really a fireplace

            if (tile.TileFrameX < 54) // fireplace is lit
            {
                for (int i = x - 1; i <= x + 1; i++)
                {
                    for (int j = y - 1; j <= y; j++)
                    {
                        Main.tile[i, j].TileFrameX += 54; // make the fireplace unlit
                        if (j == y - 1) Main.tile[i, j].TileFrameY = 0; // make the fireplace unlit
                        if (j == y) Main.tile[i, j].TileFrameY = 18; // make the fireplace unlit
                    }
                }
            }
        }

        /// <summary>
        /// Turns a lantern from it's lit appearance (standard appearance after placing) to it's unlit appearance
        /// </summary>
        /// <param name="x">The x-coordinate used for placing the lantern</param>
        /// <param name="y">The y-coordinate used for placing the lantern</param>
        public static void UnlightLantern(int x, int y)
        {
            Tile tile = Main.tile[x, y];
            if (tile.TileType != TileID.Candelabras) return; // check if it's really a lantern

            if (tile.TileFrameX < 18) // lantern is lit
            {
                for (int j = y; j <= y + 1; j++)
                {
                    Main.tile[x, j].TileFrameX += 18; // make the lantern unlit
                }
            }
        }

        /// <summary>
        /// Turns a candelabra from it's lit appearance (standard appearance after placing) to it's unlit appearance
        /// </summary>
        /// <param name="x">The x-coordinate used for placing the candelabra</param>
        /// <param name="y">The y-coordinate used for placing the candelabra</param>
        public static void UnlightCandelabra(int x, int y)
        {
            Tile tile = Main.tile[x, y];
            if (tile.TileType != TileID.Candelabras) return; // check if it's really a candelabra

            if (tile.TileFrameX < 36) // candelabra is lit
            {
                for (int i = x - 1; i <= x; i++)  // don't know why the PlaceTile anker point bottom right and ingame placing is bottom left...
                {
                    for (int j = y - 1; j <= y; j++)
                    {
                        Main.tile[i, j].TileFrameX += 36; // make the candelabra unlit
                    }
                }

            }
        }

        /// <summary>
        /// Turns a 1x1 light source from it's lit appearance (standard appearance after placing) to it's unlit appearance
        /// </summary>
        /// <param name="x">The x-coordinate used for placing the 1x1 light source</param>
        /// <param name="y">The y-coordinate used for placing the 1x1 light source</param>
        public static void Unlight1x1(int x, int y)
        {
            Tile tile = Main.tile[x, y];

            if (tile.TileType == TileID.Candles) // candles
            {
                if (tile.TileFrameX < 18) // candle is lit
                {
                    Main.tile[x, y].TileFrameX += 18; // make the candle unlit
                }
            }
            else if (tile.TileType == TileID.Torches) // torch tiles are actually 22 x 22 pixels wide big! 
            {
                if (tile.TileFrameX < 66) // torch is lit
                {
                    Main.tile[x, y].TileFrameX += 66; // make the torch unlit
                }
            }
        }

        /// <summary>
        /// Turns a lamp from it's lit appearance (standard appearance after placing) to it's unlit appearance
        /// </summary>
        /// <param name="x">The x-coordinate used for placing the lamp</param>
        /// <param name="y">The y-coordinate used for placing the lamp</param>
        public static void UnlightLamp(int x, int y)
        {
            Tile tile = Main.tile[x, y];
            if (tile.TileType != TileID.Lamps) return; // check if it's really a lamp

            if (tile.TileFrameX < 18 || (tile.TileFrameX > 18 && tile.TileFrameX < 54)) // lamp is lit....there are 2 colums of lamps in the spritesheet
            {
                for (int j = y - 2; j <= y; j++)
                {
                    Main.tile[x, j].TileFrameX += 18; // make the lamp unlit
                }
            }
        }

        /// <summary>
        /// Changes a chair's facing direction from "to the left" (standard appearance after placing) to "to the right"
        /// </summary>
        /// <param name="x">The x-coordinate used for placing the chair</param>
        /// <param name="y">The y-coordinate used for placing the chair</param>
        public static void ChairTurnRight(int x, int y)
        {
            if (Main.tile[x, y].TileFrameX < 18) //chair is facing "to the left"
            {
                Main.tile[x, y].TileFrameX += 18; // make the chair face "to the right"
                Main.tile[x, y - 1].TileFrameX += 18; // make the chair face "to the right"
            }
        }

        /// <summary>
        /// Changes a bed's facing direction from "to the right" (standard appearance after placing) to "to the left"
        /// </summary>
        /// <param name="x">The x-coordinate used for placing the bed</param>
        /// <param name="y">The y-coordinate used for placing the bed</param>
        public static void BedTurnLeft(int x, int y)
        {
            if (Main.tile[x, y].TileFrameX >= 72) //bed is facing "to the right"
            {
                for (int i = x - 1; i <= x + 2; i++)
                {
                    for (int j = y - 1; j <= y; j++)
                    {
                        Main.tile[i, j].TileFrameX -= 72; // make the bed face "to the left"
                    }
                }

            }
        }

        /// <summary>
        /// Works like WorldGen.PlaceSmallPile, but for large piles (186 or 187).
        /// <br/>Has an option for painting the pile.
        /// </summary>
        /// <param name="xPlace">x-coordinate of world placement position</param>
        /// <param name="yPlace">y-coordinate of world placement position</param>
        /// <param name="XSprite">Horizontal count of chosen sprite, counting starts at 0 (f.ex. "Broken Chandelier covered in CobWeb" is 25)</param>
        /// <param name="YSprite">Vertical count of chosen sprite, counting starts at 0 (type 186 only has Y=0) </param>
        /// <param name="type">TileID</param>
        /// <param name="paint">State a PaintID bigger than 0 to automatically paint the pile</param>
        public static void PlaceLargePile(int xPlace, int yPlace, int XSprite, int YSprite, ushort type = (ushort)186.187, byte paint = 0)
        {
            if (type < 186 || type > 187) return;

            WorldGen.PlaceTile(xPlace, yPlace, type);

            for (int x = xPlace - 1; x <= xPlace + 1; x++)
            {
                for (int y = yPlace - 1; y <= yPlace; y++)
                {
                    Main.tile[x, y].TileFrameX += (short)(XSprite * 18 * 3);
                    Main.tile[x, y].TileFrameY += (short)(YSprite * 18 * 2);

                    if (paint > 0)   WorldGen.paintTile(x, y, paint);
                }
            }
            //TODO: check if free and check if PlaceTile was successful
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
        /// Adapted from "Place2x3Wall"....I did not like that a background wall is required
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="type"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        public static bool PlaceHangingLantern(int x, int y, ushort type, int style = 0)
        {
            if (!Main.tile[x, y - 1].HasTile || !Main.tile[x + 1, y - 1].HasTile) return false; // no solid tiles to hang on to

            for (int i = x; i <= x + 1; i++) // check if area is free
            {
                for (int j = y; j <= y + 2; j++)
                {
                    if (Main.tile[i, j].HasTile)
                    {
                        return false;
                    }
                }
            }

            int num2 = style * 36;
            int num3 = 0;
            Tile tile;
            for (int i = x; i <= x + 1; i++)
            {
                for (int j = y; j <= y + 2; j++)
                {
                    tile = Main.tile[i, j];
                    //tile.CopyFrom(Main.tile[x, y - 1]);
                    tile.HasTile = true;
                    tile.TileType = type;
                    tile.TileFrameX = (short)(num2 + 18 * (i - x));
                    tile.TileFrameY = (short)(num3 + 18 * (j - y));
                }
            }

            return true;
        }

        /// <summary>
        /// Tries to place an ItemFrame.
        /// <br/>If there is a background wall missing on the 2x2 placement area it can be filled by a wallType > 0. If not, placement will fail
        /// <br/>Has an option for painting the ItemFrame and for placing an item inside of it.
        /// </summary>
        /// <param name="x">Left x-coordinate of the ItemFrame placement position</param>
        /// <param name="y">Top y-coordinate of the ItemFrame placement position</param>
        /// <param name="wallType">State a WallID > 0 to automatically place missing background walls</param>
        /// <param name="paint">State a PaintID >= 0 to automatically paint the ItemFrame</param>
        /// <param name="item">State an ItemID > 0 to automatically place the stated item inside the ItemFrame</param>
        /// <returns></returns>
        public static bool PlaceItemFrame(int x, int y, int wallType = 0, int paint = -1, int item = 0)
        {
            bool forceWall = wallType > 0;
            bool applyPaint = paint >= 0;

            // pre-checks
            for (int i = x; i <= x + 1; i++)
            {
                for (int j = y; j <= y + 1; j++)
                {
                    if (Main.tile[i, j].HasTile)  // check if area is free
                    {
                        return false;
                    }
                    if (Main.tile[i, j].WallType == 0) // and has background wall
                    {
                        if (!forceWall) return false;
                        else WorldGen.PlaceWall(i, j, wallType);
                    }
                }
            }

            // place tiles
            Tile tile;
            for (int i = x; i <= x + 1; i++)
            {
                for (int j = y; j <= y + 1; j++)
                {
                    tile = Main.tile[i, j];
                    tile.HasTile = true;
                    tile.TileType = TileID.ItemFrame;
                    tile.TileFrameX = (short)(18 * (i - x));
                    tile.TileFrameY = (short)(18 * (j - y));

                    if (applyPaint) WorldGen.paintTile(i, j, (byte)paint);
                }
            }

            // place TileEntity
            int id = TEItemFrame.Place(x, y); // creates the TileEntity at the top left corner of the multitile (TileEntities are always at the top-left multitile corner)

            //place item inside of the ItemFrame
            if (item > 0)
            {
                TEItemFrame itemFrame = TileEntity.ByID[id] as TEItemFrame;
                itemFrame.item = new Item(item);
            }

            return true;
        }

        /// <summary>
        /// Tries to place a WeaponRack (TileID 471).
        /// <br/>If there is a background wall missing on the 3x3 placement area it can be filled by a wallType > 0. If not, placement will fail
        /// <br/>Has an option for painting the WeaponRack and for placing an item inside of it.
        /// </summary>
        /// <param name="x">Center x-coordinate of the WeaponRack placement position</param>
        /// <param name="y">Center y-coordinate of the WeaponRack placement position</param>
        /// <param name="wallType">State a WallID > 0 to automatically place missing background walls</param>
        /// <param name="paint">State a PaintID >= 0 to automatically paint the WeaponRack</param>
        /// <param name="item">State an ItemID > 0 to automatically place the stated item inside the WeaponRack</param>
        /// <param name="direction">If a placed sword would point from left-to-right (-1) or from right-to-left (+1) - seen from the handle to the tip)</param>
        /// <returns></returns>
        public static bool PlaceWeaponRack(int x, int y, int wallType = 0, int paint = -1, int item = 0, int direction = -1)
        {
            bool forceWall = wallType > 0;
            bool applyPaint = paint >= 0;
            short RightToLeftOffset = 0;
            if (direction == 1) RightToLeftOffset = 54;
            int xTL = x - 1; // x-coordinate of the top left point in the WeaponRack
            int yTL = y - 1; // y-coordinate of the top left point in the WeaponRack


            // pre-checks
            for (int i = xTL; i <= xTL + 2; i++)
            {
                for (int j = yTL; j <= yTL + 2; j++)
                {
                    if (Main.tile[i, j].HasTile)  // check if area is free
                    {
                        return false;
                    }
                    if (Main.tile[i, j].WallType == 0) // and has background wall
                    {
                        if (!forceWall) return false;
                        else WorldGen.PlaceWall(i, j, wallType);
                    }
                }
            }

            // place tiles
            Tile tile;
            for (int i = xTL; i <= xTL + 2; i++)
            {
                for (int j = yTL; j <= yTL + 2; j++)
                {
                    tile = Main.tile[i, j];
                    tile.HasTile = true;
                    tile.TileType = TileID.WeaponsRack2;
                    tile.TileFrameX = (short)(18 * (i - xTL) + RightToLeftOffset);
                    tile.TileFrameY = (short)(18 * (j - yTL));

                    if (applyPaint) WorldGen.paintTile(i, j, (byte)paint);
                }
            }

            // place TileEntity
            int id = TEWeaponsRack.Place(xTL, yTL); // creates the TileEntity at the top left corner of the multitile (TileEntities are always at the top-left multitile corner)

            //place item inside of the WeaponRack
            if (item > 0)
            {
                Item itemToPlace = new Item(item);
                if (TEWeaponsRack.FitsWeaponFrame(itemToPlace))
                {
                    TEWeaponsRack weaponRack = TileEntity.ByID[id] as TEWeaponsRack;
                    weaponRack.item = itemToPlace;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks the space and places a Mannequin at the given world position and equips it with items
        /// </summary>
        /// <param name="x">The bottom left x-coordinate of the Mannequin placement position</param>
        /// <param name="y">The bottom left y-coordinate of the Mannequin placement position</param>
        /// <param name="headArmorID">The ArmorID of the to be equipped head equipment</param>
        /// <param name="bodyArmorID">The ArmorID of the to be equipped body equipment</param>
        /// <param name="legsArmorID">The ArmorID of the to be equipped legs equipment</param>
        /// <param name="female">If the female version of the Mannequin shall be placed</param>
        /// <param name="direction">If the Mannequin shall look to the left (-1) or to the right (+1)</param>
        /// <returns><br/>Tupel item1 <b>success</b>: true if placement was successful
        ///          <br/>Tupel item2 <b>id</b>: The ID of the Mannequins TileEntity. -1 if placement was not successful</returns>
        public static (bool success, int dollID) PlaceMannequin(int x, int y, (int headArmorID, int bodyArmorID, int legsArmorID) armor, bool female = false, int direction = -1)
        {
            // check placement location
            for (int i = x; i <= x + 1; i++)
            {
                for (int j = y - 2; j <= y; j++)
                {
                    if (Main.tile[i, j].HasTile)
                    {
                        return (false, -1);
                    }
                }
            }

            // check floor tiles (where the Mannequin will stand on)
            for (int i = x; i <= x + 1; i++)
            { 
                if (!Main.tile[i, y + 1].HasTile) return (false, -1);
                if (!WorldGen.SolidTile2(i, y + 1)) return (false, -1);
            }

            // place Mannequin tiles 
            int style = 0;
            if (female) style = 2;
            WorldGen.PlaceObject(x, y, TileID.DisplayDoll, style: style, direction: direction);

            // check for correct tile placement
            for (int i = x; i <= x + 1; i++)
            {
                for (int j = y - 2; j <= y; j++)
                {
                    if (!(Main.tile[i, j].TileType == TileID.DisplayDoll))
                    {
                        return (false, -1);
                    }
                }
            }

            // place TileEntity
            int id = TEDisplayDoll.Place(x, y - 2); // creates the TileEntity at the top left corner of the multitile (TileEntities are always at the top-left multitile corner)
            TEDisplayDoll doll = TileEntity.ByID[id] as TEDisplayDoll;

            // equip armor
            doll.SetInventoryFromMannequin(armor.headArmorID * 100, armor.bodyArmorID * 100, armor.legsArmorID * 100);

            return (true, id);
        }

        /// <summary>
        /// Tries to place a tile repeatedly in a given space (a straight line), each time variating the placement position.
        /// <br/> There is also an adjustable initial "placement chance" to make the placement even more randomized.
        /// </summary>
        /// <param name="area">The straight line (must be a horizontal or vertical line!) where the object shall be placed at random. </param>
        /// <param name="blockedArea">And area that will be ignored when randomizing the placement position. If not desired make it an empty area. </param>
        /// <param name="type">TileID</param>
        /// <param name="style">Specification of the TileID (f.ex. TileID 215 (Campfire) -> style 3 = Frozen Campfire)</param>
        /// <param name="maxTry">Maximum count of tries to place the object</param>
        /// <param name="chance">Chance of the part to be actual placed (1% .. chance .. 100%) </param>
        /// <returns><br/>Tupel item1 <b>success</b>: true if placement was successful
        ///          <br/>Tupel item2 <b>xPlace</b>: x-coordinate of successful placed object, otherwise 0
        ///          <br/>Tupel item3 <b>yPlace</b>: y-coordinate of successful placed object, otherwise 0</returns>
        public static (bool success, int xPlace, int yPlace) TryPlaceTile(Rectangle2P area, Rectangle2P blockedArea, ushort type, int style = 0, byte maxTry = 5, byte chance = 100)
        {
            if (chance < 100) if ( !Chance.Perc(chance))   return (false, 0, 0);

            bool randomizeX = area.YTiles == 1;
            bool considerBlockedArea = !(blockedArea.IsEmpty());
            bool placementPosBlocked;

            int x, y, actTry = 0;
            Tile actTile;

            do
            {
                // randomize placement position
                do
                {
                    if (randomizeX)
                    {
                        x = WorldGen.genRand.Next(area.X0, area.X1 + 1); // X0 <= x <= X1
                        y = area.Y0;
                    }
                    else
                    {
                        x = area.X0;
                        y = WorldGen.genRand.Next(area.Y0, area.Y1 + 1); // Y0 <= y <= Y1
                    }

                    if (considerBlockedArea) placementPosBlocked = blockedArea.Contains(x, y);
                    else placementPosBlocked = false;
                }
                while ( placementPosBlocked);



                // try placement
                if (!Main.tile[x, y].HasTile)
                {
                    if (type == TileID.Fireplace) WorldGen.Place3x2(x, y, TileID.Fireplace); // Fireplace just doesn't work with "PlaceTile" don't know why
                    if (type == TileID.PotsSuspended) PlaceHangingLantern(x, y, TileID.PotsSuspended, style);
                    else WorldGen.PlaceTile(x, y, type, style: style);

                    // check placement
                    actTile = Main.tile[x, y];
                    if (actTile.HasTile && actTile.TileType == type) // placement successful
                    {
                        return (true, x, y);
                    }
                }

                actTry++;
            }
            while ( actTry < maxTry );

            return (false, 0, 0);
        }

        /// <summary>
        /// Places a Ghostly Stinkbug in a room.
        /// <br/> Order of tries: left or right wall (random), ceiling, floor
        /// </summary>
        /// <param name="hollowRoom">The room where the stinkbug shall be placed.
        ///    <br/> It must be the "hollow" room, meaning not the outside dimension where the walls are placed, but the inside of the room, where stuff is placed </param>
        /// <returns><br/>Tupel item1 <b>success</b>: true if placement was successful
        ///          <br/>Tupel item2 <b>xPlace</b>: x-coordinate of successful placed stinkbug, otherwise 0
        ///          <br/>Tupel item3 <b>yPlace</b>: y-coordinate of successful placed stinkbug, otherwise 0</returns>
        public static (bool success, int xPlace, int yPlace) PlaceStinkbug(Rectangle2P hollowRoom)
        {
            bool startLeft = Chance.Simple();
            bool stinkbugPlaced;
            (bool success, int x, int y) placeResult;
            Rectangle2P area1 = new Rectangle2P(hollowRoom.X0, hollowRoom.Y0, hollowRoom.X0, hollowRoom.Y1, "dummyString");
            Rectangle2P area2 = new Rectangle2P(hollowRoom.X1, hollowRoom.Y0, hollowRoom.X1, hollowRoom.Y1, "dummyString");

            // left or right wall
            if (startLeft)
            {
                placeResult = TryPlaceTile(area1, Rectangle2P.Empty, TileID.StinkbugHousingBlockerEcho, maxTry: 10);
                stinkbugPlaced = placeResult.success;
            }
            else
            {
                placeResult = TryPlaceTile(area2, Rectangle2P.Empty, TileID.StinkbugHousingBlockerEcho, maxTry: 10);
                stinkbugPlaced = placeResult.success;
            }
            if ( stinkbugPlaced ) return placeResult;

            // the other wall
            if (startLeft)
            {
                placeResult = TryPlaceTile(area2, Rectangle2P.Empty, TileID.StinkbugHousingBlockerEcho, maxTry: 10);
                stinkbugPlaced = placeResult.success;
            }
            else
            {
                placeResult = TryPlaceTile(area1, Rectangle2P.Empty, TileID.StinkbugHousingBlockerEcho, maxTry: 10);
                stinkbugPlaced = placeResult.success;
            }
            if (stinkbugPlaced) return placeResult;

            // ceiling
            area1 = new Rectangle2P(hollowRoom.X0, hollowRoom.Y0, hollowRoom.X1, hollowRoom.Y0, "dummyString");
            placeResult = TryPlaceTile(area1, Rectangle2P.Empty, TileID.StinkbugHousingBlockerEcho, maxTry: 10);
            stinkbugPlaced = placeResult.success;
            if (stinkbugPlaced) return placeResult;

            // floor
            area1 = new Rectangle2P(hollowRoom.X0, hollowRoom.Y1, hollowRoom.X1, hollowRoom.Y1, "dummyString");
            placeResult = TryPlaceTile(area1, Rectangle2P.Empty, TileID.StinkbugHousingBlockerEcho, maxTry: 10);
            stinkbugPlaced = placeResult.success;
            if (stinkbugPlaced) return placeResult;

            return (false, 0, 0); //if you reach this point something went terribly wrong....most probably the room is not the hollow room
        }

        /// <summary>
        /// Checks if the rectangular area is free of any other tiles
        /// <br/>Has an option for checking if background walls are present and another option to place some at once if missing
        /// </summary>
        /// <param name="area">The to be checked area</param>
        /// <param name="checkWall">If the area shall be checked if background walls are present</param>
        /// <param name="wallType">If <i>checkWall = true</i> a value > 0 will place background walls where they are missing</param>
        public static bool CheckFree(Rectangle2P area, bool checkWall = false, int wallType = 0)
        {
            for (int i = area.X0; i <= area.X1; i++)
            {
                for (int j = area.Y0; j <= area.Y1; j++)
                {
                    if (Main.tile[i, j].HasTile)  // check if area is free
                    {
                        return false;
                    }
                    if (checkWall)
                    {
                        if (Main.tile[i, j].WallType == 0) // and has background wall
                        {
                            if (wallType > 0) WorldGen.PlaceWall(i, j, wallType);
                            else return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Places background walls in the given area
        /// </summary>
        /// <param name="area">The to be filled area</param>
        /// <param name="wallType">The ID of the to be placed wall</param>
        public static bool PlaceWallArea(Rectangle2P area, int wallType)
        {
            // pre-checks
            if (wallType <= 0) return false;

            for (int i = area.X0; i <= area.X1; i++)
            {
                for (int j = area.Y0; j <= area.Y1; j++)
                {
                    WorldGen.KillWall(i, j);
                    WorldGen.PlaceWall(i, j, wallType);
                }
            }

            return true;
        }

        /// <summary>
        /// Checks for free space, places a tile (option to paint it) and attaches a banner to it
        /// </summary>
        /// <param name="x">The x coordinate of where the banner shall be placed at</param>
        /// <param name="y">The y coordinate of where the banner shall be placed at</param>
        /// <param name="bannerStyle">The BannerID of the to-be-placed banner</param>
        /// <param name="tileType">The TileID of the tile, where the banner will be attached to</param>
        /// <param name="tileStyle">The Style of the tile, where the banner will be attached to</param>
        /// <param name="paintType">The PaintID of the tile, where the banner will be attached to</param>
        public static bool PlaceTileAndBanner(int x, int y, int bannerStyle, int tileType, int tileStyle, int paintType = 0)
        {
            // pre-checks
            if (bannerStyle < 0 || tileType <= 0 || tileStyle < 0) return false;

            // check free
            for (int j = y - 1; j <= y + 2; j++)
            {
                if (Main.tile[x, j].HasTile) return false;
            }

            WorldGen.PlaceTile(x, y - 1, tileType, style: tileStyle); // Tile
            if (paintType > 0)   WorldGen.paintTile(x, y - 1, (byte)paintType);

            WorldGen.PlaceObject(x, y, TileID.Banners, style: bannerStyle); // Banner

            if (!Main.tile[x, y].HasTile)  return false; // banner wasn't created, who knows why!

            return true;
        }

    }

    internal class Chance
    {
        /// <summary> Returns true every "1 out of x times" (2 .. x .. maxInt)</summary>
        public static bool OneOut(int x)
        {
            return WorldGen.genRand.NextBool(Math.Max(1, x));
        }

        /// <summary> Returns true in x percent of cases (0 .. x .. 100) </summary>
        public static bool Perc(float x)
        {
            return (WorldGen.genRand.NextFloat() < (Math.Clamp(x, 0f, 100f) / 100.0f));
        }

        /// <summary> Returns true in x percent of cases (0 .. x .. 1)</summary>
        public static bool Perc2(float x)
        {
            return (WorldGen.genRand.NextFloat() < Math.Clamp(x, 0f, 1f));
        }

        /// <summary> Just a shorter way for WorldGen.genRand.NextBool()</summary>
        public static bool Simple()
        {
            return (WorldGen.genRand.NextBool());
        }
    }

    public struct Rectangle2P
    {
        public static readonly Rectangle2P Empty; // an empty rectangle for cases when one wants to say "there is nothing"

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
        /// Adjusts the location of this Rectangle2Point by the specified amount.
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
        /// Creates a copy of this Rectangle2Point and gives the possibility to alter its location by offsets.
        /// </summary>
        public Rectangle2P CloneAndMove(int xOffset = 0, int yOffset = 0)
        {
            return new Rectangle2P(this.X0 + xOffset, this.Y0 + yOffset, this.X1 + xOffset, this.Y1 + yOffset, "dummyString");
        }

        /// <summary>
        /// Determines if the specified point is contained (including the frame) within the rectangular region defined by this Rectangle2Point.
        /// </summary>
        public readonly bool Contains(int x, int y) => (X0 <= x) && (x <= X1) && (Y0 <= y) && (y <= Y1);

        /// <summary>
        /// Checks if the Rectangle2Point is empty
        /// </summary>
        public readonly bool IsEmpty() => (this.Equals(Empty));

        /// <summary>
        /// Returns if the Rectangle2Point has an even amount of xTiles
        /// </summary>
        public readonly bool IsEvenX() => (this.xTiles % 2 == 0);

        /// <summary>
        /// Returns if the Rectangle2Point has an even amount of yTiles
        /// </summary>
        public readonly bool IsEvenY() => (this.yTiles % 2 == 0);

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
}
