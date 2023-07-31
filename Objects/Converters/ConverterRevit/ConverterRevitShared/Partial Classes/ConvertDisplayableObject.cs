using System;
using System.Collections.Generic;
using System.Linq;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

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
    if (!obj.IsDisplayableObject())
      throw new Exception("The provided object is not displayable (has no 'displayValue' property).");

    // Extract info from the object dynamically.
    var name = obj["name"] as string ?? "speckleDisplayableObject" + obj.id;
    var displayValue = (obj["displayValue"] ?? obj["@displayValue"]) as List<object>;
    var casted = displayValue?.Cast<Base>().ToList();

    // TODO: Compute RevitCategory based on object type.
    var category = RevitCategory.GenericModel;

    // TODO: Figure out the logic needed for parameter transferring to a DS.
    List<Parameter> parameters = null;

    // Create a temp DirectShape and use the DirectShape conversion routine
    var ds = new DirectShape(name, category, casted, parameters);
    return DirectShapeToNative(ds, ToNativeMeshSettingEnum.Default);
  }
}
