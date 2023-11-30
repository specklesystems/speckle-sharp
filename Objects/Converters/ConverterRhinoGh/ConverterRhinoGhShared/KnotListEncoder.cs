using System;
using System.Collections.Generic;
using System.Diagnostics;
using Rhino.Geometry;
using Rhino.Geometry.Collections;

namespace Objects.Converter.RhinoGh;

internal static class KnotListEncoder
{
  public const double KnotTolerance = 1e-9;

  /// <summary>
  /// Compares a curve knot <paramref name="value"/> with a <paramref name="successor"/> value,
  /// in a monotonic nondecreasing sequence, for equality.
  /// </summary>
  /// <param name="value"></param>
  /// <param name="successor"></param>
  /// <param name="tolerance"></param>
  /// <param name="strict">true in case <paramref name="value"/> and <paramref name="successor"/> are strictly equal, else false is returned.</param>
  /// <returns>true if <paramref name="value"/> and <paramref name="successor"/> are equal within <paramref name="tolerance"/>.</returns>
  public static bool CurveKnotEqualTo(double value, double successor, double tolerance, out bool strict)
  {
    var distance = successor - value;
    if (distance <= tolerance)
    {
      strict = value == successor;
      return true;
    }

    strict = false;
    return distance <= Math.Max(Math.Abs(value), Math.Abs(successor)) * tolerance;
  }

  /// <summary>
  /// Compares a surface knot <paramref name="value"/> with a <paramref name="successor"/> value,
  /// in a monotonic nondecreasing sequence, for equality.
  /// </summary>
  /// <param name="value"></param>
  /// <param name="successor"></param>
  /// <param name="tolerance"></param>
  /// <param name="strict">true in case <paramref name="value"/> and <paramref name="successor"/> are strictly equal, else false is returned.</param>
  /// <returns>true if <paramref name="value"/> and <paramref name="successor"/> are equal within <paramref name="tolerance"/>.</returns>
  public static bool SurfaceKnotEqualTo(double value, double successor, double tolerance, out bool strict)
  {
    var distance = successor - value;
    if (distance <= tolerance)
    {
      strict = value == successor;
      return true;
    }

    strict = false;

    // DB.BRepBuilderSurfaceGeometry.CreateNURBSSurface do not check using relative tolerance
    // return distance <= Math.Max(Math.Abs(value), Math.Abs(successor)) * tolerance;
    return false;
  }

  /// <summary>
  /// Get knot multiplicity.
  /// </summary>
  /// <param name="knots"></param>
  /// <param name="index">Index of knot to query.</param>
  /// <param name="tolerance"></param>
  /// <param name="average"></param>
  /// <param name="strict"></param>
  /// <returns>The multiplicity (valence) of the knot.</returns>
  public static int KnotMultiplicity(
    NurbsCurveKnotList knots,
    int index,
    double tolerance,
    out double average,
    out bool strict
  )
  {
    var i = index;
    var value = knots[i++];
    average = value;

    strict = true;
    while (i < knots.Count && CurveKnotEqualTo(value, knots[i], tolerance, out var s))
    {
      strict &= s;
      average += knots[i];
      i++;
    }

    var multiplicity = i - index;

    if (strict)
    {
      average = knots[index];
    }
    else
    {
      average /= multiplicity;
    }

    return multiplicity;
  }

  /// <summary>
  /// Get knot multiplicity.
  /// </summary>
  /// <param name="knots"></param>
  /// <param name="index">Index of knot to query.</param>
  /// <param name="tolerance"></param>
  /// <param name="average"></param>
  /// <param name="strict"></param>
  /// <returns>The multiplicity (valence) of the knot.</returns>
  public static int KnotMultiplicity(
    NurbsSurfaceKnotList knots,
    int index,
    double tolerance,
    out double average,
    out bool strict
  )
  {
    var i = index;
    var value = knots[i++];
    average = value;

    strict = true;
    while (i < knots.Count && SurfaceKnotEqualTo(value, knots[i], tolerance, out var s))
    {
      strict &= s;
      average += knots[i];
      i++;
    }

    var multiplicity = i - index;

    if (strict)
    {
      average = knots[index];
    }
    else
    {
      average /= multiplicity;
    }

    return multiplicity;
  }

  /// <summary>
  /// Returns the minimum delta in a monotonic nondecreasin sequence of System.Double values.
  /// </summary>
  /// <param name="knots"></param>
  /// <returns>The minimum delta in the sequence.</returns>
  public static double MinDelta(IEnumerable<double> knots)
  {
    var delta = double.PositiveInfinity;

    using (var enumerator = knots.GetEnumerator())
    {
      if (enumerator.MoveNext())
      {
        var previous = enumerator.Current;

        while (enumerator.MoveNext())
        {
          var current = enumerator.Current;
          if (previous == current)
          {
            continue;
          }

          var d = current - previous;
          if (d < delta)
          {
            delta = d;
          }

          previous = current;
        }
      }
    }

    return delta;
  }

  /// <summary>
  /// Splits <paramref name="curve"/> as a <see cref="Rhino.Geometry.PolyCurve"/> where knot multiplicity is > degree - 2.
  /// </summary>
  /// <remarks>
  /// Collapses knots using <paramref name="knotTolerance"/>.
  /// </remarks>
  /// <param name="curve"></param>
  /// <param name="polyCurve"></param>
  /// <param name="knotTolerance"></param>
  /// <returns>false if no new polycurve is created.</returns>
  public static bool TryGetPolyCurveC2(
    this NurbsCurve curve,
    out PolyCurve polyCurve,
    double knotTolerance = KnotTolerance
  )
  {
    bool duplicate = false;
    var spans = default(List<double>);
    var degree = curve.Degree;
    var knots = curve.Knots;

    for (int k = degree; k < knots.Count - degree; )
    {
      var multiplicity = KnotMultiplicity(knots, k, knotTolerance, out var _, out var strict);

      if (multiplicity > degree - 2)
      {
        if (spans is null)
        {
          spans = new List<double>();
        }

        spans.Add(knots[k]);
      }

      if (!strict)
      {
        // We are going to modify curve so we need a duplicate here
        if (!duplicate)
        {
          curve = curve.Duplicate() as NurbsCurve;
          knots = curve.Knots;
          duplicate = true;
        }

        var excess = multiplicity - knots.KnotMultiplicity(k);

        // Correct multiplicity in case more than degree knots are snapped here
        multiplicity = Math.Min(multiplicity, degree);

        // Insert new knot multiplicity
        knots.InsertKnot(knots[k], multiplicity);

        // Remove old knots that do not overlap knots[k]
        if (excess > 0)
        {
          knots.RemoveKnots(k + multiplicity, k + multiplicity + excess);
        }
      }

      k += multiplicity;
    }

    if (spans is null)
    {
      polyCurve = default;
      return false;
    }

    polyCurve = new PolyCurve();
    foreach (var span in curve.Split(spans))
    {
      polyCurve.AppendSegment(span);
    }

    // Split may generate PolyCurves on seams.
    if (curve.IsClosed)
    {
      polyCurve.RemoveNesting();
    }

    return true;
  }

  private static bool ToKinkedSpans(
    NurbsSurfaceKnotList knots,
    int degree,
    out List<double> kinks,
    double knotTolerance = KnotTolerance
  )
  {
    kinks = default;

    for (int k = degree; k < knots.Count - degree; )
    {
      var multiplicity = KnotMultiplicity(knots, k, knotTolerance, out var _, out var strict);

      if (multiplicity > degree)
      {
        if (kinks is null)
        {
          kinks = new List<double>();
        }

        kinks.Add(knots[k]);
      }

      if (!strict)
      {
        var excess = multiplicity - knots.KnotMultiplicity(k);

        // Correct multiplicity in case more than degree knots are snapped here
        multiplicity = Math.Min(multiplicity, degree);

        // Insert new knot multiplicity
        knots.InsertKnot(knots[k], multiplicity);

        // Remove old knots that do not overlap knots[k]
        if (excess > 0)
        {
          knots.RemoveKnots(k + multiplicity, k + multiplicity + excess);
        }
      }

      k += multiplicity;
    }

    for (int k = degree; k < knots.Count - degree; )
    {
      var m = KnotMultiplicity(knots, k, knotTolerance, out var a, out var s);
      Debug.Assert(m <= degree);
      k += m;
    }

    return !(kinks is null);
  }
}
