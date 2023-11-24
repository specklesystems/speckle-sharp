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
      var category = GetSpeckleObjectBuiltInCategory(obj);

      // Create a temp DirectShape and use the DirectShape conversion routine
      var ds = new DirectShape(name, category, displayValue.ToList(), parameters?.ToList())
      {
        applicationId = obj.applicationId
      };
      return DirectShapeToNative(ds, ToNativeMeshSettingEnum.Default);
    }
    else if (obj is Other.Instance instance)
    {
      // Extract info from the object dynamically.
      var name = obj.TryGetName() ?? "speckleDisplayableObject" + obj.id;
      var displayValue = instance.GetTransformedGeometry().Cast<Base>();

      var parameters = obj.TryGetParameters<Parameter>();
      var builtInCategory = GetSpeckleObjectBuiltInCategory(obj);

      // Create a temp DirectShape and use the DirectShape conversion routine
      var ds = new DirectShape(name, builtInCategory, displayValue.ToList(), parameters?.ToList());
      return DirectShapeToNative(ds, ToNativeMeshSettingEnum.Default);
    }
    else
    {
      throw new Exception("Object is not displayable (is not an instance, has no display value or it was null");
    }
  }

  public string GetSpeckleObjectBuiltInCategory(Base @object)
  {
    //from 2.16 onwards we're passing the BuiltInCategory on every object
    if (@object["builtInCategory"] is not null)
      return @object["builtInCategory"] as string;

    if (RevitCategory.TryParse(@object["category"] as string, out RevitCategory category))
    {
      return Categories.GetBuiltInFromSchemaBuilderCategory(category);
    }

    switch (@object)
    {
      case BE.Beam _:
      case BE.Brace _:
      case BE.TeklaStructures.TeklaContourPlate _:
        return BuiltInCategory.OST_StructuralFraming.ToString();
      case BE.TeklaStructures.Bolts _:
        return BuiltInCategory.OST_StructConnectionBolts.ToString();
      case BE.TeklaStructures.Welds _:
        return BuiltInCategory.OST_StructConnectionWelds.ToString();
      case BE.Floor _:
        return BuiltInCategory.OST_Floors.ToString();
      case BE.Ceiling _:
        return BuiltInCategory.OST_Ceilings.ToString();
      case BE.Column _:
        return BuiltInCategory.OST_Columns.ToString();
      case BE.Pipe _:
        return BuiltInCategory.OST_PipeSegments.ToString();
      case BE.Rebar _:
        return BuiltInCategory.OST_Rebar.ToString();
      case BE.Topography _:
        return BuiltInCategory.OST_Topography.ToString();
      case BE.Wall _:
        return BuiltInCategory.OST_Walls.ToString();
      case BE.Roof _:
        return BuiltInCategory.OST_Roofs.ToString();
      case BE.Duct _:
        return BuiltInCategory.OST_DuctSystem.ToString();
      case BE.CableTray _:
        return BuiltInCategory.OST_CableTray.ToString();
      default:
        return BuiltInCategory.OST_GenericModel.ToString();

    }
  }
}
