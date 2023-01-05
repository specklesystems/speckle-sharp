using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Interop;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Objects.Converter.Navisworks
{
  public partial class ConverterNavisworks
  {
    internal static Base GetPropertiesBase(ModelItem element, ref Base @base)
    {
      Base propertiesBase = new Base();
      // GUI visible properties varies by a Global Options setting.
      PropertyCategoryCollection userVisiblePropertyCategories = element.GetUserFilteredPropertyCategories();

      foreach (PropertyCategory propertyCategory in userVisiblePropertyCategories)
      {
        DataPropertyCollection properties = propertyCategory.Properties;
        Base propertyCategoryBase = new Base();

        properties.ToList().ForEach(property =>
          BuildPropertyCategory(propertyCategory, property, ref propertyCategoryBase));

        if (propertyCategoryBase.GetMembers().Any() && propertyCategory.DisplayName != null)
        {
          string propertyCategoryDisplayName = SanitizePropertyName(propertyCategory.DisplayName);

          switch (propertyCategory.DisplayName)
          {
            case "Geometry":
              continue;
            case "Item":
            {
              foreach (string property in propertyCategoryBase.GetMembers().Keys)
              {
                @base[property] = propertyCategoryBase[property];
              }

              break;
            }
            default:
              propertiesBase[propertyCategoryDisplayName] = propertyCategoryBase;
              break;
          }
        }
      }

      return propertiesBase;
    }

    public static string SanitizePropertyName(string name)
    {
      // Regex pattern from speckle-sharp/Core/Core/Models/DynamicBase.cs IsPropNameValid
      return name == "Item"
        ? "$Item"
        : Regex.Replace(name, @"[\.\/]", "_");
    }

    public static void BuildPropertyCategory(PropertyCategory propertyCategory, DataProperty property,
      ref Base propertyCategoryBase)
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
          propertyValue = property.Value.ToPoint3D();
          break;
        case VariantDataType.None: break;
        case VariantDataType.Point2D:
          break;
      }

      if (propertyValue != null)
      {
        object keyPropValue = propertyCategoryBase[propertyName];

        if (keyPropValue == null)
        {
          propertyCategoryBase[propertyName] = propertyValue;
        }
        else if (keyPropValue is List<dynamic>)
        {
          List<dynamic> arrayPropValue = (List<dynamic>)keyPropValue;

          if (!arrayPropValue.Contains(propertyValue))
          {
            arrayPropValue.Add(propertyValue);
          }

          propertyCategoryBase[propertyName] = arrayPropValue;
        }
        else
        {
          dynamic existingValue = keyPropValue;

          if (existingValue != propertyValue)
          {
            List<dynamic> arrayPropValue = new List<dynamic>
            {
              existingValue,
              propertyValue
            };

            propertyCategoryBase[propertyName] = arrayPropValue;
          }
        }
      }
    }

    public static List<Tuple<NamedConstant, NamedConstant>> LoadQuickProperties()
    {
      List<Tuple<NamedConstant, NamedConstant>> quickPropertiesCategoryPropertyPairs =
        new List<Tuple<NamedConstant, NamedConstant>>();
      using (LcUOptionLock optionLock = new LcUOptionLock())
      {
        LcUOptionSet set = LcUOption.GetSet("interface.smart_tags.definitions", optionLock);
        int numOptions = set.GetNumOptions();
        if (numOptions > 0)
        {
          for (int index = 0; index < numOptions; ++index)
          {
            LcUOptionSet optionSet = set.GetValue(index, null);
            NamedConstant cat = optionSet.GetName("category").GetPtr();
            NamedConstant prop = optionSet.GetName("property").GetPtr();
            quickPropertiesCategoryPropertyPairs.Add(Tuple.Create(cat, prop));
          }
        }
      }

      return quickPropertiesCategoryPropertyPairs;
    }
  }
}