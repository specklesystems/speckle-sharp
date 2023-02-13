using System;
using System.Collections.Generic;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace ConnectorGrasshopper.Objects
{
  public class SpeckleObjectGroup : GH_Goo<List<object>>
  {
    public SpeckleObjectGroup(List<object> values)
    {
      m_value = values;
    }
    public override IGH_Goo Duplicate()
    {
      return new SpeckleObjectGroup(Value);
    }

    public override string ToString() => $"Speckle Group ({Value.Count} objects)";

    public override bool IsValid => Value?.Count != 0;
    public override string TypeName => "SpeckleGroup";

    public override string TypeDescription =>
      "Represents a list of speckle objects. This is only intended to be used with our 'CSO K/V' and 'ESO K/V' components";
  }
  
  public class SpeckleObjectGroupComponent : SelectKitComponentBase
  {
    public SpeckleObjectGroupComponent() : base("Speckle Group", "SG",
      "Represents a list of objects to be used with the CSO K/V and ESO K/V components",
      ComponentCategories.PRIMARY_RIBBON, ComponentCategories.OBJECTS)
    {
    }

    public override Guid ComponentGuid => new Guid("C9800EFB-F29E-4985-A13D-9C6F4CECDEDC");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Objects", "O", "The objects to be grouped", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Group", "G", "The group containing the input objects", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      var values = new List<object>();
      if (!DA.GetDataList(0, values)) return;

      var group = new SpeckleObjectGroup(values);

      DA.SetData(0, group);
    }
  }
}
