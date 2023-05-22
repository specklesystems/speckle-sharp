using System;
using Autodesk.Navisworks.Api;
using Objects.Other;
using Color = System.Drawing.Color;

namespace Objects.Converter.Navisworks;

public partial class ConverterNavisworks
{
  private static Color NavisworksColorToColor(Autodesk.Navisworks.Api.Color color)
  {
    return Color.FromArgb(
      Convert.ToInt32(color.R * 255),
      Convert.ToInt32(color.G * 255),
      Convert.ToInt32(color.B * 255)
    );
  }

  private static RenderMaterial TranslateMaterial(ModelItem geom)
  {
    // Already there anticipating other options becoming possible
    var materialSettings = new { Mode = "original" };

    Color renderColor;

    switch (materialSettings.Mode)
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

    var black = Color.FromArgb(Convert.ToInt32(0), Convert.ToInt32(0), Convert.ToInt32(0));

    var itemCategory = geom.PropertyCategories.FindCategoryByDisplayName("Item");
    if (itemCategory != null)
    {
      var itemProperties = itemCategory.Properties;
      var itemMaterial = itemProperties.FindPropertyByDisplayName("Material");
      if (itemMaterial != null && !string.IsNullOrEmpty(itemMaterial.DisplayName))
        materialName = itemMaterial.Value.ToDisplayString();
    }

    var materialPropertyCategory = geom.PropertyCategories.FindCategoryByDisplayName("Material");
    if (materialPropertyCategory != null)
    {
      var material = materialPropertyCategory.Properties;
      var name = material.FindPropertyByDisplayName("Name");
      if (name != null && !string.IsNullOrEmpty(name.DisplayName))
        materialName = name.Value.ToDisplayString();
    }

    var r = new RenderMaterial(1 - geom.Geometry.OriginalTransparency, 0, 1, renderColor, black)
    {
      name = materialName
    };

    return r;
  }

  private static void ConsoleLog(string message, ConsoleColor color = ConsoleColor.Blue)
  {
    Console.WriteLine(message, color);
  }

  private static void ErrorLog(string errorMessage)
  {
    ConsoleLog(errorMessage, ConsoleColor.DarkRed);
  }
}
