using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace AStarXna
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Node[,] map;
        Texture2D tileTexture;
        private Texture2D tileTextureSmall;
        private Node startNode;

        private Node endNode;
        private GameTime gameTime;
        private List<Node> path;
        List<Node> closedNodes = new List<Node>();
        List<Node> openNodes = new List<Node>();
        //List<Node> searchedNodes = new List<Node>()
        private KeyboardState currentKeyboard;
        private KeyboardState previousKeyboard;

        private MouseState currentMouseState;
        private MouseState previousMouseState;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            //Create a map of nodes
            map = new Node[Window.ClientBounds.Width/32,Window.ClientBounds.Height/32];
            for (int y = 0; y < map.GetLength(1); y++)
            {
                for (int x = 0; x < map.GetLength(0); x++)
                {
                    map[x, y] = new Node(false, x*32, y*32, Color.Gray);
                }
            }
            startNode = map[0, 0];
            endNode = map[1, 1];
            path = new List<Node>();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            this.IsMouseVisible = true;
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
            tileTexture = Content.Load<Texture2D>("tile");
            tileTextureSmall = Content.Load<Texture2D>("tileSmall");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            this.gameTime = gameTime;
            currentMouseState = Mouse.GetState();
            currentKeyboard = Keyboard.GetState();
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            //Adds a wall 
            if (currentMouseState.LeftButton == ButtonState.Pressed &&
                previousMouseState.LeftButton == ButtonState.Released)
            {
                int gridX = (int)MathHelper.Clamp(currentMouseState.X / 32,0,map.GetLength(0)-1);
                int gridY = (int)MathHelper.Clamp(currentMouseState.Y / 32, 0, map.GetLength(1)-1);

                map[gridX, gridY].color = Color.Black;
                if (map[gridX, gridY].isWall)
                {
                    map[gridX, gridY].isWall = false;
                }
                else
                {
                    map[gridX, gridY].isWall = true;
                }

            }
            //Change start and stop
            if (currentMouseState.RightButton == ButtonState.Pressed &&
                previousMouseState.RightButton == ButtonState.Released)
            {   
                int gridX = currentMouseState.X / 32;
                int gridY = currentMouseState.Y / 32;
                startNode = map[gridX, gridY];
            }
            if (currentMouseState.MiddleButton == ButtonState.Pressed &&
                previousMouseState.MiddleButton == ButtonState.Released)
            {
                int gridX = currentMouseState.X / 32;
                int gridY = currentMouseState.Y / 32;
                endNode = map[gridX, gridY];
            }
            //Generate map
            if (currentKeyboard.IsKeyDown(Keys.Space) && previousKeyboard.IsKeyUp(Keys.Space))
            {
                generatePath();
            }
            //Reset the map
            if (currentKeyboard.IsKeyDown(Keys.R) && previousKeyboard.IsKeyUp(Keys.R))
            {
                foreach (Node n in map)
                {
                    n.isWall = false;
                }
                openNodes.Clear();
                closedNodes.Clear();
                path.Clear();
            }

            previousMouseState = Mouse.GetState();
            previousKeyboard = Keyboard.GetState();
            base.Update(gameTime);
        }
        /// <summary>
        /// Calculates the distance between each node
        /// </summary>
        /// <param name="from">The node to calculate from</param>
        /// <param name="to">The Node to calculate to </param>
        /// <returns>The distance in number of nodes away times 10</returns>
        /// <example>
        /// From 1,1 to 2,2 Returns 14 as the the hypotenus/distance of 1,1 is 1.4
        /// then we multiply this by 10 
        /// </example>
        public int distanceBetweenNodes(Node from, Node to)
        {
            int horDist = Math.Abs(to.positionX - from.positionX) / 32;
            int vertDist = Math.Abs(to.positionY - from.positionY) / 32;

            return (int)(Math.Sqrt(horDist * horDist + vertDist * vertDist)*10);
        }
        /// <summary>
        /// Generates our path, Uses values in class so nothing is passed as arguments
        /// or returned from the method, does not handle errors that well yet :P
        /// </summary>
        private void generatePath()
        {
            ///Reset previous values
            foreach (Node n in map)
            {
                n.Parent=null;
                n.costSoFar = 0;
                n.FCost = 0;
            }
            closedNodes.Clear();
            openNodes.Clear();
            bool foundPath = false;

            ///Set the current node to our start node
            Node currentNode = startNode;
            //Add the start node to the possible paths we can travel/open nodes
            openNodes.Add(startNode);

            //As long as there are nodes that can be visited, continue
            while (openNodes.Count>0)
            {
                //////STEP ONE: 
                /// Find the item in the list by iteration through it
                /// This was the fastest way when prototyping, might
                /// change it to sorting the list or using a priority queue later
                int minVal = int.MaxValue;
                foreach (Node openNode in openNodes)
                {
                    if (openNode.FCost <= minVal)
                    {
                        currentNode = openNode;
                        minVal = openNode.FCost;
                    }
                }
                //If the current node is our end node we found the path, yay
                if (currentNode == endNode)
                {
                    Debug.WriteLine("found path");
                    foundPath = true;
                    break;
                }
                ///STEP TWO:
                /// Find all our neighboors and set the F=G+H cost of them
                //Get the neighboors (typo?)
                Node[] neighboors = getNeighboors(currentNode);
                //Iterate through all of the nodes
                foreach (Node n in neighboors)
                {
                    //Is it a wall we could not care less, dont bother to continue
                    //jump to next iteration
                    if (n.isWall)
                    {
                        continue;
                    }
                    //Calculate our distance traveled if we we're to travel to this
                    int distanceTraveled = currentNode.costSoFar + distanceBetweenNodes(currentNode,n);
                    Debug.WriteLine("Travel Cost: " + distanceBetweenNodes(currentNode, n)+"\nHeuristic Distance: "+Heuristic(n));
                    //If its not discovered yet lets add it
                    if (!openNodes.Contains(n) && !closedNodes.Contains(n))
                    {
                        ///CostSoFar = G the distance we have traveled
                        n.costSoFar = distanceTraveled;
                        //Calucate the FCost
                        n.FCost = n.costSoFar + Heuristic(n);
                        //Set its parent to the current node so we later can traverse backwards
                        n.Parent = currentNode;
                        //Add it to the open nodes list
                        openNodes.Add(n);
                    }
                    else
                    {
                        //If it is in the closed list or open list (most intresting we want to update it also
                        if (distanceTraveled < n.costSoFar)
                        {
                            n.costSoFar = distanceTraveled;
                            n.FCost = distanceTraveled + Heuristic(n);
                            n.Parent = currentNode;
                        }
                    }
                }
                //Remove it from the open and add it to the closed
                openNodes.Remove(currentNode);
                closedNodes.Add(currentNode);
            }
            //If we did fid a path backtrace it
            if (foundPath)
            {
               
                Node backTrackNode = currentNode;
                //Clear the path and add our first
                path.Clear();
                path.Add(backTrackNode);
                //if we have not yet met our parent, continue
                while (backTrackNode.Parent != startNode)
                {
                    //Add it to our path and set to next
                    path.Add(backTrackNode.Parent);
                    backTrackNode = backTrackNode.Parent;
                }
                //Reverse the path so it goes in the correct direction
                path.Reverse();
            }

        }
        /// <summary>
        /// Gets the neighboors of a node
        /// </summary>
        /// <param name="node">The node to find parent of</param>
        /// <returns>An array containing arrays that are adjacent of the node</returns>
        private Node[] getNeighboors(Node node)
        {
            int nodeIndexX = node.positionX/32;
            int nodeIndexY = node.positionY/32;
            List<Node> nodesAsNeighboors = new List<Node>();
            for (int y = -1; y < 2; y++)
            {
                for (int x = -1; x < 2; x++)
                {
                    //dont include ourselves
                    if (x == 0 && y == 0)
                        continue;
                    int xx = nodeIndexX + x;
                    int yy = nodeIndexY + y;
                    if (xx >= 0 && xx < map.GetLength(0) && yy >= 0 && yy < map.GetLength(1))
                    {
                        nodesAsNeighboors.Add(map[xx, yy]);
                    }
                }
            }
            return nodesAsNeighboors.ToArray();
        }
        /// <summary>
        /// Generate the heurstic value from node to end
        /// </summary>
        /// <param name="node">The node of which to generate the value</param>
        /// <returns>The heuristic value</returns>
        /// <remarks>Uses eucalean heuristic</remarks>
        public int Heuristic(Node node)
        {
            int horDist = Math.Abs(endNode.positionX - node.positionX) / 32;
            int vertDist = Math.Abs(endNode.positionY - node.positionY) / 32;
            
            //return (int)Math.Sqrt(horDist * horDist + vertDist * vertDist)*10;
            return ((horDist + vertDist != 0) ? (horDist + vertDist - 1) : 0)*10;

        }
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {

            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin();
            //Draw the map
            for (int y = 0; y < map.GetLength(1); y++)
            {
                for (int x = 0; x < map.GetLength(0); x++)
                {
                    if (map[x, y].isWall)
                    {
                        spriteBatch.Draw(tileTexture, new Vector2(map[x, y].positionX, map[x, y].positionY),
                                         Color.Black);
                    }
                    else
                    {
                        spriteBatch.Draw(tileTexture, new Vector2(map[x, y].positionX, map[x, y].positionY),
                                        Color.Gray);
                    }
                }
            }
            ///Draw our helpers
            foreach (Node n in openNodes)
            {
                spriteBatch.Draw(tileTextureSmall, new Vector2(n.positionX + 8, n.positionY + 8), Color.Yellow);
            }
            foreach (Node n in closedNodes)
            {
                spriteBatch.Draw(tileTextureSmall, new Vector2(n.positionX + 8, n.positionY + 8), Color.Orange);
            }
            foreach (Node n in path)
            {
                spriteBatch.Draw(tileTextureSmall, new Vector2(n.positionX + 8, n.positionY + 8), Color.Blue);
            }

            //foreach (Node n in getNeighboors(startNode))
            //{
            //    spriteBatch.Draw(tileTextureSmall, new Vector2(n.positionX+8, n.positionY+8), Color.Blue);
            //}
            spriteBatch.Draw(tileTexture, new Vector2(startNode.positionX, startNode.positionY), Color.Green);
            spriteBatch.Draw(tileTexture, new Vector2(endNode.positionX, endNode.positionY), Color.Red);

 
            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
