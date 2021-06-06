using System;
using UnityEngine;

// an event is either a site or circle event for the sweep line to process
public class Event : IComparable<Event>
{
    // a site event is when the point is a site
    public static int SITE_EVENT = 0;

    // a circle event is when the point is a vertex of the voronoi diagram/parabolas
    public static int CIRCLE_EVENT = 1;

    public Point p;
    public int type;
    public Parabola arc; // only if circle event

    public Event(Point p, int type)
    {
        this.p = p;
        this.type = type;
        arc = null;
    }

    public int CompareTo(Event other)
    {
        return p.CompareTo(other.p);
    }
}