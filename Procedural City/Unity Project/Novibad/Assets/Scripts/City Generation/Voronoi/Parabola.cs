﻿using UnityEngine;

// represents the beach line
// can either be a site that is the center of a parabola
// or can be a vertex that bisects two sites
public class Parabola
{

    public static int IS_FOCUS = 0;
    public static int IS_VERTEX = 1;

    public int type;
    public Point point; // if is focus
    public Edge edge; // if is vertex
    public Event voronoiEvent; // a parabola with a focus can disappear in a circle event

    public Parabola parent;
    public Parabola child_left;
    public Parabola child_right;


    public Parabola()
    {
        type = IS_VERTEX;
    }

    //public Parabola(Parabola source)
    //{
    //    type = source.type;
    //    point = source.point;
    //    edge = source.edge;
    //    voronoiEvent = source.voronoiEvent;

    //    parent = source.parent;
    //    child_left = source.child_left;
    //    child_right = source.child_right;
    //}

    public Parabola(Point p)
    {
        point = p;
        type = IS_FOCUS;
    }

    public void setLeftChild(Parabola p)
    {
        child_left = p;
        p.parent = this;
    }

    public void setRightChild(Parabola p)
    {
        child_right = p;
        p.parent = this;
    }

    public string toString()
    {
        if (type == IS_FOCUS)
        {
            return "Focus at " + point;
        }
        else
        {
            return "Vertex/Edge beginning at " + edge.start;
        }
    }

    // returns the closest left site (focus of parabola) 
    public static Parabola getLeft(Parabola p)
    {
        return getLeftChild(getLeftParent(p));
    }

    // returns closest right site (focus of parabola)
    public static Parabola getRight(Parabola p)
    {
        return getRightChild(getRightParent(p));
    }

    // returns the closest parent on the left
    public static Parabola getLeftParent(Parabola p)
    {
        Parabola parent = p.parent;
        if (parent == null) return null;
        Parabola last = p;
        while (parent.child_left == last)
        {
            if (parent.parent == null) return null;
            last = parent;
            parent = parent.parent;
        }
        return parent;
    }

    // returns the closest parent on the right
    public static Parabola getRightParent(Parabola p)
    {
        Parabola parent = p.parent;
        if (parent == null) return null;
        Parabola last = p;
        while (parent.child_right == last)
        {
            if (parent.parent == null) return null;
            last = parent;
            parent = parent.parent;
        }
        return parent;
    }

    // returns closest site (focus of another parabola) to the left
    public static Parabola getLeftChild(Parabola p)
    {
        if (p == null) return null;
        Parabola child = p.child_left;
        while (child.type == IS_VERTEX) child = child.child_right;
        return child;
    }

    // returns closest site (focus of another parabola) to the right
    public static Parabola getRightChild(Parabola p)
    {
        if (p == null) return null;
        Parabola child = p.child_right;
        while (child.type == IS_VERTEX) child = child.child_left;
        return child;
    }
}