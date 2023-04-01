using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Interop;
using Objects.Geometry;
using Speckle.Core.Models;

namespace Objects.Converter.Navisworks
{
  public partial class ConverterNavisworks
  {
    internal static Base GetPropertiesBase(ModelItem element, Base @base)
    {
      var propertiesBase = new Base();
      // GUI visible properties varies by a Global Options setting.
      var userVisiblePropertyCategories = element.GetUserFilteredPropertyCategories();

      foreach (var propertyCategory in userVisiblePropertyCategories)
      {
        if (propertyCategory.DisplayName == "Geometry") continue;

        var properties = propertyCategory.Properties;
        var propertyCategoryBase = new Base();

        properties.ToList().ForEach(property =>
          BuildPropertyCategory(propertyCategory, property, propertyCategoryBase));

        if (!propertyCategoryBase.GetMembers().Any() || propertyCategory.DisplayName == null) continue;

        var propertyCategoryDisplayName = SanitizePropertyName(propertyCategory.DisplayName);

        propertiesBase[propertyCategoryDisplayName] = propertyCategoryBase;
      }

      return propertiesBase;
    }

    public static string SanitizePropertyName(string name)
    {
      // Regex pattern from speckle-sharp/Core/Core/Models/DynamicBase.cs IsPropNameValid
      return name == "Item"
        // Item is a reserved term for Indexed Properties: https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/indexers/using-indexers
        ? "Item_" 
        : Regex.Replace(name, @"[\.\/]", "_");
    }

    public static void BuildPropertyCategory(PropertyCategory propertyCategory, DataProperty property,
      Base propertyCategoryBase)
    {
      string propertyName;
      try
      {
        SanitizePropertyName(propertyCategory.DisplayName);
      }
      catch (Exception err)
      {
        ErrorLog($"Category Name not converted. {err.Message}");
        return;
      }

      try
      {
        propertyName = SanitizePropertyName(property.DisplayName);
      }
      catch (Exception err)
      {
        ErrorLog($"Category Name not converted. {err.Message}");
        return;
      }

      dynamic propertyValue = null;

      var type = property.Value.DataType;

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
          var point = property.Value.ToPoint3D();
          var pointProperty = new Point(point.X, point.Y, point.Z);
          propertyValue = pointProperty.ToString();
          break;
        case VariantDataType.None: break;
        case VariantDataType.Point2D:
          break;
      }

      if (propertyValue != null)
      {
        var keyPropValue = propertyCategoryBase[propertyName];

        if (keyPropValue == null)
        {
          propertyCategoryBase[propertyName] = propertyValue;
        }
        else if (keyPropValue is List<dynamic>)
        {
          var arrayPropValue = (List<dynamic>)keyPropValue;

          if (!arrayPropValue.Contains(propertyValue)) arrayPropValue.Add(propertyValue);

          propertyCategoryBase[propertyName] = arrayPropValue;
        }
        else
        {
          dynamic existingValue = keyPropValue;

          if (existingValue != propertyValue)
          {
            var arrayPropValue = new List<dynamic> { existingValue, propertyValue };

            propertyCategoryBase[propertyName] = arrayPropValue;
          }
        }

        propertyCategoryBase.applicationId = propertyCategory.CombinedName.ToString();
      }
    }

    private static void AddItemProperties(ModelItem element, Base @base)
    {
      @base["class"] = element.ClassName;

      var properties =
        !bool.TryParse(Settings.FirstOrDefault(x => x.Key == "include-properties").Value, out var result) || result;

      // Cascade through the Property Sets
      @base["properties"] = properties
        ? GetPropertiesBase(element, @base)
        : new Base();

      // If the node is a Model
      if (element.HasModel) ((Base)@base["properties"])["Model"] = GetModelProperties(element.Model);

      // Internal Properties - some are matched dynamically already, some can be added from the core API
      var internals = (Base)((Base)@base["properties"])?["Internal"] ?? new Base();

      internals["ClassDisplayName"] = element.ClassDisplayName ?? internals["ClassDisplayName"];
      internals["ClassName"] = element.ClassName ?? internals["ClassName"];
      internals["DisplayName"] = element.DisplayName ?? internals["DisplayName"];
      internals["InstanceGuid"] = element.InstanceGuid.ToByteArray()
        .Select(x => (int)x).Sum() > 0
        ? element.InstanceGuid
        : (Guid?)null;
      internals["Source"] = element.Model?.SourceFileName ?? internals["Source"];
      internals["Source Guid"] = element.Model?.SourceGuid ?? internals["Source Guid"];
      internals["NodeType"] = element.IsCollection ? "Collection" :
        element.IsComposite ? "Composite Object" :
        element.IsInsert ? "Geometry Insert" :
        element.IsLayer ? "Layer" : null;

      ((Base)@base["properties"])["Internal"] = internals;
    }

    private static Base GetModelProperties(Autodesk.Navisworks.Api.Model elementModel)
    {
      var model = new Base
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

    public static List<Tuple<NamedConstant, NamedConstant>> LoadQuickProperties()
    {
      var quickPropertiesCategoryPropertyPairs =
        new List<Tuple<NamedConstant, NamedConstant>>();
      using (var optionLock = new LcUOptionLock())
      {
        var set = LcUOption.GetSet("interface.smart_tags.definitions", optionLock);
        var numOptions = set.GetNumOptions();
        if (numOptions > 0)
          for (var index = 0; index < numOptions; ++index)
          {
            var optionSet = set.GetValue(index, null);
            var cat = optionSet.GetName("category").GetPtr();
            var prop = optionSet.GetName("property").GetPtr();
            quickPropertiesCategoryPropertyPairs.Add(Tuple.Create(cat, prop));
          }
      }

      return quickPropertiesCategoryPropertyPairs;
    }
  }
}
