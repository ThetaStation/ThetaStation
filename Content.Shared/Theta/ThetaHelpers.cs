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

    #region Graphs

    /// <summary>
    /// Generic graph node
    /// </summary>
    public sealed class GraphNode<T>
    {
        public required T Value;
        public List<GraphNode<T>> Neighbours = new();
        public List<int> Costs = new();
    }

    #endregion

    //they proved to be more useful than I've thought initially, so moved code for related operations from mapgen to here
    #region Rect ranges

    /// <summary>
    /// RectRange represents a rectangular part of space from Bottom to Top on Y axis
    /// Each xrange represents free/occupied ranges of space on X axis
    /// </summary>
    public struct RectRange
    {
        public int Bottom, Top;
        public List<(int, int)> XRanges;

        public RectRange(int bottom, int top, List<(int, int)> xRanges)
        {
            Bottom = bottom;
            Top = top;
            XRanges = xRanges;
        }
    }

    /// <summary>
    /// Generic operation on xranges, like addition/subtraction/inversion/etc.
    /// </summary>
    public delegate List<(int, int)> XRangeOperation(List<(int, int)> ranges1, List<(int, int)> ranges2);

    /// <summary>
    /// Converts Box2i to RectRange (area inside the box is considered free)
    /// </summary>
    public static RectRange RangeFromBox(Box2i box)
    {
        return new RectRange(box.Bottom, box.Top, new List<(int, int)> { (box.Left, box.Right) });
    }

    public static List<GraphNode<Vector2>> RangesToGraph(List<RectRange> ranges)
    {
    }

    /// <summary>
    /// Combines all ranges lying between end X & start X, and above/below height into a single range (with single X range)
    /// with width above minWidth and combined height of included ranges
    /// </summary>
    public static RectRange CombineRangesVertically(List<RectRange> ranges, int start, int end, int heightBottom, int heightTop, int minWidth)
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
    /// Returns true if atleast one x-range overlaps another
    /// </summary>
    public static bool XRangesOverlap(List<(int, int)> ranges1, List<(int, int)> ranges2)
    {
        foreach ((int start1, int end1) in ranges1)
        {
            foreach ((int start2, int end2) in ranges2)
            {
                if (start1 < end2 && end1 > start2)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Applies operation to the xranges of range and all of the ranges vertically overlapping with it
    /// </summary>
    public static List<RectRange> CombineRangesHorizontaly(List<RectRange> ranges, RectRange range, XRangeOperation operation)
    {
        List<RectRange> rangesNew = new();
        foreach (RectRange rangeOther in ranges)
        {
            if (rangeOther.Top >= range.Bottom && rangeOther.Bottom <= range.Top) //overlap
            {
                int overlapBottom = rangeOther.Bottom;
                int overlapTop = rangeOther.Top;

                if (range.Bottom > rangeOther.Bottom)
                {
                    rangesNew.Add(new RectRange(rangeOther.Bottom, range.Bottom, rangeOther.XRanges));
                    overlapBottom = range.Bottom;
                }
                if (range.Top < rangeOther.Top)
                {
                    rangesNew.Add(new RectRange(range.Top, rangeOther.Top, rangeOther.XRanges));
                    overlapTop = range.Top;
                }

                rangesNew.Add(new RectRange(overlapBottom, overlapTop, operation(rangeOther.XRanges, range.XRanges)));
            }
            else
            {
                rangesNew.Add(rangeOther);
            }
        }

        foreach (RectRange nrange in rangesNew)
        {
            if (nrange.Top - nrange.Bottom == 0 || nrange.XRanges.Count == 0)
                rangesNew.Remove(nrange);
        }

        return rangesNew;
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