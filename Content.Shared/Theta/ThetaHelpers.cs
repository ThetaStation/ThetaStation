using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared.Theta;

//todo: stuff here should be ported to RT someday
public static class ThetaHelpers
{
    #region Angles

    /// <summary>
    /// Returns value of angle in 0~2pi range
    /// </summary>
    public static Angle AngNormal(Angle x)
    {
        x = x.Reduced();
        if (x < 0)
            x += 2 * Math.PI;
        return x;
    }

    public static bool AngInSector(Angle x, Angle start, Angle width)
    {
        Angle dist = Angle.ShortestDistance(start, x);
        return Math.Sign(width) == Math.Sign(dist) ? Math.Abs(width) >= Math.Abs(dist) : Math.Abs(width) >= Math.Tau - Math.Abs(dist);
    }

    public static bool AngSectorsOverlap(Angle start0, Angle width0, Angle start1, Angle width1)
    {
        Angle end0 = AngNormal(start0 + width0);
        Angle end1 = AngNormal(start1 + width1);
        return AngInSector(start0, start1, width1) || AngInSector(start1, start0, width0) ||
               AngInSector(end0, start1, width1) || AngInSector(end1, start0, width0);
    }

    /// <summary>
    /// Sectors must have positive widths
    /// </summary>
    public static (Angle, Angle) AngCombinedSector(Angle start0, Angle width0, Angle start1, Angle width1)
    {
        Angle startlow, widthlow, starthigh, widthhigh, disthigh;
        (startlow, widthlow, starthigh, widthhigh) = AngInSector(start1, start0, width0) ? (start0, width0, start1, width1) : (start1, width1, start0, width0);
        disthigh = AngNormal(starthigh + widthhigh) > startlow ? starthigh + widthhigh - startlow : Math.Tau - startlow + AngNormal(starthigh + widthhigh);

        return (startlow, Math.Min(Math.Max(widthlow, disthigh), Math.Tau));
    }

    #endregion

    #region Geometry

    public static bool SegBoxIntersect(Vector2 a, Vector2 b, Box2 box, [NotNullWhen(true)] out Vector2? ipos)
    {
        ipos = null;
        return false;
    }

    #endregion

    #region Graphs

    /// <summary>
    /// Generic graph node
    /// </summary>
    public sealed class GraphNode<T>
    {
        public T Value;
        public List<(GraphNode<T>, float)> Neighbours = new();

        public GraphNode(T value)
        {
            Value = value;
        }

        public void AddNeighbour(GraphNode<T> node, float cost, bool twoway = true)
        {
            if (Neighbours.Contains((node, cost)))
                return;

            Neighbours.Add((node, cost));

            if (twoway)
                node.Neighbours.Add((this, cost));
        }

        public void RemoveNeighbour(GraphNode<T> node, bool twoway = true)
        {
            for (int i = 0; i < node.Neighbours.Count; i++)
            {
                GraphNode<T> neighbour = node.Neighbours[i].Item1;
                if (neighbour == node)
                {
                    if (twoway)
                        RemoveNeighbour(neighbour, false);
                    node.Neighbours.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>
    /// Finds the shortest path between start and finish nodes
    /// Graph nodes will be modified in the process so pass a copy if you want to save it
    /// </summary>
    public static List<GraphNode<T>> DjikstraFindPath<T>(GraphNode<T> start, GraphNode<T> finish)
    {
        Dictionary<GraphNode<T>, (GraphNode<T>, float)> tree = new() { { start, (start, 0) } };
        List<GraphNode<T>> frontNodes = new() { start };

        //building tree
        List<GraphNode<T>> buffer = new();
        while (frontNodes.Count > 0)
        {
            foreach (GraphNode<T> node in frontNodes)
            {
                float nodeCost = tree[node].Item2;
                foreach ((GraphNode<T> neighbour, float cost) in node.Neighbours)
                {
                    if (tree.TryGetValue(neighbour, out var pair))
                    {
                        if (cost + nodeCost < pair.Item2)
                        {
                            neighbour.RemoveNeighbour(pair.Item1); //remove parent connection
                            pair.Item1 = node;
                            pair.Item2 = cost + nodeCost;
                        }
                        else
                        {
                            neighbour.RemoveNeighbour(node);
                        }
                    }
                    else
                    {
                        tree[neighbour] = (node, cost + nodeCost);
                    }

                    buffer.Add(neighbour);
                }
            }

            frontNodes = buffer;
            buffer.Clear();
        }

        //then traversing it in reverse to get the resulting path
        List<GraphNode<T>> result = new();
        GraphNode<T> currentNode = finish;
        while (currentNode != start)
        {
            GraphNode<T> parent = tree[currentNode].Item1;
            result.Add(parent);
            currentNode = parent;
        }

        result.Add(start);
        result.Reverse();
        return result;
    }

    #endregion

    //they proved to be more useful than I've thought initially, so moved code for related operations from mapgen to here
    #region Rect ranges

    /// <summary>
    /// RectRange represents a rectangular part of space from Bottom to Top on Y axis
    /// Each xrange represents free/occupied ranges of space on X axis
    /// Group is used by CombineAllRanges to mark ranges which are in contact with each other
    /// </summary>
    public struct RectRange
    {
        public int Bottom, Top;
        public List<(int, int)> XRanges;
        public int Group;

        public RectRange(int bottom, int top, List<(int, int)> xRanges, int group = 0)
        {
            Bottom = bottom;
            Top = top;
            XRanges = xRanges;
            Group = group;
        }
    }

    /// <summary>
    /// Generic operation on xranges, like addition/subtraction/inversion/etc.
    /// </summary>
    public delegate List<(int, int)> XRangeOperation(List<(int, int)> ranges1, List<(int, int)> ranges2);

    /// <summary>
    /// Converts Box2i to RectRange
    /// </summary>
    public static RectRange RangeFromBox(Box2i box)
    {
        return new RectRange(box.Bottom, box.Top, new List<(int, int)> { (box.Left, box.Right) });
    }

    public static List<GraphNode<Vector2i>> RangeToNodes(RectRange range)
    {
        List<GraphNode<Vector2i>> result = new();

        int height = range.Top - range.Bottom;
        foreach ((int start, int end) in range.XRanges)
        {
            int width = end - start;

            GraphNode<Vector2i> lb = new(new(start, range.Bottom));
            GraphNode<Vector2i> lt = new(new(start, range.Top));
            GraphNode<Vector2i> rb = new(new(end, range.Bottom));
            GraphNode<Vector2i> rt = new(new(end, range.Top));

            lb.AddNeighbour(lt, height);
            lb.AddNeighbour(rb, width);
            lt.AddNeighbour(rt, width);
            rb.AddNeighbour(rt, height);

            result.AddRange([lb, lt, rb, rt]);
        }

        return result;
    }

    /// <summary>
    /// Combines geometry of all ranges in the list, each node representing a vertex of the resulting shape
    /// All input ranges should have only 1 xrange (since it's expected that they were created from boxes)
    /// </summary>
    public static List<GraphNode<Vector2i>> RangesToGraph(List<RectRange> ranges)
    {
        //combine overlapping ranges, basically splitting them into a bunch of horizontal layers
        ranges = CombineAllRanges(ranges, AddXRanges, out _);

        List<List<RectRange>> groups = new();
        Dictionary<RectRange, List<GraphNode<Vector2i>>> nodes = new();

        //separate em by groups (adjacent ranges)
        foreach (RectRange range in ranges)
        {
            groups[range.Group - 1].Add(range);
        }

        foreach (List<RectRange> group in groups)
        {
            //sort by height
            group.Sort((r1, r2) => r1.Bottom - r2.Bottom);

            for (int ilow = 0; ilow < group.Count; ilow++)
            {
                for (int ihigh = 0; ihigh < group.Count; ihigh++)
                {
                    if (ilow == ihigh)
                        continue;

                    RectRange lowRange = group[ilow];
                    RectRange highRange = group[ihigh];

                    //if ranges are actually adjacent combine them and convert the result to nodes
                    if (lowRange.Top == highRange.Bottom && XRangesOverlap(lowRange.XRanges, highRange.XRanges))
                    {
                        List<GraphNode<Vector2i>> allNodes;

                        if (nodes.TryGetValue(highRange, out var value))
                        {
                            allNodes = value;
                        }
                        else
                        {
                            allNodes = RangeToNodes(highRange);
                        }
                        allNodes.AddRange(RangeToNodes(lowRange));

                        //for combining them we get all the points lying on the border of the ranges (horizontal line)...
                        List<GraphNode<Vector2i>> borderNodes = new();
                        foreach (GraphNode<Vector2i> node in allNodes)
                        {
                            if (node.Value.Y == lowRange.Top)
                                borderNodes.Add(node);
                        }

                        //arrange them from left to right...
                        borderNodes.Sort((n1, n2) => n1.Value.X - n2.Value.X);
                        for (int m = 0; m < borderNodes.Count; m++)
                        {
                            GraphNode<Vector2i> node = borderNodes[m];

                            //delete their old horizontal connections...
                            for (int n = 0; n < node.Neighbours.Count; n++)
                            {
                                if (node.Neighbours[n].Item1.Value.Y == lowRange.Top)
                                {
                                    node.Neighbours.RemoveAt(n);
                                    n--;
                                }
                            }

                            /*
                            and finally create the connections, 
                            changing between connection and no connection for each pair of points, example:

                            .                           .
                            .                           .
                            |       Top range (A)       |
                            |                           |
                            A1___B1   B2___C1     C2___A2
                                 |  B  |   |   C   |
                                 |_____|   |       |
                                           |_______|
                            */
                            if (m % 2 == 0)
                                borderNodes[m + 1].AddNeighbour(node, (borderNodes[m + 1].Value - node.Value).Length);
                        }

                        nodes[highRange] = allNodes;
                        nodes[lowRange] = allNodes;
                    }
                }
            }
        }

        //finally combine nodes of each group into a single list
        List<GraphNode<Vector2i>> result = new();
        foreach ((RectRange _, List<GraphNode<Vector2i>> n) in nodes)
        {
            result.AddRange(n);
        }
        return result;
    }

    /// <summary>
    /// Tries to find a free spot with width no less than minWidth in given ranges 
    /// from start to end on X axis and from heightBottom to heightTop on Y
    /// Result is a rect range with a single x range
    /// </summary>
    public static RectRange VerticalRangeSearch(List<RectRange> ranges, int start, int end, int heightBottom, int heightTop, int minWidth)
    {
        int startn, endn, bottomn, topn;
        (startn, endn, bottomn, topn) = (start, end, heightBottom, heightTop);

        RectRange? GetNextFreeRange(bool above, int height)
        {
            List<RectRange> sranges = ranges.Where(x => above ? x.Bottom >= height : x.Top <= height).
                OrderBy(x => above ? x.Top : x.Bottom).ToList();
            if (!above)
                sranges.Reverse();

            if (sranges.Count == 0)
                return null;
            RectRange srange = sranges[0];

            if (Math.Abs((above ? srange.Bottom : srange.Top) - (above ? topn : bottomn)) > 1)
                return null;

            foreach ((int startf, int endf) in srange.XRanges)
            {
                int startnn = startf > startn ? startf : startn;
                int endnn = endf < endn ? endf : endn;
                if (endnn - startnn < minWidth)
                    continue;
                return new RectRange(srange.Bottom, srange.Top, new List<(int, int)> { (startnn, endnn) });
            }

            return null;
        }

        while (true)
        {
            RectRange? r = GetNextFreeRange(false, bottomn);
            if (r == null)
                break;
            bottomn = r.Value.Bottom;
            (startn, endn) = r.Value.XRanges[0];

        }

        while (true)
        {
            RectRange? r = GetNextFreeRange(true, topn);
            if (r == null)
                break;
            topn = r.Value.Top;
            (startn, endn) = r.Value.XRanges[0];
        }

        return new RectRange(bottomn, topn, new List<(int, int)> { (startn, endn) });
    }

    /// <summary>
    /// Applies operation to the xranges of range and all of the ranges vertically overlapping with it
    /// Also assigns range's group to all ranges in contact
    /// newRanges are ranges that we're created after executing this method,
    /// oldRanges are ranges that weren't modified in any way,
    /// returned ranges are new and old ranges combined
    /// </summary>
    public static List<RectRange> CombineRanges(
        List<RectRange> ranges,
        RectRange range,
        XRangeOperation operation,
        out List<RectRange> newRanges,
        out List<RectRange> oldRanges)
    {
        newRanges = new();
        oldRanges = new();

        foreach (RectRange rangeOther in ranges)
        {
            if (rangeOther.Top >= range.Bottom && rangeOther.Bottom <= range.Top) //overlap
            {
                int overlapBottom = rangeOther.Bottom;
                int overlapTop = rangeOther.Top;

                if (range.Bottom > rangeOther.Bottom)
                {
                    newRanges.Add(new RectRange(rangeOther.Bottom, range.Bottom, rangeOther.XRanges, range.Group));
                    overlapBottom = range.Bottom;
                }
                if (range.Top < rangeOther.Top)
                {
                    newRanges.Add(new RectRange(range.Top, rangeOther.Top, rangeOther.XRanges, range.Group));
                    overlapTop = range.Top;
                }

                newRanges.Add(new(overlapBottom, overlapTop, operation(rangeOther.XRanges, range.XRanges), range.Group));
            }
            else
            {
                oldRanges.Add(rangeOther);
            }
        }

        for (int i = 0; i < newRanges.Count; i++)
        {
            RectRange newRange = newRanges[i];
            if (newRange.Top - newRange.Bottom == 0 || newRange.XRanges.Count == 0)
                newRanges.RemoveAt(i);
        }

        oldRanges.AddRange(newRanges);
        return oldRanges;
    }

    /// <summary>
    /// Like CombineRanges but combines all of em at once
    /// Also assigns groups to ranges in contact with eachother
    /// </summary>
    public static List<RectRange> CombineAllRanges(List<RectRange> ranges, XRangeOperation operation, out int lastGroup)
    {
        lastGroup = 0;
        List<RectRange> currentRanges = new(ranges);

        for (int i = 0; i < ranges.Count; i++) //not foreach cause range is modified during iteration
        {
            RectRange range = ranges[i];
            if (!currentRanges.Contains(range)) //already got modified
                continue;

            range.Group = lastGroup++; //should propagate to all ranges in contact
            currentRanges = CombineRanges(currentRanges, range, operation, out var newRanges, out _);
            List<RectRange> buffer = new();
            while (newRanges.Count > 0)
            {
                foreach (RectRange newRange in newRanges)
                {
                    currentRanges = CombineRanges(currentRanges, newRange, operation, out var newNewRanges, out _);
                    buffer.AddRange(newNewRanges);
                }
                newRanges = buffer;
                buffer.Clear();
            }
        }

        return currentRanges;
    }

    /// <summary>
    /// Returns list of inverted ranges (analogous to logic not)
    /// </summary>
    public static List<(int, int)> InvertedXRanges(List<(int, int)> ranges)
    {
        List<(int, int)> results = new();
        foreach ((int start, int end) in ranges)
        {
            (int, int) nextRange = ranges.Where(r => r.Item1 > end).OrderBy(r => r.Item1).FirstOrDefault();
            if (nextRange == default)
                continue;

            results.Add((end, nextRange.Item1));
        }

        return results;
    }

    /// <summary>
    /// Adds ranges1 to ranges2 (analogous to logic or)
    /// </summary>
    public static List<(int, int)> AddXRanges(List<(int, int)> ranges1, List<(int, int)> ranges2)
    {
        List<(int, int)> AddOne(List<(int, int)> ranges, (int, int) range)
        {
            int startr = range.Item1;
            int endr = range.Item2;
            List<(int, int)> r = new();
            foreach ((int start, int end) in ranges)
            {
                if (start <= range.Item2 && end >= range.Item1) //overlap
                {
                    startr = start < startr ? start : startr;
                    endr = end > endr ? end : endr;
                }
                else
                {
                    r.Add((start, end));
                }
            }

            r.Add((startr, endr));
            return r;
        }

        List<(int, int)> newRanges = ranges1;
        foreach ((int, int) range2 in ranges2)
        {
            newRanges = AddOne(ranges1, range2);
        }

        return newRanges;
    }

    /// <summary>
    /// Subtracts ranges1 from ranges2 (analogous to logic xor)
    /// </summary>
    public static List<(int, int)> SubtractXRanges(List<(int, int)> ranges1, List<(int, int)> ranges2)
    {
        List<(int, int)> SubtractOne(List<(int, int)> ranges, (int, int) range)
        {
            List<(int, int)> r = new();
            foreach ((int start, int end) in ranges)
            {
                if (start <= range.Item2 && end >= range.Item1) //overlap
                {
                    if (end > range.Item1 && range.Item1 > start)
                        r.Add((start, range.Item1));
                    if (end > range.Item2 && range.Item2 > start)
                        r.Add((range.Item2, end));
                }
                else
                {
                    r.Add((start, end));
                }
            }

            return r;
        }

        List<(int, int)> newRanges = ranges1;
        foreach ((int, int) range2 in ranges2)
        {
            newRanges = SubtractOne(ranges1, range2);
        }

        return newRanges;
    }

    /// <summary>
    /// Returns true if atleast one pair of xranges overlap
    /// </summary>
    public static bool XRangesOverlap(List<(int, int)> ranges1, List<(int, int)> ranges2)
    {
        foreach ((int start1, int end1) in ranges1)
        {
            foreach ((int start2, int end2) in ranges2)
            {
                if (start1 <= end2 && end1 >= start2)
                    return true;
            }
        }

        return false;
    }

    #endregion

    #region Components

    //todo: this is a copypaste from AddComponentSpecial, all concerns from there apply here too
    public static void AddComponentsFromRegistry(EntityUid uid, ComponentRegistry registry)
    {
        var factory = IoCManager.Resolve<IComponentFactory>();
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var serializationManager = IoCManager.Resolve<ISerializationManager>();

        foreach (var (name, data) in registry)
        {
            var component = (Component) factory.GetComponent(name);
            component.Owner = uid;

            var temp = (object) component;
            serializationManager.CopyTo(data.Component, ref temp);
            entityManager.RemoveComponent(uid, temp!.GetType());
            entityManager.AddComponent(uid, (Component) temp);
        }
    }

    public static void RemoveComponentsFromRegistry(EntityUid uid, ComponentRegistry registry)
    {
        var factory = IoCManager.Resolve<IComponentFactory>();
        var entityManager = IoCManager.Resolve<IEntityManager>();

        foreach (var (name, _) in registry)
        {
            entityManager.RemoveComponent(uid, factory.GetRegistration(name).Type);
        }
    }

    #endregion
}