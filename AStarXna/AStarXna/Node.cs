using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;


namespace AStarXna
{
    public class Node
    {
        public bool isWall;
        public Color color;
        public int positionX;
        public int positionY;
        public Node Parent;
        public int costSoFar;
        public int FCost;

        public Node(bool isWall, int positionX, int positionY, Color color)
        {
            this.color = color;
            this.isWall = isWall;
            this.positionX = positionX;
            this.positionY = positionY;
        }
        public Node(int x, int y)
        {
            this.color = Color.Yellow;
            this.isWall = false;
            this.positionX = x;
            this.positionY = y;
        }

    }
}
