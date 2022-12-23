using Autodesk.Navisworks.Api;
using System;
using Color = System.Drawing.Color;

namespace Objects.Converter.Navisworks
{
  public partial class ConverterNavisworks
  {
    public static Color NavisworksColorToColor(Autodesk.Navisworks.Api.Color color)
    {
      return Color.FromArgb(
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

      Other.RenderMaterial r =
        new Other.RenderMaterial(1 - geom.Geometry.OriginalTransparency, 0, 1, renderColor, black)
        {
          name = materialName
        };

      return r;
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