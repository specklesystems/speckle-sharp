﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rhino;
using Rhino.Geometry;

namespace Objects.Converter.RhinoGh
{
  /// <summary>
  /// Converts a "complex" <see cref="Brep"/> to be transferred to a <see cref="ARDB.Solid"/>.
  /// </summary>
  static class BrepEncoder
  {
    #region Encode
    public static Brep ToRawBrep(/*const*/ Brep brep, double scaleFactor)
    {
      brep = brep.DuplicateShallow() as Brep;
      return EncodeRaw(ref brep, scaleFactor) ? brep : default;
    }

    internal static bool EncodeRaw(ref Brep brep, double scaleFactor)
    {
      if (scaleFactor != 1.0 && !brep.Scale(scaleFactor))
        return default;

      var bbox = brep.GetBoundingBox(false);
      if (!bbox.IsValid || bbox.Diagonal.Length < RhinoDoc.ActiveDoc.ModelAngleToleranceRadians)
        return default;

      // Split and Shrink faces
      {
        brep.Faces.SplitKinkyFaces(RhinoDoc.ActiveDoc.ModelAngleToleranceRadians, true);
        brep.Faces.SplitClosedFaces(0);
        brep.Faces.ShrinkFaces();
      }

      var options = AuditBrep(brep);

      return RebuildBrep(ref brep, options);
    }

    [Flags]
    enum BrepIssues
    {
      Nothing                     = 0,
      OutOfToleranceEdges         = 1,
      OutOfToleranceSurfaceKnots  = 2,
    }

    static BrepIssues AuditBrep(Brep brep)
    {
      var options = default(BrepIssues);

      // Edges
      {
        foreach (var edge in brep.Edges)
        {
          if (edge.Tolerance > RhinoDoc.ActiveDoc.ModelRelativeTolerance)
          {
            options |= BrepIssues.OutOfToleranceEdges;
            //GeometryEncoder.Context.Peek.RuntimeMessage(10, $"Geometry contains out of tolerance edges, it will be rebuilt.", edge);
          }
        }
      }

      // Faces
      {
        foreach (var face in brep.Faces)
        {
          var deltaU = KnotListEncoder.MinDelta(face.GetSpanVector(0));
          if (deltaU < 1e-5)
          {
            options |= BrepIssues.OutOfToleranceSurfaceKnots;
            break;
          }

          var deltaV = KnotListEncoder.MinDelta(face.GetSpanVector(1));
          if (deltaV < 1e-5)
          {
            options |= BrepIssues.OutOfToleranceSurfaceKnots;
            break;
          }
        }
      }

      return options;
    }

    static bool RebuildBrep(ref Brep brep, BrepIssues options)
    {
      if(options != BrepIssues.Nothing)
      {
        var edgesToUnjoin = brep.Edges.Select(x => x.EdgeIndex);
        var shells = brep.UnjoinEdges(edgesToUnjoin);
        if (shells.Length == 0)
          shells = new Brep[] { brep };

        var kinkyEdges = 0;
        var microEdges = 0;
        var mergedEdges = 0;

        foreach (var shell in shells)
        {
          // Edges
          {
            var edges = shell.Edges;

            int edgeCount = edges.Count;
            for (int ei = 0; ei < edgeCount; ++ei)
              edges.SplitKinkyEdge(ei, RhinoDoc.ActiveDoc.ModelAngleToleranceRadians);

            kinkyEdges += edges.Count - edgeCount;
            microEdges += edges.RemoveNakedMicroEdges(RhinoDoc.ActiveDoc.ModelRelativeTolerance, cleanUp: true);
            mergedEdges += edges.MergeAllEdges(RhinoDoc.ActiveDoc.ModelAngleToleranceRadians) - edgeCount;
          }

          // Faces
          {
            foreach (var face in shell.Faces)
            {
              if(options.HasFlag(BrepIssues.OutOfToleranceSurfaceKnots))
              {
                face.GetSurfaceSize(out var width, out var height);

                face.SetDomain(0, new Interval(0.0, width));
                var deltaU = KnotListEncoder.MinDelta(face.GetSpanVector(0));
                if (deltaU < 1e-6)
                  face.SetDomain(0, new Interval(0.0, width * (1e-6 / deltaU)));

                face.SetDomain(1, new Interval(0.0, height));
                var deltaV = KnotListEncoder.MinDelta(face.GetSpanVector(1));
                if (deltaV < 1e-6)
                  face.SetDomain(1, new Interval(0.0, height * (1e-6 / deltaV)));
              }

              face.RebuildEdges(1e-6, false, true);
            }
          }

          // Flags
          shell.SetTrimIsoFlags();
        }
        
        var join = Brep.JoinBreps(shells, RhinoDoc.ActiveDoc.ModelRelativeTolerance);
        if (join.Length == 1) brep = join[0];
        else
        {
          var merge = new Brep();
          foreach (var shell in join)
            merge.Append(shell);

          brep = merge;
        }
      }

      return brep.IsValid;
    }
    #endregion
    
  }
}
