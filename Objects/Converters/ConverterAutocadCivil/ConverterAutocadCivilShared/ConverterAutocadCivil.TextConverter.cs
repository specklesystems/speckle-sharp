using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Polyline = Objects.Geometry.Polyline;
using Curve = Objects.Geometry.Curve;

using CADSpline = Autodesk.AutoCAD.DatabaseServices.Spline;
using CADCurve = Autodesk.AutoCAD.DatabaseServices.Curve;
using CADLine = Autodesk.AutoCAD.DatabaseServices.Line;
using CADPolyline = Autodesk.AutoCAD.DatabaseServices.Polyline;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    public List<ICurve> TextCurvesToSpeckle(string text, string fontName, FontDescriptor fontDescriptor, double height, Point3d pointOriginal)
    {
      if (!GetGlyphTypeface(fontName, fontDescriptor, out GlyphTypeface glyphTypeface))
        return null;

      List<GlyphRun> listGlyphRun = CreateGlyphRunByLetter(text, glyphTypeface);
      if (listGlyphRun == null)
        return null;

      List<ICurve> listCurve = new List<ICurve>();

      foreach (GlyphRun glyphRun in listGlyphRun)//Text Scope
      {
        List<ICurve> listPolylineLetter = CreatePolylineListByLetter(glyphRun, height, pointOriginal);

        if (listPolylineLetter != null)
          listCurve.AddRange(listPolylineLetter);
      }

      return listCurve;
    }

    private bool GetGlyphTypeface(string fontName, FontDescriptor fontDescriptor, out GlyphTypeface glyphTypeface)
    {
      glyphTypeface = null;
      FontStyle fontStyle = FontStyles.Normal;
      FontWeight fontWeight = FontWeight.FromOpenTypeWeight(400); //Normal Regular

      if (fontDescriptor.Italic)
        fontStyle = FontStyles.Italic;
      else if (fontDescriptor.Bold)
        fontWeight = FontWeight.FromOpenTypeWeight(700); //Bold

      Typeface typeface = new Typeface(new System.Windows.Media.FontFamily(fontName), fontStyle, fontWeight, new System.Windows.FontStretch());
      try
      {
        if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
          return false;
      }
      catch
      {
        return false;
      }

      return true;
    }

    private float GetPixelsPerDip()
    {
      float dpi = 150;

      //Device-independent pixel for WPF
      float pixelsPerDip = dpi / 96f;

      return pixelsPerDip;
    }

    private List<GlyphRun> CreateGlyphRunByLetter(string text, GlyphTypeface glyphTypeface)
    {
      int count = text.Length;
      //float pixelsPerDip = GetPixelsPerDip();
      double renderingEmSize = 10;
      ushort[] glyphIndices = new ushort[count];
      double[] advanceWidths = new double[count];
      double totalWidth = 0;
      System.Windows.Point origin = new System.Windows.Point(0, 0);

      List<GlyphRun> listGlyphRun = new List<GlyphRun>();

      for (int i = 0; i < count; i++)
      {
        try
        {
          ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[text[i]];
          glyphIndices[i] = glyphIndex;
          double width = glyphTypeface.AdvanceWidths[glyphIndex] * renderingEmSize;
          advanceWidths[i] = width;
          totalWidth += width;

          GlyphRun glyphRun = new GlyphRun(glyphTypeface: glyphTypeface, bidiLevel: 0, isSideways: false, renderingEmSize: renderingEmSize,
           glyphIndices: glyphIndices, baselineOrigin: origin, advanceWidths: advanceWidths, glyphOffsets: null, characters: null,
           deviceFontName: null, clusterMap: null, caretStops: null, language: null);

          listGlyphRun.Add(glyphRun);
        }
        catch
        {
          return null;
        }
      }

      return listGlyphRun;
    }

    private List<ICurve> CreatePolylineListByLetter(GlyphRun glyphRun, double heightText, Point3d pointOriginal)
    {
      var geometry = glyphRun.BuildGeometry();
      var pathGeometry = geometry.GetOutlinedPathGeometry();
      var pathFigureList = pathGeometry.Figures;

      if (pathFigureList.Count < 1)
        return null; //Text space

      List<CADCurve> listCurveCAD = new List<CADCurve>();
      PathSegmentConverter pathSegmentConverter = new PathSegmentConverter();

      foreach (PathFigure pathFigure in pathFigureList) //Letter paths
      {
        listCurveCAD.AddRange(pathSegmentConverter.ConverterPathFigureLetterToCurveCAD(pathFigure, pathFigureList.Count));
      }

      Extents3d extentsText = new Extents3d();
      listCurveCAD.ForEach(x => extentsText.AddExtents(x.GeometricExtents));
      Double Factor = heightText / Point3d.Origin.GetVectorTo(extentsText.MaxPoint).Y;

      //Coordinate system adjustments
      foreach (var curve in listCurveCAD)
      {
        curve.TransformBy(Matrix3d.Scaling(Factor, Point3d.Origin));
       
        curve.TransformBy(Matrix3d.Displacement(new Point3d() - extentsText.MinPoint));

        curve.TransformBy(Matrix3d.Displacement(pointOriginal - new Point3d()));
      }

      //BlockTableRecord currSpace = Trans.GetObject(Doc.Database.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
    
      //foreach (var curve in listCurveCAD)
      //{
      //  curve.SetDatabaseDefaults();
      //  currSpace.AppendEntity(curve);
      //  Trans.AddNewlyCreatedDBObject(curve, true);
      //}

      //throw new Exception("Adicionar no banco para testar");

      List<ICurve> listCurveSpeckle = new List<ICurve>();
      foreach (var curve in listCurveCAD)
      {
        if (curve is CADSpline)
        {
          var curveSpeckle = SplineToSpeckle(curve as CADSpline);
          listCurveSpeckle.Add(curveSpeckle);
        }
        else if (curve is CADLine)
        {
          var curveSpeckle = LineToSpeckle(curve as CADLine);
          listCurveSpeckle.Add(curveSpeckle);
        }
        else if (curve is CADPolyline)
        {
          var curveSpeckle = PolylineToSpeckle(curve as CADPolyline);
          listCurveSpeckle.Add(curveSpeckle);
        }
        else
        {
          throw new NotImplementedException($"Conversion from {curve.GetType()} to Speckle not implemented");
        }
      }

      return listCurveSpeckle;
    }


  }
}
