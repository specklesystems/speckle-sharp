using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino;
using Rhino.DocObjects;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using Rhino.PlugIns;

namespace SpeckleRhino
{
  public class SchemaBuilderCommand : Command
  {
    // speckle user string for custom schemas
    // TODO: address consistency weak point, since this string needs to match exactly in ConverterRhinoGH.Geometry.cs!
    string SpeckleSchemaKey = "SpeckleSchema";

    RhinoDoc ActiveDoc = null;

    public SchemaBuilderCommand()
    {
      Instance = this;
    }

    public static SchemaBuilderCommand Instance
    {
      get; private set;
    }

    public override string EnglishName
    {
      get { return "SpeckleSchema"; }
    }

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      ActiveDoc = doc;

      const ObjectType geometryFilter = ObjectType.Curve | ObjectType.Surface;
      OptionToggle toggle = new OptionToggle(true, "Remove", "Apply");

      GetObject go = new GetObject();
      go.SetCommandPrompt("Select curves or surfaces");
      go.GeometryFilter = geometryFilter;
      go.AddOptionToggle("BuiltElements", ref toggle);
      go.GroupSelect = true;
      go.SubObjectSelect = false;
      go.EnableClearObjectsOnEntry(false);
      go.EnableUnselectObjectsOnExit(false);
      go.DeselectAllBeforePostSelect = false;

      bool hasPreselected = false;

      for (; ; )
      {
        GetResult res = go.GetMultiple(1, 0);

        if (res == GetResult.Option)
        {
          go.EnablePreSelect(false, true);
          continue;
        }
        else if (res != GetResult.Object)
          return Result.Cancel;

        if (go.ObjectsWerePreselected)
        {
          hasPreselected = true;
          go.EnablePreSelect(false, true);
          continue;
        }
        break;
      }

      if (hasPreselected)
      {
        // Normally, pre-selected objects will remain selected, when a
        // command finishes, and post-selected objects will be unselected.
        // Currently, leave everything selected post command.
        for (int i = 0; i < go.ObjectCount; i++)
        {
          RhinoObject rhinoObject = go.Object(i).Object();
          if (null != rhinoObject)
            rhinoObject.Select(true);
        }
        doc.Views.Redraw();
      }

      List<RhinoObject> commandObjs = go.Objects().Select(o => o.Object()).ToList();
      if (toggle.CurrentValue == true)
        ApplySchemas(commandObjs);
      else
        DeleteSchemas(commandObjs);
      return Result.Success;
    }

    protected void DeleteSchemas(List<RhinoObject> objs)
    {
      foreach (RhinoObject obj in objs)
        obj.Attributes.DeleteUserString(SpeckleSchemaKey);
    }
    protected void ApplySchemas(List<RhinoObject> objs)
    {
      SchemaObjectFilter schemaFilter = new SchemaObjectFilter(objs, ActiveDoc);
      var schemaDictionary = schemaFilter.SchemaDictionary;
      foreach (string schema in schemaDictionary.Keys)
        foreach (RhinoObject obj in schemaDictionary[schema])
          obj.Attributes.SetUserString(SpeckleSchemaKey, schema);
    }
  }
}
