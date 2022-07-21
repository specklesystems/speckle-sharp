using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using Speckle.Core.Models;
using BE = Objects.BuiltElements;
using Objects.BuiltElements.TeklaStructures;
using System.Linq;
using Tekla.Structures.Model;
using Tekla.Structures.Solid;
using System.Collections;
using StructuralUtilities.PolygonMesher;

namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {
    public void ContourPlateToNative(BE.Area area)
    {
      if (!(area.outline is Polyline)) { }
      var ContourPlate = new ContourPlate();
      if (area is TeklaContourPlate)
      {
        //var countourPoints = GetContourPointsFromPolyLine((Polyline)area.outline);
        var contour = (TeklaContourPlate)area;

        ContourPlate.Profile.ProfileString = contour.profile.name;
        //ContourPlate.Contour.ContourPoints = countourPoints;
        ContourPlate.Material.MaterialString = contour.material.name;
        foreach (var cp in contour.contour)
        {
          ContourPlate.AddContourPoint(ToTeklaContourPoint(cp));
        }
        SetPartProperties(ContourPlate, contour);
      }
      ContourPlate.Insert();
      //Model.CommitChanges();

    }
    public TeklaContourPlate ContourPlateToSpeckle(Tekla.Structures.Model.ContourPlate plate)
    {
      var specklePlate = new TeklaContourPlate();
      specklePlate.name = plate.Name;
      specklePlate.profile = GetContourPlateProfile(plate.Profile.ProfileString);
      specklePlate.material = GetMaterial(plate.Material.MaterialString);
      specklePlate.finish = plate.Finish;
      specklePlate.classNumber = plate.Class;
      specklePlate.position = GetPositioning(plate.Position);

      //Polygon teklaPolygon = null;

      // Get general outline for other programs
      //plate.Contour.CalculatePolygon(out teklaPolygon);
      //if (teklaPolygon != null)
      //  specklePlate.outline = ToSpecklePolyline(teklaPolygon);

      // Getting polycurve now works with new nuget packages
      var teklaPolycurve = plate.GetContourPolycurve();
      specklePlate.outline = ToSpecklePolycurve(teklaPolycurve);

      // Get contour for ToNative Tekla conversion
      specklePlate.contour = new List<TeklaContourPoint>();
      var cPts = plate.Contour.ContourPoints.Cast<ContourPoint>();
      foreach (ContourPoint pt in cPts)
      {
        specklePlate.contour.Add(ToSpeckleContourPoint(pt));
      }

      GetAllUserProperties(specklePlate, plate);

      var solid = plate.GetSolid();
      specklePlate.displayValue = new List<Mesh>{ GetMeshFromSolid(solid)};
      var rebars = plate.GetReinforcements();
      if (rebars != null)
      {
        foreach (var rebar in rebars)
        {
          if (rebar is RebarGroup) { specklePlate.rebars = RebarGroupToSpeckle((RebarGroup)rebar); }

        }
      }
      return specklePlate;


    }
    /// <summary>
    /// Create a contour plate without a display mesh for boolean parts
    /// </summary>
    /// <param name="plate"></param>
    /// <returns></returns>
    public TeklaContourPlate AntiContourPlateToSpeckle(Tekla.Structures.Model.ContourPlate plate)
    {
      var specklePlate = new TeklaContourPlate();
      specklePlate.name = plate.Name;
      specklePlate.profile = GetContourPlateProfile(plate.Profile.ProfileString);
      specklePlate.material = GetMaterial(plate.Material.MaterialString);

      specklePlate.classNumber = plate.Class;
      specklePlate.position = GetPositioning(plate.Position);

      Polygon teklaPolygon = null;
      plate.Contour.CalculatePolygon(out teklaPolygon);
      if (teklaPolygon != null)
        specklePlate.outline = ToSpecklePolyline(teklaPolygon);

      // Get contour for ToNative Tekla conversion
      specklePlate.contour = new List<TeklaContourPoint>();
      var cPts = plate.Contour.ContourPoints.Cast<ContourPoint>();
      foreach (ContourPoint pt in cPts)
      {
        specklePlate.contour.Add(ToSpeckleContourPoint(pt));
      }

      var units = GetUnitsFromModel();
      specklePlate.applicationId = plate.Identifier.GUID.ToString();
      specklePlate["units"] = units;
      return specklePlate;
    }
    public void SetPartProperties(Part part, TeklaContourPlate teklaPlate)
    {
      part.Material.MaterialString = teklaPlate.material.name;
      part.Profile.ProfileString = teklaPlate.profile.name;
      part.Class = teklaPlate.classNumber;
      part.Finish = teklaPlate.finish;
      part.Name = teklaPlate.name;
      part.Position = SetPositioning(teklaPlate.position);
    }
  }

}
