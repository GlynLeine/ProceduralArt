using System;
using UnityEngine;

// a point in 2D, sorted by y-coordinate
public class Point : IComparable<Point>
{
    public Vector2 position;
    public float x => position.x;
    public float y => position.y;

    public Point(float x, float y)
    {
        position.x = x;
        position.y = y;
    }

    public Point(Vector2 position)
    {
        this.position = position;
    }

    public int CompareTo(Point other)
    {
        if (y == other.y)
        {
            if (x == other.x)
                return 0;
            else if (x > other.x)
                return 1;
            else
                return -1;
        }
        else if (y > other.y)
        {
            return 1;
        }
        else
        {
            return -1;
        }
    }

    public string toString()
    {
        return "(" + x + ", " + y + ")";
    }
}
