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
using TSG = Tekla.Structures.Geometry3d;
using System.Collections;
using StructuralUtilities.PolygonMesher;
using Objects.Structural.Materials;

namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {
    public TeklaRebar RebarGroupToSpeckle(RebarGroup rebarGroup)
    {

      var Rebar = new TeklaRebar();
      Rebar.displayValue = new List<Mesh> { GetMeshFromSolid(rebarGroup.GetSolid()) };
      foreach (Polygon polygon in rebarGroup.Polygons){
        var polyline = ToSpecklePolyline(polygon);
        Rebar.curves.Add(polyline);
      }
      Rebar.units = GetUnitsFromModel();
      Rebar.name = rebarGroup.Name;
      Rebar.material = new Structural.Materials.StructuralMaterial();
      Rebar.material.name = rebarGroup.Grade;
      Rebar.material.grade = rebarGroup.Grade;
      Rebar.size = rebarGroup.Size;
      Rebar.classNumber = rebarGroup.Class;
      Rebar.startHook = new Hook();
      Rebar.startHook.angle = rebarGroup.StartHook.Angle;
      Rebar.startHook.length = rebarGroup.StartHook.Length;
      Rebar.startHook.radius = rebarGroup.StartHook.Radius;
      switch (rebarGroup.StartHook.Shape){
        case RebarHookData.RebarHookShapeEnum.NO_HOOK:
          Rebar.startHook.shape = shape.NO_HOOK;
          break;
        case RebarHookData.RebarHookShapeEnum.HOOK_90_DEGREES:
          Rebar.startHook.shape = shape.HOOK_90_DEGREES;
          break;
        case RebarHookData.RebarHookShapeEnum.HOOK_135_DEGREES:
          Rebar.startHook.shape = shape.HOOK_135_DEGREES;
          break;
        case RebarHookData.RebarHookShapeEnum.HOOK_180_DEGREES:
          Rebar.startHook.shape = shape.HOOK_180_DEGREES;
          break;
        case RebarHookData.RebarHookShapeEnum.CUSTOM_HOOK:
          Rebar.startHook.shape = shape.CUSTOM_HOOK;
          break;
      }
      Rebar.endHook = new Hook();
      Rebar.endHook.angle = rebarGroup.EndHook.Angle;
      Rebar.endHook.length = rebarGroup.EndHook.Length;
      Rebar.endHook.radius = rebarGroup.EndHook.Radius;
      switch (rebarGroup.EndHook.Shape)
      {
        case RebarHookData.RebarHookShapeEnum.NO_HOOK:
          Rebar.endHook.shape = shape.NO_HOOK;
          break;
        case RebarHookData.RebarHookShapeEnum.HOOK_90_DEGREES:
          Rebar.endHook.shape = shape.HOOK_90_DEGREES;
          break;
        case RebarHookData.RebarHookShapeEnum.HOOK_135_DEGREES:
          Rebar.endHook.shape = shape.HOOK_135_DEGREES;
          break;
        case RebarHookData.RebarHookShapeEnum.HOOK_180_DEGREES:
          Rebar.endHook.shape = shape.HOOK_180_DEGREES;
          break;
        case RebarHookData.RebarHookShapeEnum.CUSTOM_HOOK:
          Rebar.endHook.shape = shape.CUSTOM_HOOK;
          break;
      }
      return Rebar;
    }

  }


}
