using System;
using System.Diagnostics.CodeAnalysis;
using Autodesk.Navisworks.Api;
using Objects.Other;
using Color = System.Drawing.Color;

namespace Objects.Converter.Navisworks;

// ReSharper disable once UnusedType.Global
public partial class ConverterNavisworks
{
  private static Color NavisworksColorToColor(Autodesk.Navisworks.Api.Color color) =>
    Color.FromArgb(Convert.ToInt32(color.R * 255), Convert.ToInt32(color.G * 255), Convert.ToInt32(color.B * 255));

  [SuppressMessage(
    "design",
    "CA1508:Avoid dead conditional code",
    Justification = "Already there anticipating other options becoming possible"
  )]
  private static RenderMaterial TranslateMaterial(ModelItem geom)
  {
    // Already there anticipating other options becoming possible
    var materialSettings = new { Mode = "original" };
    var renderColor = materialSettings.Mode switch
    {
      "active" => NavisworksColorToColor(geom.Geometry.ActiveColor),
      "permanent" => NavisworksColorToColor(geom.Geometry.PermanentColor),
      "original" => NavisworksColorToColor(geom.Geometry.OriginalColor),
      _ => new Color(),
    };
    var materialName = $"NavisworksMaterial_{Math.Abs(renderColor.ToArgb())}";

    var black = Color.FromArgb(Convert.ToInt32(0), Convert.ToInt32(0), Convert.ToInt32(0));

    var itemCategory = geom.PropertyCategories.FindCategoryByDisplayName("Item");
    if (itemCategory != null)
    {
      var itemProperties = itemCategory.Properties;
      var itemMaterial = itemProperties.FindPropertyByDisplayName("Material");
      if (itemMaterial != null && !string.IsNullOrEmpty(itemMaterial.DisplayName))
      {
        materialName = itemMaterial.Value.ToDisplayString();
      }
    }

    var materialPropertyCategory = geom.PropertyCategories.FindCategoryByDisplayName("Material");
    if (materialPropertyCategory != null)
    {
      var material = materialPropertyCategory.Properties;
      var name = material.FindPropertyByDisplayName("Name");
      if (name != null && !string.IsNullOrEmpty(name.DisplayName))
      {
        materialName = name.Value.ToDisplayString();
      }
    }

    var r = new RenderMaterial(1 - geom.Geometry.OriginalTransparency, 0, 1, renderColor, black)
    {
      name = materialName
    };

    return r;
  }

  private static void ConsoleLog(string message, ConsoleColor color = ConsoleColor.Blue) =>
    Console.WriteLine(message, color);

  private static void ErrorLog(string errorMessage) => ConsoleLog(errorMessage, ConsoleColor.DarkRed);
}
