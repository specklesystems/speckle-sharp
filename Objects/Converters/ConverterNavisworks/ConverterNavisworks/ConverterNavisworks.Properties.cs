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
  internal static Base GetPropertiesBase(ModelItem element, Base @base)
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
}
