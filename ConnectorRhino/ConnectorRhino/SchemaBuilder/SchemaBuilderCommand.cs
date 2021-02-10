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
  public class SchemaBuilder
  {
    // speckle user string for custom schemas
    // TODO: address consistency weak point, since this string needs to match exactly in ConverterRhinoGH.Geometry.cs!
    static string SpeckleSchemaKey = "SpeckleSchema";
    static string DirectShapeKey = "DirectShape";
    public RhinoDoc ActiveDoc = null;

    public enum SupportedSchema { Floor, Wall, Roof, Ceiling, Column, Beam, none };

    public class ApplySpeckleSchema : Command
    {
      RhinoDoc ActiveDoc = null;

      public ApplySpeckleSchema()
      {
        Instance = this;
      }

      public static ApplySpeckleSchema Instance
      {
        get; private set;
      }

      public override string EnglishName
      {
        get { return "Apply" + SpeckleSchemaKey; }
      }

      protected override Result RunCommand(RhinoDoc doc, RunMode mode)
      {
        ActiveDoc = doc;

        // set option toggle variables
        var toggleAutomatic = new OptionToggle(true, "Off", "On");
        var toggleDirectShape = new OptionToggle(false, "Off", "On");

        // Construct an objects getter
        var getObj = new GetObject();
        getObj.SetCommandPrompt("Select geometry");

        // set up options
        List<string> options = new List<string>();
        GetOption option = new GetOption();
        option.AddOptionToggle("Automatic", ref toggleAutomatic);

        // determine if user selects objects or options
        bool hasPreselected = false;
        string selectedSchema = null;
        for (; ; )
        {
          GetResult res = getObj.GetMultiple(1, 0);

          if (res == GetResult.Option)
          {
            if (toggleAutomatic.CurrentValue == false && getObj.ObjectCount > 0)
            {
              // Construct an options picker for the supported schemas
              var getSchema = new GetOption();
              getSchema.SetCommandPrompt("Schema");
              var schemaDict = GetValidSchemas(getObj.Objects());

              int indexList = getSchema.AddOptionList("schema", schemaDict.Keys.ToList(), 0);
              while (getSchema.Get() == GetResult.Option)
              {
                selectedSchema = getSchema.Option().StringOptionValue;
              }
            }
            getObj.EnablePreSelect(false, true);
            continue;
          }
          else if (res != GetResult.Object)
            return Result.Cancel;

          if (getObj.ObjectsWerePreselected)
          {
            hasPreselected = true;
            getObj.EnablePreSelect(false, true);
            continue;
          }
          break;
        }

        if (hasPreselected)
        {
          // Normally, pre-selected objects will remain selected, when a
          // command finishes, and post-selected objects will be unselected.
          // Currently, leave everything selected post command.
          for (int i = 0; i < getObj.ObjectCount; i++)
          {
            RhinoObject rhinoObject = getObj.Object(i).Object();
            if (null != rhinoObject)
              rhinoObject.Select(true);
          }
          doc.Views.Redraw();
        }

        List<RhinoObject> commandObjs = getObj.Objects().Select(o => o.Object()).ToList();
        if (toggleAutomatic.CurrentValue == true)
          ApplySchemas(commandObjs);
        else
        {
          ApplySchema(commandObjs, options[getObj.OptionIndex()]);
        }
        return Result.Success;
      }
      protected void ApplySchemas(List<RhinoObject> objs) // this is the automatic option
      {
        SchemaObjectFilter schemaFilter = new SchemaObjectFilter(objs, ActiveDoc);
        var schemaDictionary = schemaFilter.SchemaDictionary;
        foreach (string schema in schemaDictionary.Keys)
          foreach (RhinoObject obj in schemaDictionary[schema])
            obj.Attributes.SetUserString(SpeckleSchemaKey, schema);
      }
      protected void ApplySchema(List<RhinoObject> objs, string schema, bool asNative = true)
      {
        foreach (RhinoObject obj in objs)
        {
          if (asNative)
            obj.Attributes.SetUserString(SpeckleSchemaKey, schema);
          else
            obj.Attributes.SetUserString(SpeckleSchemaKey, DirectShapeKey+":"+schema);
        }
      }
      protected Dictionary<string, int> GetValidSchemas(ObjRef[] objs) // this returns a dictionary of schema string and its index
      {
        string[] validSchemas = null;
        var objTypes = objs.Select(o => o.Object().ObjectType).Distinct().ToList();
        if (objTypes.Count == 1)
        {
          switch (objTypes[0])
          {
            case ObjectType.Curve:
              validSchemas = new string[] {
              SupportedSchema.Beam.ToString(),
              SupportedSchema.Column.ToString() }; break;
            case ObjectType.Surface:
              validSchemas = new string[] {
              SupportedSchema.Ceiling.ToString(),
              SupportedSchema.Floor.ToString(),
              SupportedSchema.Wall.ToString(),
              SupportedSchema.Roof.ToString() }; break;
            case ObjectType.Brep | ObjectType.Mesh:
              break;
            default:
              break;
          }
        }

        var schemaDict = new Dictionary<string, int>();
        if (validSchemas != null)
          for (int i = 0; i < validSchemas.Length; i++)
            schemaDict.Add(validSchemas[i], i);

        return schemaDict;
      }
    }

    public class RemoveSpeckleSchema : Command
    {

      public RemoveSpeckleSchema()
      {
        Instance = this;
      }

      public static RemoveSpeckleSchema Instance
      {
        get; private set;
      }

      public override string EnglishName
      {
        get { return "Remove" + SpeckleSchemaKey; }
      }

      protected override Result RunCommand(RhinoDoc doc, RunMode mode)
      {
        GetObject go = new GetObject();
        go.SetCommandPrompt("Select objects for schema removal");
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
          for (int i = 0; i < go.ObjectCount; i++)
          {
            RhinoObject rhinoObject = go.Object(i).Object();
            if (null != rhinoObject)
              rhinoObject.Select(true);
          }
          doc.Views.Redraw();
        }

        List<RhinoObject> objs = go.Objects().Select(o => o.Object()).ToList();
        foreach (RhinoObject obj in objs)
          obj.Attributes.DeleteUserString(SpeckleSchemaKey);
        return Result.Success;
      }
    }
  }
  
}
