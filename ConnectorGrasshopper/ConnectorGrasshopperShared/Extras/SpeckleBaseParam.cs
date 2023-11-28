using System;
using System.Drawing;
using ConnectorGrasshopper.Properties;
using Grasshopper.Kernel;

namespace ConnectorGrasshopper.Extras;

public class SpeckleBaseParam : GH_Param<GH_SpeckleBase>
{
  public bool IsSchemaBuilderOutput;

  public bool UseSchemaTag;

  public SpeckleBaseParam(
    string name,
    string nickname,
    string description,
    GH_ParamAccess access,
    bool isSchemaBuilderOutput = false
  )
    : this(name, nickname, description, "Params", "Primitive", access)
  {
    IsSchemaBuilderOutput = isSchemaBuilderOutput;
  }

  public SpeckleBaseParam(
    string name,
    string nickname,
    string description,
    string category,
    string subcategory,
    GH_ParamAccess access
  )
    : base(name, nickname, description, category, subcategory, access) { }

  public SpeckleBaseParam()
    : this("Speckle Object", "SO", "Base object for Speckle", GH_ParamAccess.item) { }

  public override Guid ComponentGuid => new("55F13720-45C1-4B43-892A-25AE4D95EFF2");
  protected override Bitmap Icon => Resources.BaseParam;
  public override GH_Exposure Exposure => GH_Exposure.tertiary;

  public override GH_StateTagList StateTags
  {
    get
    {
      var tags = base.StateTags;
      if (Kind != GH_ParamKind.output)
      {
        return tags;
      }

      if (!IsSchemaBuilderOutput)
      {
        return tags;
      }

      if (UseSchemaTag)
      {
        tags.Add(new SchemaTagStateTag());
      }

      return tags;
    }
  }
}
