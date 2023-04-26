using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DesktopUI2.ViewModels.MappingTool;

namespace DesktopUI2;

public class DummyMappingsBindings : MappingsBindings
{
  public override void SetMappings(string schema, string viewModel) { }

  public override MappingSelectionInfo GetSelectionInfo()
  {
    var type = typeof(Schema);
    var schemas = Assembly
      .GetExecutingAssembly()
      .GetTypes()
      .Where(p => type.IsAssignableFrom(p) && type != p)
      .Select(x => (Schema)Activator.CreateInstance(x))
      .ToList();

    return new MappingSelectionInfo(schemas, 13);
  }

  public override List<Schema> GetExistingSchemaElements()
  {
    return new List<Schema>
    {
      new RevitWallViewModel
      {
        SelectedFamily = new RevitFamily { Name = "Yolo Family" },
        SelectedType = "Simplex Ultra 500x500"
      },
      new RevitWallViewModel
      {
        SelectedFamily = new RevitFamily { Name = "Basic Wall" },
        SelectedType = "Partitioning Extra Fine 2mm"
      },
      new DirectShapeFreeformViewModel { ShapeName = "Curvy BREP", SelectedCategory = "GenericModels" }
    };
  }

  public override void HighlightElements(List<string> ids) { }

  public override void SelectElements(List<string> ids) { }

  public override void ClearMappings(List<string> ids) { }
}
