using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using BE = Objects.BuiltElements;
using DirectShape = Objects.BuiltElements.Revit.DirectShape;
using Parameter = Objects.BuiltElements.Revit.Parameter;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  /// <summary>
  /// Converts a displayable object into a DirectShape.
  /// </summary>
  /// <param name="obj">The "displayable object". This can be any object that has a `displayValue` property, and an optional `name` property.</param>
  /// <returns>The application object containing the conversion result and info.</returns>
  public ApplicationObject DisplayableObjectToNative(Base obj)
  {
    if (obj.IsDisplayableObject())
    {
      // Extract info from the object dynamically.
      var name = obj.TryGetName() ?? "speckleDisplayableObject" + obj.id;
      var displayValue = obj.TryGetDisplayValue() ?? throw new Exception("Display value was empty or null");

      var parameters = obj.TryGetParameters<Parameter>();
      var category = GetSpeckleObjectCategory(obj);

      // Create a temp DirectShape and use the DirectShape conversion routine
      var ds = new DirectShape(name, category, displayValue.ToList(), parameters?.ToList());
      return DirectShapeToNative(ds, ToNativeMeshSettingEnum.Default);
    }
    else if (obj is Other.Instance instance)
    {
      // Extract info from the object dynamically.
      var name = obj.TryGetName() ?? "speckleDisplayableObject" + obj.id;
      var displayValue = instance.GetTransformedGeometry().Cast<Base>();

      var parameters = obj.TryGetParameters<Parameter>();
      var category = GetSpeckleObjectCategory(obj);

      // Create a temp DirectShape and use the DirectShape conversion routine
      var ds = new DirectShape(name, category, displayValue.ToList(), parameters?.ToList());
      return DirectShapeToNative(ds, ToNativeMeshSettingEnum.Default);
    }
    else
    {
      throw new Exception("Object is not displayable (is not an instance, has no display value or it was null");
    }
  }

  public RevitCategory GetSpeckleObjectCategory(Base @object)
  {
    switch (@object)
    {
      case BE.Beam _:
      case BE.Brace _:
      case BE.TeklaStructures.TeklaContourPlate _:
        return RevitCategory.StructuralFraming;
      case BE.TeklaStructures.Bolts _:
        return RevitCategory.StructConnectionBolts;
      case BE.TeklaStructures.Welds _:
        return RevitCategory.StructConnectionWelds;
      case BE.Floor _:
        return RevitCategory.Floors;
      case BE.Ceiling _:
        return RevitCategory.Ceilings;
      case BE.Column _:
        return RevitCategory.Columns;
      case BE.Pipe _:
        return RevitCategory.PipeSegments;
      case BE.Rebar _:
        return RevitCategory.Rebar;
      case BE.Topography _:
        return RevitCategory.Topography;
      case BE.Wall _:
        return RevitCategory.Walls;
      case BE.Roof _:
        return RevitCategory.Roofs;
      case BE.Duct _:
        return RevitCategory.DuctSystem;
      case BE.CableTray _:
        return RevitCategory.CableTray;
      default:
        return RevitCategory.GenericModel;
    }
  }
}
