using System;
using System.Linq;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Speckle.Core.Kits;

namespace ConnectorGrasshopper.Objects;

public abstract class SelectKitComponentBase : GH_SpeckleComponent
{
  protected ISpeckleConverter Converter;

  protected ISpeckleKit Kit;

  protected SelectKitComponentBase(
    string name,
    string nickname,
    string description,
    string category,
    string subCategory
  )
    : base(name, nickname, description, category, subCategory) { }

  public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
  {
    try
    {
      var kits = KitManager.GetKitsWithConvertersForApp(Utilities.GetVersionedAppName());

      Menu_AppendSeparator(menu);
      Menu_AppendItem(menu, "Select the converter you want to use:", null, false);
      foreach (var kit in kits)
        Menu_AppendItem(
          menu,
          $"{kit.Name} ({kit.Description})",
          (s, e) =>
          {
            SetConverterFromKit(kit.Name);
          },
          true,
          kit.Name == Kit.Name
        );

      Menu_AppendSeparator(menu);
    }
    catch (Exception e)
    {
      // Todo: handle this
      Console.WriteLine(e);
    }
  }

  public void SetConverterFromKit(string kitName)
  {
    if (kitName == Kit.Name)
      return;

    try
    {
      Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
      Converter = Kit.LoadConverter(Utilities.GetVersionedAppName());
      Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);
      SpeckleGHSettings.OnMeshSettingsChanged += (sender, args) =>
        Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);
      Converter.SetContextDocument(Loader.GetCurrentDocument());
      Message = $"Using the {Kit.Name} Converter";
      ExpireSolution(true);
    }
    catch (Exception e)
    {
      // TODO: handle this.
      Console.WriteLine(e);
    }
  }

  public override void AddedToDocument(GH_Document document)
  {
    base.AddedToDocument(document);

    try
    {
      Kit = KitManager
        .GetKitsWithConvertersForApp(Utilities.GetVersionedAppName())
        .FirstOrDefault(kit => kit.Name == SpeckleGHSettings.SelectedKitName);
      Converter = Kit.LoadConverter(Utilities.GetVersionedAppName());
      Converter.SetContextDocument(Loader.GetCurrentDocument());
      Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);
      SpeckleGHSettings.OnMeshSettingsChanged += (sender, args) =>
        Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);
      Message = $"{Kit.Name} Kit";
    }
    catch
    {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
    }
  }
}
