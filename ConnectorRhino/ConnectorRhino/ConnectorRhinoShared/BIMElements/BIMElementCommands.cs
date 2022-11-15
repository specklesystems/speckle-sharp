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
  public class SpeckleBIM
  {
    // speckle user string for custom schemas
    // TODO: address consistency weak point, since this string needs to match exactly in ConverterRhinoGH.Geometry.cs!
    static string SpeckleSchemaKey = "SpeckleSchema";
    static string DirectShapeKey = "DirectShape";
    
    public class AutomaticBIM : Command
    {
      public override string EnglishName => "CreateAutomatic";

      protected override Result RunCommand(RhinoDoc doc, RunMode mode)
      {
        var selectedObjects = GetObjectSelection();
        if (selectedObjects == null)
          return Result.Cancel;
        ApplySchemas(selectedObjects, doc);
        return Result.Success;
      }
    }

    public class CreateWall : Command
    {
      public override string EnglishName => "CreateWall";

      protected override Result RunCommand(RhinoDoc doc, RunMode mode)
      {
        var selectedObjects = GetObjectSelection(new List<ObjectType> { ObjectType.Brep });
        if (selectedObjects == null)
          return Result.Cancel;
        ApplySchema(selectedObjects, SchemaObjectFilter.SupportedSchema.Wall.ToString(), doc, false);
        return Result.Success;
      }
    }

    public class CreateFloor : Command
    {
      public override string EnglishName => "CreateFloor";

      protected override Result RunCommand(RhinoDoc doc, RunMode mode)
      {
        var selectedObjects = GetObjectSelection(new List<ObjectType> { ObjectType.Brep });
        if (selectedObjects == null)
          return Result.Cancel;
        ApplySchema(selectedObjects, SchemaObjectFilter.SupportedSchema.Floor.ToString(), doc, false);
        return Result.Success;
      }
    }

    public class CreateColumn : Command
    {
      public override string EnglishName => "CreateColumn";

      protected override Result RunCommand(RhinoDoc doc, RunMode mode)
      {
        var selectedObjects = GetObjectSelection(new List<ObjectType> { ObjectType.Curve });
        if (selectedObjects == null)
          return Result.Cancel;
        ApplySchema(selectedObjects, SchemaObjectFilter.SupportedSchema.Column.ToString(), doc, false);
        return Result.Success;
      }
    }

    public class CreateBeam : Command
    {
      public override string EnglishName => "CreateBeam";

      protected override Result RunCommand(RhinoDoc doc, RunMode mode)
      {
        var selectedObjects = GetObjectSelection(new List<ObjectType> { ObjectType.Curve });
        if (selectedObjects == null)
          return Result.Cancel;
        ApplySchema(selectedObjects, SchemaObjectFilter.SupportedSchema.Beam.ToString(), doc, false);
        return Result.Success;
      }
    }

    public class CreatePipe : Command
    {
      public override string EnglishName => "CreatePipe";

      protected override Result RunCommand(RhinoDoc doc, RunMode mode)
      {
        var selectedObjects = GetObjectSelection(new List<ObjectType> { ObjectType.Curve });
        if (selectedObjects == null)
          return Result.Cancel;
        ApplySchema(selectedObjects, SchemaObjectFilter.SupportedSchema.Pipe.ToString(), doc, false);
        return Result.Success;
      }
    }

    public class CreateDuct : Command
    {
      public override string EnglishName => "CreateDuct";

      protected override Result RunCommand(RhinoDoc doc, RunMode mode)
      {
        var selectedObjects = GetObjectSelection(new List<ObjectType> { ObjectType.Curve });
        if (selectedObjects == null)
          return Result.Cancel;
        ApplySchema(selectedObjects, SchemaObjectFilter.SupportedSchema.Duct.ToString(), doc, false);
        return Result.Success;
      }
    }

    public class CreateFaceWall : Command
    {
      public override string EnglishName => "CreateFaceWall";

      protected override Result RunCommand(RhinoDoc doc, RunMode mode)
      {
        var selectedObjects = GetObjectSelection(new List<ObjectType> { ObjectType.Brep });
        if (selectedObjects == null)
          return Result.Cancel;
        ApplySchema(selectedObjects, SchemaObjectFilter.SupportedSchema.FaceWall.ToString(), doc, false);
        return Result.Success;
      }
    }

    public class CreateTopography : Command
    {
      public override string EnglishName => "CreateTopography";

      protected override Result RunCommand(RhinoDoc doc, RunMode mode)
      {
        var selectedObjects = GetObjectSelection(new List<ObjectType> { ObjectType.Mesh , ObjectType.Brep, ObjectType.SubD});
        if (selectedObjects == null)
          return Result.Cancel;
        ApplySchema(selectedObjects, SchemaObjectFilter.SupportedSchema.Topography.ToString(), doc, false);
        return Result.Success;
      }
    }

    public class CreateDirectShape : Command
    {
      public override string EnglishName => "CreateDirectShape";

      protected override Result RunCommand(RhinoDoc doc, RunMode mode)
      {
        var selectedObjects = GetObjectSelection();
        if (selectedObjects == null)
          return Result.Cancel;

        // Construct an options getter for BIM element types
        // DirectShape assigns selected type as the family
        var getOpt = new GetOption();
        getOpt.SetCommandPrompt("Select BIM type. Press Enter when done");
        List<string> schemas = Enum.GetNames(typeof(SchemaObjectFilter.SupportedSchema)).ToList();
        string selectedSchema = schemas[0];
        int schemaListOptionIndex = getOpt.AddOptionList("Type", schemas, 0);

        // Get options
        while (getOpt.Get() == GetResult.Option)
          if (getOpt.OptionIndex() == schemaListOptionIndex)
            selectedSchema = schemas[getOpt.Option().CurrentListOptionIndex];

        ApplySchema(selectedObjects, selectedSchema, doc, true);
        return Result.Success;
      }
    }

    public class CreateAdaptiveComponent : Command
    {
      public override string EnglishName => "CreateAdaptiveComponent";

      protected override Result RunCommand(RhinoDoc doc, RunMode mode)
      {
        string family = "";

        // Get family from user
        var getFamily = new GetString();
        getFamily.SetCommandPrompt("Enter Family name");
        getFamily.Get();
        family = getFamily.StringResult();
        if (string.IsNullOrEmpty(family.Trim()))
          return Result.Nothing;

        // get point selection in and create blocks from each group
        var getObj = new GetObject();
        getObj.SetCommandPrompt("Select points in order");
        getObj.SubObjectSelect = false;
        getObj.GeometryFilter = ObjectType.Point;
        getObj.OneByOnePostSelect = true;
        List<RhinoObject> selectedObjects = new List<RhinoObject>();
        for (; ; )
        {
          GetResult res = getObj.GetMultiple(3, 0);
          if (res == GetResult.Object)
          {
            //make block from points and assign adaptive component schema
            var blockName = $"AdaptiveComponent-{Guid.NewGuid()}";

            // Gather all of the selected objects
            var geometry = new List<Rhino.Geometry.GeometryBase>();
            var attributes = new List<ObjectAttributes>();
            for (int i = 0; i < getObj.ObjectCount; i++)
            {
              var rhinoObject = getObj.Object(i).Object();
              if (rhinoObject != null)
              {
                geometry.Add(rhinoObject.Geometry);
                attributes.Add(rhinoObject.Attributes);
              }
            }
            var basePoint = geometry.First() as Rhino.Geometry.Point; // Use first selected point as basepoint

            // Gather all of the selected objects and make definition
            int definitionIndex = doc.InstanceDefinitions.Add(blockName, string.Empty, basePoint.Location, geometry, attributes);
            if (definitionIndex < 0)
            {
              RhinoApp.WriteLine("Unable to create block definition ", blockName);
              return Result.Failure;
            }

            // add the objects to a block instance
            var transform = Rhino.Geometry.Transform.Translation(basePoint.Location - doc.ModelBasepoint);
            Guid instanceId = doc.Objects.AddInstanceObject(definitionIndex, transform);
            if (instanceId == Guid.Empty)
            {
              RhinoApp.WriteLine("Unable to create block instance ", blockName);
              return Result.Failure;
            }
            var instance = doc.Objects.FindId(instanceId) as InstanceObject;

            // attach user string to block instance
            ApplyAdaptiveComponent(instance, family, family, doc);

            // clear everything for next selection
            selectedObjects.Clear();
            getObj.ClearObjects();
            continue;
          }
          else if (res == GetResult.Cancel)
          {
            break;
          }
          break;
        }
        
        return Result.Success;
      }
    }

    public class ApplySpeckleSchema : Command
    {
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
          // This includes an option toggle for "DirectShape" (true will assign selected schema as the family)
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
          ApplySchemas(selectedObjects, doc);
        else
          ApplySchema(selectedObjects, selectedSchema, doc, directShape);
        return Result.Success;
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
        GetObject getObjs = new GetObject();
        getObjs.SetCommandPrompt("Select objects for schema removal");
        getObjs.GroupSelect = true;
        getObjs.SubObjectSelect = false;
        getObjs.EnableClearObjectsOnEntry(false);
        getObjs.EnableUnselectObjectsOnExit(false);
        getObjs.DeselectAllBeforePostSelect = false;

        for (; ; )
        {
          GetResult res = getObjs.GetMultiple(1, 0);

          if (res != GetResult.Object)
            return Result.Cancel;

          if (getObjs.ObjectsWerePreselected)
          {
            getObjs.EnablePreSelect(false, true);
            continue;
          }
          break;
        }

        List<RhinoObject> objs = getObjs.Objects().Select(o => o.Object()).ToList();
        foreach (RhinoObject obj in objs)
          obj.Attributes.DeleteUserString(SpeckleSchemaKey);
        return Result.Success;
      }
    }

    #region helper methods
    private static List<RhinoObject> GetObjectSelection(List<ObjectType> filter = null)
    {
      filter = filter ?? new List<ObjectType> { ObjectType.AnyObject };
      // Construct an objects getter
      var getObj = new GetObject();
      getObj.SetCommandPrompt("Select geometry");
      getObj.GroupSelect = true;
      getObj.SubObjectSelect = false;
      getObj.EnableClearObjectsOnEntry(false);
      getObj.EnableUnselectObjectsOnExit(false);
      getObj.DeselectAllBeforePostSelect = false;
      getObj.GeometryFilter = filter.Aggregate((a,b) => a | b);

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
          return null;
        if (getObj.ObjectsWerePreselected)
        {
          getObj.EnablePreSelect(false, true);
          continue;
        }
        break;
      }

      return getObj.Objects().Select(o => o.Object()).ToList();
    }

    // This is the automagic method
    protected static void ApplySchemas(List<RhinoObject> objs, RhinoDoc ActiveDoc)
    {
      var schemaFilter = new SchemaObjectFilter(objs, ActiveDoc);
      var schemaDictionary = schemaFilter.SchemaDictionary;
      foreach (string schema in schemaDictionary.Keys)
        foreach (RhinoObject obj in schemaDictionary[schema])
          WriteUserString(obj, schema);
    }

    // This is the manual method
    protected static void ApplySchema(List<RhinoObject> objs, string schema, RhinoDoc ActiveDoc, bool asDirectShape = false)
    {
      // test for direct shape - apply user string as long as objs are breps and meshes
      if (asDirectShape)
      {
        foreach (RhinoObject obj in objs)
          if (obj.ObjectType == ObjectType.Brep || obj.ObjectType == ObjectType.Extrusion || obj.ObjectType == ObjectType.Mesh)
            WriteUserString(obj, schema, true);
      }
      else
      {
        var schemaFilter = new SchemaObjectFilter(objs, ActiveDoc, schema);
        foreach (RhinoObject obj in schemaFilter.SchemaDictionary[schema])
          WriteUserString(obj, schema);
      }
    }

    protected static void ApplyAdaptiveComponent(RhinoObject instance,  string family, string type, RhinoDoc ActiveDoc)
    {
      // assign this block instance an adaptive component speckle schema 
      string uniqueName = $"AdaptiveComponent_{instance.Id.ToString().Substring(0, 5)}";
      string value = $"AdaptiveComponent({family},{type})";

      instance.Attributes.SetUserString(SpeckleSchemaKey, value);
    }

    private static void WriteUserString(RhinoObject obj, string schema, bool asDirectShape = false)
    {
      string value = schema;
      if (asDirectShape)
      {
        // generate placeholder unique name for revit
        string uniqueName = $"DirectShape_{obj.Id.ToString().Substring(0,5)}";
        value = $"{DirectShapeKey}({schema},{uniqueName})";
      }
      if (Enum.TryParse<SchemaObjectFilter.SupportedSchema>(schema, out var parsedSchema))
      {
        switch (parsedSchema)
        {
          case SchemaObjectFilter.SupportedSchema.FaceWall:
            value = $"{schema}([family],[type])";
            break;
          case SchemaObjectFilter.SupportedSchema.Pipe:
            value = $"{schema}(1.0)";
            break;
          case SchemaObjectFilter.SupportedSchema.Duct:
            value = $"{schema}(1.0,1.0,1.0)";
            break;
        }
      }

      obj.Attributes.SetUserString(SpeckleSchemaKey, value);
    }

    #endregion
  }
  
}
