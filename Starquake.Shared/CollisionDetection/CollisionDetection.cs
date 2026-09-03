using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StarquakeXNA.Helpers;

namespace StarquakeXNA
{
    public static class CollisionDetection
    {
        private static int obj1CenterX;
        private static int obj1CenterY;

        private static int obj2CenterX;
        private static int obj2CenterY;

        private static int distanceX;
        private static int distanceY;

        private static bool coordColY;
        private static bool coordColX;

        /// <summary>
        /// Checks for a collision between two objects and returns the amount of distance that it is legal to travel
        /// </summary>
        /// <param name="Direction"></param>
        /// <param name="BoundingBox1Velocity"></param>
        /// <param name="BoundingBox1"></param>
        /// <param name="BoundingBox2"></param>
        /// <returns></returns>
        public static int CheckDirection(enDirection Direction, int BoundingBox1Velocity,
                                          Rectangle BoundingBox1, Rectangle BoundingBox2)
        {
            return CheckDirection(
                                    Direction,
                                    BoundingBox1Velocity,
                                    BoundingBox1.X,
                                    BoundingBox1.Y,
                                    BoundingBox1.Width,
                                    BoundingBox1.Height,
                                    BoundingBox2.X,
                                    BoundingBox2.Y,
                                    BoundingBox2.Width,
                                    BoundingBox2.Height);
        }

        /// <summary>
        /// Checks for a collision between two objects and returns the amount of distance that it is legal to travel
        /// </summary>
        /// <param name="Direction"></param>
        /// <param name="Object1Velocity"></param>
        /// <param name="Obj1X"></param>
        /// <param name="Obj1Y"></param>
        /// <param name="Obj1W"></param>
        /// <param name="Obj1H"></param>
        /// <param name="Obj2X"></param>
        /// <param name="Obj2Y"></param>
        /// <param name="Obj2W"></param>
        /// <param name="Obj2H"></param>
        /// <returns></returns>
        public static int CheckDirection(enDirection Direction, int Obj1Velocity,
                                          int Obj1X, int Obj1Y, int Obj1W, int Obj1H,
                                          int Obj2X, int Obj2Y, int Obj2W, int Obj2H)
        {
            if (Direction == enDirection.Up)
            {
                int AmountThatCanBeMoved = 0;

                // Count up in steps to the full velocity requested to see if any collision is taking place
                for (int a = 0; a < Obj1Velocity; a++)
                {
                    if (CollisionDetection.Check(Obj1X, Obj1Y - (a + 1), Obj1W, Obj1H, Obj2X, Obj2Y, Obj2W, Obj2H) == false)
                    {
                        AmountThatCanBeMoved++;
                    }
                    else
                    {
                        break;
                    }
                }

                return AmountThatCanBeMoved;
            }
            else if (Direction == enDirection.Down)
            {
                int AmountThatCanBeMoved = 0;

                // Count up in steps to the full velocity requested to see if any collision is taking place
                for (int a = 0; a < Obj1Velocity; a++)
                {
                    if (CollisionDetection.Check(Obj1X, Obj1Y + (a + 1), Obj1W, Obj1H, Obj2X, Obj2Y, Obj2W, Obj2H) == false)
                    {
                        AmountThatCanBeMoved++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (AmountThatCanBeMoved == 0)
                {

                }

                return AmountThatCanBeMoved;
            }
            else if (Direction == enDirection.Left)
            {
                int AmountThatCanBeMoved = 0;

                // Count up in steps to the full velocity requested to see if any collision is taking place
                for (int a = 0; a < Obj1Velocity; a++)
                {
                    if (CollisionDetection.Check(Obj1X - (a + 1), Obj1Y, Obj1W, Obj1H, Obj2X, Obj2Y, Obj2W, Obj2H) == false)
                    {
                        AmountThatCanBeMoved++;
                    }
                    else
                    {
                        break;
                    }
                }

                return AmountThatCanBeMoved;
            }
            else if (Direction == enDirection.Right)
            {
                int AmountThatCanBeMoved = 0;

                // Count up in steps to the full velocity requested to see if any collision is taking place
                for (int a = 0; a < Obj1Velocity; a++)
                {
                    if (CollisionDetection.Check(Obj1X + (a + 1), Obj1Y, Obj1W, Obj1H, Obj2X, Obj2Y, Obj2W, Obj2H) == false)
                    {
                        AmountThatCanBeMoved++;
                    }
                    else
                    {
                        break;
                    }
                }

                return AmountThatCanBeMoved;
            }

            return 0;
        }

        /// <summary>
        /// Checks for a collision and if it has taken place return the direction it occurred in
        /// </summary>
        /// <param name="Direction"></param>
        /// <param name="Object1Velocity"></param>
        /// <param name="Obj1X"></param>
        /// <param name="Obj1Y"></param>
        /// <param name="Obj1W"></param>
        /// <param name="Obj1H"></param>
        /// <param name="Obj2X"></param>
        /// <param name="Obj2Y"></param>
        /// <param name="Obj2W"></param>
        /// <param name="Obj2H"></param>
        /// <returns></returns>
        public static enDirection CheckCollisionDirection(int Obj1Velocity,
                                          int Obj1X, int Obj1Y, int Obj1W, int Obj1H,
                                          int Obj2X, int Obj2Y, int Obj2W, int Obj2H)
        {
            if (CollisionDetection.CheckDirection(enDirection.Up, Obj1Velocity, new Rectangle(Obj1X, Obj1Y, Obj1W, Obj1H), new Rectangle(Obj2X, Obj2Y, Obj2W, Obj2H)) == 0)
            {
                if (CollisionDetection.CheckDirection(enDirection.Left, Obj1Velocity, new Rectangle(Obj1X, Obj1Y, Obj1W, Obj1H), new Rectangle(Obj2X, Obj2Y, Obj2W, Obj2H)) == 0)
                {
                    return enDirection.UpLeft;
                }
                else if (CollisionDetection.CheckDirection(enDirection.Right, Obj1Velocity, new Rectangle(Obj1X, Obj1Y, Obj1W, Obj1H), new Rectangle(Obj2X, Obj2Y, Obj2W, Obj2H)) == 0)
                {
                    return enDirection.UpRight;
                }
                else return enDirection.Up;
            }

            if (CollisionDetection.CheckDirection(enDirection.Left, Obj1Velocity, new Rectangle(Obj1X, Obj1Y, Obj1W, Obj1H), new Rectangle(Obj2X, Obj2Y, Obj2W, Obj2H)) == 0)
            {
                if (CollisionDetection.CheckDirection(enDirection.Up, Obj1Velocity, new Rectangle(Obj1X, Obj1Y, Obj1W, Obj1H), new Rectangle(Obj2X, Obj2Y, Obj2W, Obj2H)) == 0)
                {
                    return enDirection.UpLeft;
                }
                else if (CollisionDetection.CheckDirection(enDirection.Down, Obj1Velocity, new Rectangle(Obj1X, Obj1Y, Obj1W, Obj1H), new Rectangle(Obj2X, Obj2Y, Obj2W, Obj2H)) == 0)
                {
                    return enDirection.DownLeft;
                }
                else return enDirection.Left;
            }

            if (CollisionDetection.CheckDirection(enDirection.Down, Obj1Velocity, new Rectangle(Obj1X, Obj1Y, Obj1W, Obj1H), new Rectangle(Obj2X, Obj2Y, Obj2W, Obj2H)) == 0)
            {
                if (CollisionDetection.CheckDirection(enDirection.Left, Obj1Velocity, new Rectangle(Obj1X, Obj1Y, Obj1W, Obj1H), new Rectangle(Obj2X, Obj2Y, Obj2W, Obj2H)) == 0)
                {
                    return enDirection.DownLeft;
                }
                else if (CollisionDetection.CheckDirection(enDirection.Right, Obj1Velocity, new Rectangle(Obj1X, Obj1Y, Obj1W, Obj1H), new Rectangle(Obj2X, Obj2Y, Obj2W, Obj2H)) == 0)
                {
                    return enDirection.DownRight;
                }
                else return enDirection.Down;
            }

            if (CollisionDetection.CheckDirection(enDirection.Right, Obj1Velocity, new Rectangle(Obj1X, Obj1Y, Obj1W, Obj1H), new Rectangle(Obj2X, Obj2Y, Obj2W, Obj2H)) == 0)
            {
                if (CollisionDetection.CheckDirection(enDirection.Up, Obj1Velocity, new Rectangle(Obj1X, Obj1Y, Obj1W, Obj1H), new Rectangle(Obj2X, Obj2Y, Obj2W, Obj2H)) == 0)
                {
                    return enDirection.UpRight;
                }
                else if (CollisionDetection.CheckDirection(enDirection.Down, Obj1Velocity, new Rectangle(Obj1X, Obj1Y, Obj1W, Obj1H), new Rectangle(Obj2X, Obj2Y, Obj2W, Obj2H)) == 0)
                {
                    return enDirection.DownRight;
                }
                else return enDirection.Right;
            }

            return enDirection.None;
        }
                                          

        public static bool Check(Rectangle BoundingBox1, Rectangle BoundingBox2)
        {
            return Check(BoundingBox1.X, BoundingBox1.Y, BoundingBox1.Width, BoundingBox1.Height,
                         BoundingBox2.X, BoundingBox2.Y, BoundingBox2.Width, BoundingBox2.Height);
        }

        public static bool Check(int Obj1X, int Obj1Y, int Obj1W, int Obj1H,
                                 int Obj2X, int Obj2Y, int Obj2W, int Obj2H)
        {
            coordColY = false;
            coordColX = false;

            // Calculate centre point of each object
            // object 1
            obj1CenterX = Obj1X + (Obj1W / 2);
            obj1CenterY = Obj1Y + (Obj1H / 2);

            // object 2
            obj2CenterX = Obj2X + (Obj2W / 2);
            obj2CenterY = Obj2Y + (Obj2H / 2);

            // Calculate the distances between the two center points
            distanceX = obj1CenterX - obj2CenterX;
            distanceY = obj1CenterY - obj2CenterY;

            // Identify where the objects are in relation to each other
            // Check y coordinates
            if (obj1CenterY - obj2CenterY > 0)
            {
                distanceY = obj1CenterY - obj2CenterY;
            }
            else
            {
                distanceY = obj2CenterY - obj1CenterY;
            }

            // Calculate distance between the two center points
            if (obj1CenterX - obj2CenterX > 0)
            {
                distanceX = obj1CenterX - obj2CenterX;
            }
            else
            {
                distanceX = obj2CenterX - obj1CenterX;
            }

            // Check for a collission
            if (distanceX < (Obj1W / 2) + (Obj2W / 2))
            {
                coordColX = true;
            }

            if (distanceY < (Obj1H / 2) + (Obj2H / 2))
            {
                coordColY = true;
            }

            // Return the results
            if (coordColX == true && coordColY == true)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
