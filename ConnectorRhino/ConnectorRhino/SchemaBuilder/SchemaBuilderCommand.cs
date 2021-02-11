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

        // Command variables
        var selectedObjects = new List<RhinoObject>();
        string selectedSchema = "";
        bool AsDirectShape = false;

        // Construct an objects getter
        // This includes an option toggle for "Automatic" (true means automagic schema application, no directshapes)
        var getObj = new GetObject();
        getObj.SetCommandPrompt("Select geometry");
        var toggleAutomatic = new OptionToggle(true, "Off", "On");
        getObj.AddOptionToggle("Automatic", ref toggleAutomatic);
        getObj.GroupSelect = true;
        getObj.SubObjectSelect = false;
        getObj.EnableClearObjectsOnEntry(false);
        getObj.EnableUnselectObjectsOnExit(false);
        getObj.DeselectAllBeforePostSelect = false;

        // Get objects
        for (; ; )
        {
          GetResult res = getObj.GetMultiple(1, 0);
          if (res == GetResult.Option)
          {
            getObj.EnablePreSelect(false, true);
            continue;
          }
          else if (res != GetResult.Object)
            return Result.Cancel;
          if (getObj.ObjectsWerePreselected)
          {
            getObj.EnablePreSelect(false, true);
            continue;
          }
          break;
        }
        selectedObjects = getObj.Objects().Select(o => o.Object()).ToList();

        // Construct an options getter if "Automatic" was set to "off"
        if (toggleAutomatic.CurrentValue == false)
        {
          // Construct an options getter for schema options
          // This includes an option toggle for "DirectShape" (true will asign selected schema as the family)
          // Also includes an option list of supported schemas
          var getOpt = new GetOption();
          getOpt.SetCommandPrompt("Select schema options. Press Enter when done");
          var toggleDirectShape = new OptionToggle(false, "Off", "On");
          var directShapeIndex = getOpt.AddOptionToggle("DirectShape", ref toggleDirectShape);
          List<string> schemas = Enum.GetNames(typeof(SupportedSchema)).ToList();
          int schemaListOptionIndex = getOpt.AddOptionList("Schema", schemas, 0);

          // Get options
          while (getOpt.Get() == GetResult.Option)
          {
            if (getOpt.OptionIndex() == schemaListOptionIndex)
              selectedSchema = schemas[getOpt.Option().CurrentListOptionIndex];
            if (getOpt.OptionIndex() == directShapeIndex)
              AsDirectShape = toggleDirectShape.CurrentValue;
          }
        }

        // run the corresponding methods based on command info
        if (toggleAutomatic.CurrentValue == true)
          ApplySchemas(selectedObjects);
        else
          ApplySchema(selectedObjects, selectedSchema, AsDirectShape);
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
      protected ObjectType GetCustomObjectType(ObjRef obj) // in place just to handle single surface brep -> srf
      {
        switch (obj.Object().ObjectType)
        {
          case ObjectType.Brep:
            Rhino.Geometry.Brep brep = obj.Brep();
            if (brep.Faces.Count == 1)
              return ObjectType.Surface;
            else return obj.Object().ObjectType;
          case ObjectType.Curve:
          case ObjectType.Surface:
          case ObjectType.Mesh:
          default:
            return obj.Object().ObjectType;
        }
      }
      protected Dictionary<string, int> GetValidSchemas(ObjRef[] objs) // this returns a dictionary of schema string and its index
      {
        string[] validSchemas = null;
        var objTypes = objs.Select(o => GetCustomObjectType(o)).Distinct().ToList();
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
            case ObjectType.Brep:
            case ObjectType.Mesh:
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
