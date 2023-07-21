using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Navisworks.Api;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.Converter.Navisworks;

// ReSharper disable once UnusedType.Global
public partial class ConverterNavisworks
{
  private static Base GetPropertiesBase(ModelItem element)
  {
    Base propertiesBase = new() { ["name"] = "Properties" };

    PropertyCategoryCollection userVisiblePropertyCategories = element.GetUserFilteredPropertyCategories();

    foreach (PropertyCategory propertyCategory in userVisiblePropertyCategories)
      ProcessPropertyCategory(propertiesBase, propertyCategory);

    return propertiesBase;
  }

  private static void ProcessPropertyCategory(DynamicBase propertiesBase, PropertyCategory propertyCategory)
  {
    if (IsCategoryToBeSkipped(propertyCategory))
      return;

    DataPropertyCollection properties = propertyCategory.Properties;
    Base propertyCategoryBase = new();

    properties.ToList().ForEach(property => BuildPropertyCategory(propertyCategory, property, propertyCategoryBase));

    if (!propertyCategoryBase.GetMembers().Any() || propertyCategory.DisplayName == null)
      return;

    string propertyCategoryDisplayName = SanitizePropertyName(propertyCategory.DisplayName);
    string internalName = GetSanitizedPropertyName(propertyCategory.CombinedName.ToString()).Replace("LcOa", "");

    propertiesBase[UseInternalPropertyNames ? internalName : propertyCategoryDisplayName] = propertyCategoryBase;
  }

  private static bool IsCategoryToBeSkipped(PropertyCategory propertyCategory)
  {
    return propertyCategory.DisplayName == "Geometry";
  }

  private static string SanitizePropertyName(string name)
  {
    // Regex pattern from speckle-sharp/Core/Core/Models/DynamicBase.cs IsPropNameValid
    return name == "Item"
      // Item is a reserved term for Indexed Properties: https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/indexers/using-indexers
      ? "Item_"
      : Regex.Replace(name, @"[\.\/]", "_");
  }

  private static void BuildPropertyCategory(
    PropertyCategory propertyCategory,
    DataProperty property,
    Base propertyCategoryBase
  )
  {
    string propertyName = GetSanitizedPropertyName(property.DisplayName);
    string internalName = GetSanitizedPropertyName(property.CombinedName.BaseName).Replace("LcOa", "");

    if (propertyName == null)
      return;

    dynamic propertyValue = ConvertPropertyValue(property.Value);

    var useInternalNames = UseInternalPropertyNames;

    UpdatePropertyCategoryBase(propertyCategoryBase, useInternalNames ? internalName : propertyName, propertyValue);

    propertyCategoryBase.applicationId = propertyCategory.CombinedName.ToString();
  }

  private static string GetSanitizedPropertyName(string displayName)
  {
    try
    {
      return SanitizePropertyName(displayName);
    }
    catch (ArgumentException err)
    {
      ErrorLog($"Category Name not converted. {err.Message}");
      return null;
    }
  }

  private static dynamic ConvertPropertyValue(VariantData value)
  {
    dynamic propertyValue = null;

    VariantDataType type = value.DataType;

    switch (type)
    {
      case VariantDataType.Boolean:
        propertyValue = value.ToBoolean();
        break;
      case VariantDataType.DisplayString:
        propertyValue = value.ToDisplayString();
        break;
      case VariantDataType.IdentifierString:
        propertyValue = value.ToIdentifierString();
        break;
      case VariantDataType.Int32:
        propertyValue = value.ToInt32();
        break;
      case VariantDataType.Double:
        propertyValue = value.ToDouble();
        break;
      case VariantDataType.DoubleAngle:
        propertyValue = value.ToDoubleAngle();
        break;
      case VariantDataType.DoubleArea:
        propertyValue = value.ToDoubleArea();
        break;
      case VariantDataType.DoubleLength:
        propertyValue = value.ToDoubleLength();
        break;
      case VariantDataType.DoubleVolume:
        propertyValue = value.ToDoubleVolume();
        break;
      case VariantDataType.DateTime:
        propertyValue = value.ToDateTime().ToString(CultureInfo.InvariantCulture);
        break;
      case VariantDataType.NamedConstant:
        propertyValue = value.ToNamedConstant().DisplayName;
        break;
      case VariantDataType.Point3D:
        Point3D point = value.ToPoint3D();
        Point pointProperty = new(point.X, point.Y, point.Z);
        propertyValue = pointProperty.ToString();
        break;
      case VariantDataType.None:
        break;
      case VariantDataType.Point2D:
        break;
      default:
        propertyValue = value.ToString();
        break;
    }

    return propertyValue;
  }

  private static void UpdatePropertyCategoryBase(Base propertyCategoryBase, string propertyName, dynamic propertyValue)
  {
    if (propertyValue == null)
      return;

    object keyPropValue = propertyCategoryBase[propertyName];

    switch (keyPropValue)
    {
      case null:
        propertyCategoryBase[propertyName] = propertyValue;
        break;
      case List<dynamic> list:
      {
        List<dynamic> arrayPropValue = list;

        if (!arrayPropValue.Contains(propertyValue))
          arrayPropValue.Add(propertyValue);

        propertyCategoryBase[propertyName] = arrayPropValue;
        break;
      }
      default:
      {
        dynamic existingValue = keyPropValue;

        if (!existingValue.Equals(propertyValue))
        {
          List<dynamic> arrayPropValue = new() { existingValue, propertyValue };

          propertyCategoryBase[propertyName] = arrayPropValue;
        }

        break;
      }
    }
  }

  private static void AddItemProperties(ModelItem element, Base @base)
  {
    @base["class"] = element.ClassName;

    bool properties = ShouldIncludeProperties();

    // Cascade through the Property Sets
    @base["properties"] = properties ? GetPropertiesBase(element) : new Base();

    // If the node is a Model
    if (element.HasModel)
      ((Base)@base["properties"])["Model"] = GetModelProperties(element.Model);

    // Internal Properties
    AddInternalProperties(element, (Base)@base["properties"]);
  }

  private static bool ShouldIncludeProperties()
  {
    return !bool.TryParse(Settings.FirstOrDefault(x => x.Key == "include-properties").Value, out bool result) || result;
  }

  private static void AddInternalProperties(ModelItem element, Base propertiesBase)
  {
    Base internals = (Base)propertiesBase["Internal"] ?? new Base();

    internals["ClassDisplayName"] = element.ClassDisplayName ?? internals["ClassDisplayName"];
    internals["ClassName"] = element.ClassName ?? internals["ClassName"];
    internals["DisplayName"] = element.DisplayName ?? internals["DisplayName"];
    internals["InstanceGuid"] =
      element.InstanceGuid.ToByteArray().Select(x => (int)x).Sum() > 0 ? element.InstanceGuid : null;
    internals["Source"] = element.Model?.SourceFileName ?? internals["Source"];
    internals["Source Guid"] = element.Model?.SourceGuid ?? internals["Source Guid"];
    internals["NodeType"] = element.IsCollection
      ? "Collection"
      : element.IsComposite
        ? "Composite Object"
        : element.IsInsert
          ? "Geometry Insert"
          : element.IsLayer
            ? "Layer"
            : null;

    propertiesBase["Internal"] = internals;
  }

  private static Base GetModelProperties(Model elementModel)
  {
    Base model =
      new()
      {
        ["Creator"] = elementModel.Creator,
        ["Filename"] = elementModel.FileName,
        ["Source Filename"] = elementModel.SourceFileName,
        ["Units"] = elementModel.Units.ToString(),
        ["Transform"] = elementModel.Transform.ToString(),
        ["Guid"] = elementModel.Guid.ToString()
      };

    if (elementModel.HasFrontVector)
      model["Front Vector"] = elementModel.FrontVector.ToString();
    if (elementModel.HasNorthVector)
      model["North Vector"] = elementModel.NorthVector.ToString();
    if (elementModel.HasRightVector)
      model["Right Vector"] = elementModel.RightVector.ToString();
    if (elementModel.HasUpVector)
      model["Up Vector"] = elementModel.UpVector.ToString();

    return model;
  }
}
