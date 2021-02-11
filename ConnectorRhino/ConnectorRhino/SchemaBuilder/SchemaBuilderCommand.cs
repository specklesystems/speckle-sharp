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
        bool automatic = true;
        bool directShape = false;

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
        automatic = toggleAutomatic.CurrentValue;

        // Construct an options getter if "Automatic" was set to "off"
        if (!automatic)
        {
          // Construct an options getter for schema options
          // This includes an option toggle for "DirectShape" (true will asign selected schema as the family)
          // Also includes an option list of supported schemas
          var getOpt = new GetOption();
          getOpt.SetCommandPrompt("Select schema options. Press Enter when done");
          var toggleDirectShape = new OptionToggle(false, "Off", "On");
          var directShapeIndex = getOpt.AddOptionToggle("DirectShape", ref toggleDirectShape);
          List<string> schemas = Enum.GetNames(typeof(SchemaObjectFilter.SupportedSchema)).ToList();
          int schemaListOptionIndex = getOpt.AddOptionList("Schema", schemas, 0);

          // Get options
          while (getOpt.Get() == GetResult.Option)
          {
            if (getOpt.OptionIndex() == schemaListOptionIndex)
              selectedSchema = schemas[getOpt.Option().CurrentListOptionIndex];
            if (getOpt.OptionIndex() == directShapeIndex)
              directShape = toggleDirectShape.CurrentValue;
          }
        }

        // Apply schemas
        if (automatic)
          ApplySchemas(selectedObjects);
        else
          ApplySchema(selectedObjects, selectedSchema, directShape);
        return Result.Success;
      }
      
      // This is the automagic method
      protected void ApplySchemas(List<RhinoObject> objs)
      {
        var schemaFilter = new SchemaObjectFilter(objs, ActiveDoc);
        var schemaDictionary = schemaFilter.SchemaDictionary;
        foreach (string schema in schemaDictionary.Keys)
          foreach (RhinoObject obj in schemaDictionary[schema])
            WriteUserString(obj, schema);
      }

      // This is the manual method
      protected void ApplySchema(List<RhinoObject> objs, string schema, bool asDirectShape = false)
      {
        // test for direct shape - apply user string as long as objs are breps and meshes
        if (asDirectShape)
        {
          foreach (RhinoObject obj in objs)
            if (obj.ObjectType == ObjectType.Brep || obj.ObjectType == ObjectType.Mesh)
              WriteUserString(obj, schema, true);
        }
        else
        {
          var schemaFilter = new SchemaObjectFilter(objs, ActiveDoc, schema);
          foreach (RhinoObject obj in schemaFilter.SchemaDictionary[schema])
            WriteUserString(obj, schema);
        }
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

    public static void WriteUserString(RhinoObject obj, string schema, bool asDirectShape = false)
    {
      string value = schema;
      if (asDirectShape)
        value = $"{DirectShapeKey}({schema},DirectShapeName)";
      obj.Attributes.SetUserString(SpeckleSchemaKey, value);
    }
  }
  
}
