using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Navisworks.Api;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.Converter.Navisworks;

public partial class ConverterNavisworks
{
  private static Base GetPropertiesBase(ModelItem element)
  {
    Base propertiesBase = new();
    // GUI visible properties varies by a Global Options setting.
    PropertyCategoryCollection userVisiblePropertyCategories = element.GetUserFilteredPropertyCategories();

    foreach (PropertyCategory propertyCategory in userVisiblePropertyCategories)
    {
      if (propertyCategory.DisplayName == "Geometry") continue;

      DataPropertyCollection properties = propertyCategory.Properties;
      Base propertyCategoryBase = new();

      properties.ToList()
        .ForEach(
          property =>
            BuildPropertyCategory(propertyCategory, property, propertyCategoryBase));

      if (!propertyCategoryBase.GetMembers().Any() || propertyCategory.DisplayName == null) continue;

      string propertyCategoryDisplayName = SanitizePropertyName(propertyCategory.DisplayName);

      propertiesBase[propertyCategoryDisplayName] = propertyCategoryBase;
    }

    return propertiesBase;
  }

  private static string SanitizePropertyName(string name)
  {
    // Regex pattern from speckle-sharp/Core/Core/Models/DynamicBase.cs IsPropNameValid
    return name == "Item"
      // Item is a reserved term for Indexed Properties: https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/indexers/using-indexers
      ? "Item_"
      : Regex.Replace(name, @"[\.\/]", "_");
  }

  private static void BuildPropertyCategory(PropertyCategory propertyCategory,
    DataProperty property,
    Base propertyCategoryBase)
  {
    string propertyName;

    try
    {
      propertyName = SanitizePropertyName(property.DisplayName);
    }
    catch (ArgumentException err)
    {
      ErrorLog($"Category Name not converted. {err.Message}");
      return;
    }

    dynamic propertyValue = null;

    VariantDataType type = property.Value.DataType;

    switch (type)
    {
      case VariantDataType.Boolean:
        propertyValue = property.Value.ToBoolean();
        break;
      case VariantDataType.DisplayString:
        propertyValue = property.Value.ToDisplayString();
        break;
      case VariantDataType.IdentifierString:
        propertyValue = property.Value.ToIdentifierString();
        break;
      case VariantDataType.Int32:
        propertyValue = property.Value.ToInt32();
        break;
      case VariantDataType.Double:
        propertyValue = property.Value.ToDouble();
        break;
      case VariantDataType.DoubleAngle:
        propertyValue = property.Value.ToDoubleAngle();
        break;
      case VariantDataType.DoubleArea:
        propertyValue = property.Value.ToDoubleArea();
        break;
      case VariantDataType.DoubleLength:
        propertyValue = property.Value.ToDoubleLength();
        break;
      case VariantDataType.DoubleVolume:
        propertyValue = property.Value.ToDoubleVolume();
        break;
      case VariantDataType.DateTime:
        propertyValue = property.Value.ToDateTime().ToString(CultureInfo.InvariantCulture);
        break;
      case VariantDataType.NamedConstant:
        propertyValue = property.Value.ToNamedConstant().DisplayName;
        break;
      case VariantDataType.Point3D:
        Point3D point = property.Value.ToPoint3D();
        Point pointProperty = new(point.X, point.Y, point.Z);
        propertyValue = pointProperty.ToString();
        break;
      case VariantDataType.None: break;
      case VariantDataType.Point2D:
        break;
      default:
        propertyValue = property.Value.ToString();
        break;
    }

    if (propertyValue != null)
    {
      object keyPropValue = propertyCategoryBase[propertyName];

      switch (keyPropValue)
      {
        case null:
          propertyCategoryBase[propertyName] = propertyValue;
          break;
        case List<dynamic> list:
        {
          List<dynamic> arrayPropValue = list;

          if (!arrayPropValue.Contains(propertyValue)) arrayPropValue.Add(propertyValue);

          propertyCategoryBase[propertyName] = arrayPropValue;
          break;
        }
        default:
        {
          dynamic existingValue = keyPropValue;

          if (!existingValue.Equals(propertyValue))
          {
            List<dynamic> arrayPropValue = new()
            {
              existingValue,
              propertyValue
            };

            propertyCategoryBase[propertyName] = arrayPropValue;
          }

          break;
        }
      }
    }

    propertyCategoryBase.applicationId = propertyCategory.CombinedName.ToString();
  }

  private static void AddItemProperties(ModelItem element, Base @base)
  {
    @base["class"] = element.ClassName;

    bool properties =
      !bool.TryParse(Settings.FirstOrDefault(x => x.Key == "include-properties").Value, out bool result) || result;

    // Cascade through the Property Sets
    @base["properties"] = properties
      ? GetPropertiesBase(element)
      : new Base();

    // If the node is a Model
    if (element.HasModel) ((Base)@base["properties"])["Model"] = GetModelProperties(element.Model);

    // Internal Properties - some are matched dynamically already, some can be added from the core API
    Base internals = (Base)((Base)@base["properties"])?["Internal"] ?? new Base();

    internals["ClassDisplayName"] = element.ClassDisplayName ?? internals["ClassDisplayName"];
    internals["ClassName"] = element.ClassName ?? internals["ClassName"];
    internals["DisplayName"] = element.DisplayName ?? internals["DisplayName"];
    internals["InstanceGuid"] = element.InstanceGuid.ToByteArray()
                                  .Select(x => (int)x)
                                  .Sum() > 0
      ? element.InstanceGuid
      : null;
    internals["Source"] = element.Model?.SourceFileName ?? internals["Source"];
    internals["Source Guid"] = element.Model?.SourceGuid ?? internals["Source Guid"];
    internals["NodeType"] = element.IsCollection ? "Collection" :
      element.IsComposite ? "Composite Object" :
      element.IsInsert ? "Geometry Insert" :
      element.IsLayer ? "Layer" : null;

    ((Base)@base["properties"])["Internal"] = internals;
  }

  private static Base GetModelProperties(Model elementModel)
  {
    Base model = new()
    {
      ["Creator"] = elementModel.Creator,
      ["Filename"] = elementModel.FileName,
      ["Source Filename"] = elementModel.SourceFileName,
      ["Units"] = elementModel.Units.ToString(),
      ["Transform"] = elementModel.Transform.ToString(),
      ["Guid"] = elementModel.Guid.ToString()
    };

    if (elementModel.HasFrontVector) model["Front Vector"] = elementModel.FrontVector.ToString();
    if (elementModel.HasNorthVector) model["North Vector"] = elementModel.NorthVector.ToString();
    if (elementModel.HasRightVector) model["Right Vector"] = elementModel.RightVector.ToString();
    if (elementModel.HasUpVector) model["Up Vector"] = elementModel.UpVector.ToString();

    return model;
  }
}
