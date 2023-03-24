using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Polyline = Objects.Geometry.Polyline;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    public List<Polyline> TextPolylineToSpeckle(string text, string fontName, FontDescriptor fontDescriptor, double height, Point3d pointOriginal)
    {
      if (!GetGlyphTypeface(fontName, fontDescriptor, out GlyphTypeface glyphTypeface))
        return null;

      List<GlyphRun> listGlyphRun = CreateGlyphRunByLetter(text, glyphTypeface);
      if (listGlyphRun == null)
        return null;

      List<Polyline> listPolyline = new List<Polyline>();

      foreach (GlyphRun glyphRun in listGlyphRun)//Text Scope
      {
        List<Polyline> listPolylineLetter = CreatePolylineListByLetter(glyphRun);

        if (listPolylineLetter != null)
          listPolyline.AddRange(listPolylineLetter);
      }

      return listPolyline;
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

      bool isValid = false;

      Typeface typeface = new Typeface(new System.Windows.Media.FontFamily(fontName), fontStyle, fontWeight, new System.Windows.FontStretch());
      try
      {
        if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
          isValid = false;
      }
      catch
      {
        isValid = false;
      }

      return isValid;
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

    private List<Polyline> CreatePolylineListByLetter(GlyphRun glyphRun)
    {
      var geometry = glyphRun.BuildGeometry();
      var pathGeometry = geometry.GetOutlinedPathGeometry();
      var pathFigureList = pathGeometry.Figures;

      if (pathFigureList.Count < 1)
        return null; //Text space

      List<Polyline> listPolyline = new List<Polyline>();

      foreach (System.Windows.Media.PathFigure pathFigure in pathFigureList)//LetterScope
      {
        System.Windows.Media.PathSegmentCollection pSegColl;
        pSegColl = pathFigure.Segments;
        int countSeg = pSegColl.Count;
        sp.WinStPt = pathFigure.StartPoint;

        foreach (System.Windows.Media.PathSegment ps in pSegColl)//LoopScope
        {
          if (pathFigureList.Count == 1 && countSeg == 1) sp.FigureClosed = pathFigure.IsClosed;
          sp.AddPathSegment(ps);
        }
        if (m_asRegion)//to subreact internal geometry out of Region letters
        {
          m_rde.ProcessObjectCollection(sp.DBObjColl);
          sp.Reset();
        }
      }

      return listPolyline;
    }

  }
}
