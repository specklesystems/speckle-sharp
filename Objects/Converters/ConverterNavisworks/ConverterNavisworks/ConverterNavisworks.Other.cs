using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Interop;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Color = System.Drawing.Color;

namespace Objects.Converter.Navisworks
{
  public partial class ConverterNavisworks
  {
    public static Color NavisworksColorToColor(Autodesk.Navisworks.Api.Color color)
    {
      return System.Drawing.Color.FromArgb(
        Convert.ToInt32(color.R * 255),
        Convert.ToInt32(color.G * 255),
        Convert.ToInt32(color.B * 255));
    }


    public static Other.RenderMaterial TranslateMaterial(ModelItem geom)
    {
      var settings = new { Mode = "original" };

      Color renderColor;

      switch (settings.Mode)
      {
        case "original":
          renderColor = NavisworksColorToColor(geom.Geometry.OriginalColor);
          break;
        case "active":
          renderColor = NavisworksColorToColor(geom.Geometry.ActiveColor);
          break;
        case "permanent":
          renderColor = NavisworksColorToColor(geom.Geometry.PermanentColor);
          break;
        default:
          renderColor = new Color();
          break;
      }

      var materialName = $"NavisworksMaterial_{Math.Abs(renderColor.ToArgb())}";

      Color black = Color.FromArgb(Convert.ToInt32(0), Convert.ToInt32(0), Convert.ToInt32(0));

      PropertyCategory itemCategory = geom.PropertyCategories.FindCategoryByDisplayName("Item");
      if (itemCategory != null)
      {
        DataPropertyCollection itemProperties = itemCategory.Properties;
        DataProperty itemMaterial = itemProperties.FindPropertyByDisplayName("Material");
        if (itemMaterial != null && itemMaterial.DisplayName != "")
        {
          materialName = itemMaterial.Value.ToDisplayString();
        }
      }

      PropertyCategory materialPropertyCategory = geom.PropertyCategories.FindCategoryByDisplayName("Material");
      if (materialPropertyCategory != null)
      {
        DataPropertyCollection material = materialPropertyCategory.Properties;
        DataProperty name = material.FindPropertyByDisplayName("Name");
        if (name != null && name.DisplayName != "")
        {
          materialName = name.Value.ToDisplayString();
        }
      }

      Objects.Other.RenderMaterial r =
        new Objects.Other.RenderMaterial(1 - geom.Geometry.OriginalTransparency, 0, 1, renderColor, black)
        {
          name = materialName
        };

      return r;
    }

    public static string SanitizePropertyName(string name)
    {
      if (name == "Item")
      {
        return "$Item";
      }

      // Regex pattern from speckle-sharp/Core/Core/Models/DynamicBase.cs IsPropNameValid
      return Regex.Replace(name, @"[\.\/]", "_");
    }

    public static void BuildPropertyCategory(PropertyCategory propertyCategory, DataProperty property,
      ref Base propertyCategoryBase)
    {
      string categoryName;
      string propertyName;
      try
      {
        categoryName = SanitizePropertyName(propertyCategory.DisplayName);
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
          propertyValue = property.Value.ToDateTime().ToString();
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
        default:
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
      List<Tuple<NamedConstant, NamedConstant>> quickProperties_CategoryPropertyPairs =
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
            quickProperties_CategoryPropertyPairs.Add(Tuple.Create(cat, prop));
          }
        }
      }

      return quickProperties_CategoryPropertyPairs;
    }


    public static void ConsoleLog(string message, ConsoleColor color = ConsoleColor.Blue)
    {
      Console.WriteLine(message, color);
    }

    public static void WarnLog(string warningMessage)
    {
      ConsoleLog(warningMessage, ConsoleColor.DarkYellow);
    }

    public static void ErrorLog(Exception err)
    {
      ErrorLog(err.Message);
      throw err;
    }

    public static void ErrorLog(string errorMessage) => ConsoleLog(errorMessage, ConsoleColor.DarkRed);


    public static string GetUnits(Document doc)
    {
      return nameof(doc.Units).ToLower();
    }
  }
}