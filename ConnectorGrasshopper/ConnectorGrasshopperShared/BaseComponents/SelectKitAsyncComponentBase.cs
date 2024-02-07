using System;
using System.Linq;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Speckle.Core.Kits;
using Speckle.Core.Logging;

namespace ConnectorGrasshopper.Objects;

public class SelectKitAsyncComponentBase : GH_SpeckleAsyncComponent
{
  public ISpeckleConverter Converter;

  public ISpeckleKit Kit;

  public string SelectedKitName;

  public SelectKitAsyncComponentBase(
    string name,
    string nickname,
    string description,
    string category,
    string subCategory
  )
    : base(name, nickname, description, category, subCategory) { }

  public virtual bool CanDisableConversion => true;

  public override Guid ComponentGuid => new("2FEE5354-0F5E-41D9-ACD3-BF376D29CCDC");

  public override void AddedToDocument(GH_Document document)
  {
    base.AddedToDocument(document);
    if (SelectedKitName == null)
    {
      SelectedKitName = SpeckleGHSettings.SelectedKitName;
    }

    SetConverter();
  }

  public virtual void SetConverter()
  {
    if (SelectedKitName == "None")
    {
      Kit = null;
      Converter = null;
      Message = "No Conversion";
      return;
    }
    try
    {
      SetConverterFromKit(SelectedKitName);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      AddRuntimeMessage(
        GH_RuntimeMessageLevel.Error,
        "No default kit found on this machine.\n"
          + "This can be caused by \n"
          + "- A corrupted install\n"
          + "- Another Grasshopper plugin using an older version of Speckle\n"
          + "- Having an older version of the Rhino connector installed\n\n"
          + "Try reinstalling both Rhino and Grasshopper connectors.\n\n"
          + "If the problem persists, please reach out to our Community Forum (https://speckle.community)"
      );
    }
  }

  public override bool Write(GH_IWriter writer)
  {
    writer.SetString("selectedKitName", SelectedKitName);
    return base.Write(writer);
  }

  public override bool Read(GH_IReader reader)
  {
    reader.TryGetString("selectedKitName", ref SelectedKitName);
    return base.Read(reader);
  }

  public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
  {
    try
    {
      var kits = KitManager.GetKitsWithConvertersForApp(Utilities.GetVersionedAppName());

      Menu_AppendSeparator(menu);
      Menu_AppendItem(menu, "Select the converter you want to use:", null, false);
      if (CanDisableConversion)
      {
        Menu_AppendItem(
          menu,
          "Do Not Convert",
          (s, e) =>
          {
            SelectedKitName = "None";
            SetConverter();
            ExpireSolution(true);
          },
          true,
          Kit == null
        );
      }

      foreach (var kit in kits)
      {
        Menu_AppendItem(
          menu,
          $"{kit.Name} ({kit.Description})",
          (s, e) =>
          {
            SelectedKitName = kit.Name;
            SetConverter();
            ExpireSolution(true);
          },
          true,
          kit.Name == Kit?.Name
        );
      }

      Menu_AppendSeparator(menu);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Error(ex, "An error occurred while fetching Kits");
      Menu_AppendItem(menu, "An error occurred while fetching Kits", null, false);
    }
  }

  public virtual void SetConverterFromKit(string kitName)
  {
    if (kitName == Kit?.Name)
    {
      return;
    }

    Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
    SelectedKitName = Kit.Name;
    Converter = Kit.LoadConverter(Utilities.GetVersionedAppName());
    Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);
    SpeckleGHSettings.OnMeshSettingsChanged += (sender, args) =>
      Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);
    Converter.SetContextDocument(Loader.GetCurrentDocument());
    Message = $"Using the {Kit.Name} Converter";
  }

  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    throw new SpeckleException("Please inherit from this class, don't use SelectKitComponentBase directly");
  }

  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    throw new SpeckleException("Please inherit from this class, don't use SelectKitComponentBase directly");
  }

  protected override void BeforeSolveInstance()
  {
    Converter?.SetContextDocument(Loader.GetCurrentDocument());
    base.BeforeSolveInstance();
  }

  protected override void SolveInstance(IGH_DataAccess DA)
  {
    //Ensure converter document is up to date
    if (Converter == null)
    {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "No converter was provided. Conversions are disabled.");
    }

    base.SolveInstance(DA);
  }
}
