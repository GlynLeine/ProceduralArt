using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// the voronoi diagram (a set of edges) for a set of points (sites)
public class Voronoi
{
    public List<Point> sites;
    public List<Edge> edges; // edges on Voronoi diagram
    SortedSet<Event> events; // priority queue represents sweep line
    Parabola root; // binary search tree represents beach line

    // size of StdDraw window
    float width = 1;
    float height = 1;

    float ycurr; // current y-coord of sweep line

    public Voronoi(List<Vector2> points, MonoBehaviour owner)
    {
        sites = new List<Point>();
        foreach (Vector2 point in points)
            sites.Add(new Point(point));
        edges = new List<Edge>();
    }

    public IEnumerator generateVoronoi()
    {
        events = new SortedSet<Event>();
        foreach (Point p in sites)
        {
            events.Add(new Event(p, Event.SITE_EVENT));
        }

        // process events (sweep line)
        while (events.Count > 0)
        {
            Event e = events.Min;
            ycurr = e.p.y;

            if (e.type == Event.SITE_EVENT)
            {
                handleSite(e.p);
            }
            else
            {
                handleCircle(e);
            }

            events.Remove(e);
            yield return null;
        }

        ycurr = width + height;

        endEdges(root); // close off any dangling edges

        // get rid of those crazy inifinte lines
        foreach (Edge e in edges)
        {
            if (e.neighbor != null)
            {
                e.start = e.neighbor.end;
                e.neighbor = null;
            }
        }
    }

    // end all unfinished edges
    private void endEdges(Parabola p)
    {
        if (p.type == Parabola.IS_FOCUS)
        {
            p = null;
            return;
        }

        float x = getXofEdge(p);
        p.edge.end = new Point(x, p.edge.slope * x + p.edge.yint);
        edges.Add(p.edge);

        endEdges(p.child_left);
        endEdges(p.child_right);

        p = null;
    }

    // processes site event
    private void handleSite(Point p)
    {
        // base case
        if (root == null)
        {
            root = new Parabola(p);
            return;
        }

        // find parabola on beach line right above p
        Parabola par = getParabolaByX(p.x);
        if (par.voronoiEvent != null)
        {
            events.Remove(par.voronoiEvent);
            par.voronoiEvent = null;
        }

        // create new dangling edge; bisects parabola focus and p
        Point start = new Point(p.x, getY(par.point, p.x));
        Edge el = new Edge(start, par.point, p);
        Edge er = new Edge(start, p, par.point);
        el.neighbor = er;
        er.neighbor = el;
        par.edge = el;
        par.type = Parabola.IS_VERTEX;

        // replace original parabola par with p0, p1, p2
        Parabola p0 = new Parabola(par.point);
        Parabola p1 = new Parabola(p);
        Parabola p2 = new Parabola(par.point);

        par.setLeftChild(p0);
        par.setRightChild(new Parabola());
        par.child_right.edge = er;
        par.child_right.setLeftChild(p1);
        par.child_right.setRightChild(p2);

        checkCircleEvent(p0);
        checkCircleEvent(p2);
    }

    // process circle voronoiEvent
    private void handleCircle(Event e)
    {

        // find p0, p1, p2 that generate this voronoiEvent from left to right
        Parabola p1 = e.arc;
        Parabola xl = Parabola.getLeftParent(p1);
        Parabola xr = Parabola.getRightParent(p1);
        Parabola p0 = Parabola.getLeftChild(xl);
        Parabola p2 = Parabola.getRightChild(xr);

        // remove associated events since the points will be altered
        if (p0.voronoiEvent != null)
        {
            events.Remove(p0.voronoiEvent);
            p0.voronoiEvent = null;
        }
        if (p2.voronoiEvent != null)
        {
            events.Remove(p2.voronoiEvent);
            p2.voronoiEvent = null;
        }

        Point p = new Point(e.p.x, getY(p1.point, e.p.x)); // new vertex

        // end edges!
        xl.edge.end = p;
        xr.edge.end = p;
        edges.Add(xl.edge);
        edges.Add(xr.edge);

        // start new bisector (edge) from this vertex on which ever original edge is higher in tree
        Parabola higher = new Parabola();
        Parabola par = p1;
        while (par != root)
        {
            par = par.parent;
            if (par == xl) higher = xl;
            if (par == xr) higher = xr;
        }
        higher.edge = new Edge(p, p0.point, p2.point);

        // delete p1 and parent (boundary edge) from beach line
        Parabola gparent = p1.parent.parent;
        if (p1.parent.child_left == p1)
        {
            if (gparent.child_left == p1.parent) gparent.setLeftChild(p1.parent.child_right);
            if (gparent.child_right == p1.parent) gparent.setRightChild(p1.parent.child_right);
        }
        else
        {
            if (gparent.child_left == p1.parent) gparent.setLeftChild(p1.parent.child_left);
            if (gparent.child_right == p1.parent) gparent.setRightChild(p1.parent.child_left);
        }

        p1.parent = null;
        p1 = null;

        checkCircleEvent(p0);
        checkCircleEvent(p2);
    }

    // adds circle voronoiEvent if foci a, b, c lie on the same circle
    private void checkCircleEvent(Parabola b)
    {
        Parabola lp = Parabola.getLeftParent(b);
        Parabola rp = Parabola.getRightParent(b);

        if (lp == null || rp == null) return;

        Parabola a = Parabola.getLeftChild(lp);
        Parabola c = Parabola.getRightChild(rp);

        if (a == null || c == null || a.point == c.point) return;

        if (ccw(a.point, b.point, c.point) != 1) return;

        // edges will intersect to form a vertex for a circle voronoiEvent
        Point start = getEdgeIntersection(lp.edge, rp.edge);
        if (start == null) return;

        // compute radius
        float distance = (b.point.position - start.position).magnitude;
        if (start.y + distance < ycurr) return; // must be after sweep line

        Point ep = new Point(start.x, start.y + distance);
        //Debug.Log("added circle voronoiEvent "+ ep);

        // add circle voronoiEvent
        Event e = new Event(ep, Event.CIRCLE_EVENT);
        e.arc = b;
        b.voronoiEvent = e;
        events.Add(e);
    }

    public int ccw(Point a, Point b, Point c)
    {
        float area2 = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        if (area2 < 0) return -1;
        else if (area2 > 0) return 1;
        else return 0;
    }

    // returns intersection of the lines of with vectors a and b
    private Point getEdgeIntersection(Edge a, Edge b)
    {

        if (b.slope == a.slope && b.yint != a.yint) return null;

        float x = (b.yint - a.yint) / (a.slope - b.slope);
        float y = a.slope * x + a.yint;

        return new Point(x, y);
    }

    // returns current x-coordinate of an unfinished edge
    private float getXofEdge(Parabola par)
    {
        //find intersection of two parabolas

        Parabola left = Parabola.getLeftChild(par);
        Parabola right = Parabola.getRightChild(par);

        Point p = left.point;
        Point r = right.point;

        float dp = 2 * (p.y - ycurr);
        float a1 = 1 / dp;
        float b1 = -2 * p.x / dp;
        float c1 = (p.x * p.x + p.y * p.y - ycurr * ycurr) / dp;

        float dp2 = 2 * (r.y - ycurr);
        float a2 = 1 / dp2;
        float b2 = -2 * r.x / dp2;
        float c2 = (r.x * r.x + r.y * r.y - ycurr * ycurr) / dp2;

        float a = a1 - a2;
        float b = b1 - b2;
        float c = c1 - c2;

        float disc = b * b - 4 * a * c;
        float x1 = (-b + Mathf.Sqrt(disc)) / (2 * a);
        float x2 = (-b - Mathf.Sqrt(disc)) / (2 * a);

        float ry;
        if (p.y > r.y) ry = Mathf.Max(x1, x2);
        else ry = Mathf.Min(x1, x2);

        return ry;
    }

    // returns parabola above this x coordinate in the beach line
    private Parabola getParabolaByX(float xx)
    {
        Parabola par = root;
        float x = 0;
        while (par.type == Parabola.IS_VERTEX)
        {
            x = getXofEdge(par);
            if (x > xx) par = par.child_left;
            else par = par.child_right;
        }
        return par;
    }

    // find corresponding y-coordinate to x on parabola with focus p
    private float getY(Point p, float x)
    {
        // determine equation for parabola around focus p
        float dp = 2 * (p.y - ycurr);
        float a1 = 1 / dp;
        float b1 = -2 * p.x / dp;
        float c1 = (p.x * p.x + p.y * p.y - ycurr * ycurr) / dp;
        return (a1 * x * x + b1 * x + c1);
    }
}
