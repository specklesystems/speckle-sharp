using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using ConnectorGrasshopper.Objects;
using ConnectorGrasshopperUtils;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Logging = Speckle.Core.Logging;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper
{
  public abstract class CreateSchemaObjectBase : SelectKitComponentBase, IGH_VariableParameterComponent
  {
    public override GH_Exposure Exposure => SpeckleGHSettings.GetTabVisibility(Category)
      ? GH_Exposure.tertiary
      : GH_Exposure.hidden;

    protected ConstructorInfo SelectedConstructor;
    private bool readFailed;
    protected override Bitmap Icon => Properties.Resources.SchemaBuilder;
    public string Seed;

    private DebounceDispatcher nicknameChangeDebounce = new DebounceDispatcher();

    public CreateSchemaObjectBase(string name, string nickname, string description, string category, string subCategory)
      : base(name, nickname, description, category, subCategory)
    {
      Seed = GenerateSeed();
    }

    private bool UseSchemaTag;
    private bool UserSetSchemaTag;

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);
      Menu_AppendSeparator(menu);
      var schemaConversionHeader = Menu_AppendItem(menu, "Select a Schema conversion option:");

      ParameterInfo mainParam = null;

      try
      {
        mainParam = SelectedConstructor.GetParameters().FirstOrDefault(cParam => CustomAttributeData.GetCustomAttributes(cParam)
          ?.Where(o => o.AttributeType.IsEquivalentTo(typeof(SchemaMainParam)))?.Count() > 0);
      }
      catch (Exception e)
      {
      }

      var objectItem = schemaConversionHeader.DropDownItems.Add("Convert as Schema object.") as ToolStripMenuItem;
      objectItem.Checked = !UseSchemaTag;
      objectItem.ToolTipText = "The default behaviour. Output will be the specified object schema.";

      var tagItem = schemaConversionHeader.DropDownItems.Add($"Convert as {mainParam?.Name ?? "geometry"} with {Name} attached") as ToolStripMenuItem;
      tagItem.Checked = UseSchemaTag;
      tagItem.Enabled = mainParam != null;
      tagItem.ToolTipText =
        "Enables Schema conversion while prioritizing the geometry over the schema.\n\nSchema information will e stored in a '@SpeckleSchema' property.";

      var speckleBaseParam = (Params.Output[0] as SpeckleBaseParam);
      tagItem.Click += (sender, args) =>
      {
        UseSchemaTag = true;
        speckleBaseParam.UseSchemaTag = UseSchemaTag;
        speckleBaseParam.ExpirePreview(true);
        UserSetSchemaTag = true;
        ExpireSolution(true);
      };

      objectItem.Click += (sender, args) =>
      {
        UseSchemaTag = false;
        speckleBaseParam.UseSchemaTag = UseSchemaTag;
        speckleBaseParam.ExpirePreview(true);

        UserSetSchemaTag = true;
        ExpireSolution(true);
      };

    }

    public string GenerateSeed()
    {
      return new string(Speckle.Core.Models.Utilities.hashString(Guid.NewGuid().ToString()).Take(20).ToArray());
    }

    public override bool Read(GH_IReader reader)
    {
      try
      {
        var constructorName = reader.GetString("SelectedConstructorName");
        var typeName = reader.GetString("SelectedTypeName");
        try
        {
          UseSchemaTag = reader.GetBoolean("UseSchemaTag");
          UserSetSchemaTag = reader.GetBoolean("UserSetSchemaTag");
        }
        catch
        {
        }

        SelectedConstructor = CSOUtils.FindConstructor(constructorName, typeName);
        if (SelectedConstructor == null)
          readFailed = true;
      }
      catch
      {
        readFailed = true;
      }

      try
      {
        Seed = reader.GetString("seed");
      }
      catch
      {
      }

      return base.Read(reader);
    }

    public override bool Write(GH_IWriter writer)
    {
      if (SelectedConstructor != null)
      {
        writer.SetString("SelectedConstructorName", CSOUtils.MethodFullName(SelectedConstructor));
        writer.SetString("SelectedTypeName", SelectedConstructor.DeclaringType.FullName);
      }

      writer.SetBoolean("UseSchemaTag", UseSchemaTag);
      writer.SetBoolean("UserSetSchemaTag", UserSetSchemaTag);
      writer.SetString("seed", Seed);
      return base.Write(writer);
    }


    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      //pManager.AddGenericParameter("Debug", "d", "debug output, please ignore", GH_ParamAccess.list);
      pManager.AddParameter(new SpeckleBaseParam("Speckle Object", "O", "Created speckle object", GH_ParamAccess.item,
        true));
    }

    public override void AddedToDocument(GH_Document document)
    {
      if (readFailed)
        return;

      if (!UserSetSchemaTag)
        UseSchemaTag = SpeckleGHSettings.UseSchemaTag;

      if (SelectedConstructor != null)
      {
        base.AddedToDocument(document);
        if (Grasshopper.Instances.ActiveCanvas.Document != null)
        {
          var otherSchemaBuilders =
            Grasshopper.Instances.ActiveCanvas.Document.FindObjects(new List<string>() { Name }, 10000);
          foreach (var comp in otherSchemaBuilders)
          {
            if (comp is CreateSchemaObject scb)
            {
              if (scb.Seed == Seed)
              {
                Seed = GenerateSeed();
                break;
              }
            }
            var baseType = comp.GetType().BaseType;
            if (typeof(CreateSchemaObjectBase) == baseType)
            {
              var csob = (CreateSchemaObjectBase)comp;
              if (csob.Seed == Seed)
              {
                Seed = GenerateSeed();
                break;
              }
            }
          }
        }
      }

      if (Params.Input.Count == 0) SetupComponent(SelectedConstructor);
      ((SpeckleBaseParam)Params.Output[0]).UseSchemaTag = UseSchemaTag;
      (Params.Output[0] as SpeckleBaseParam).ExpirePreview(true);

      Params.ParameterChanged += (sender, args) =>
      {
        if (args.ParameterSide != GH_ParameterSide.Input) return;
        switch (args.OriginalArguments.Type)
        {
          case GH_ObjectEventType.NickName:
            // This means the user is typing characters, debounce until it stops for 400ms before expiring the solution.
            // Prevents UI from locking too soon while writing new names for inputs.
            args.Parameter.Name = args.Parameter.NickName;
            nicknameChangeDebounce.Debounce(400, (e) => ExpireSolution(true));
            break;
          case GH_ObjectEventType.NickNameAccepted:
            args.Parameter.Name = args.Parameter.NickName;
            ExpireSolution(true);
            break;
        }
      };

      base.AddedToDocument(document);
    }

    public void SetupComponent(ConstructorInfo constructor)
    {
      int k = 0;
      var props = constructor.GetParameters();

      foreach (var p in props)
      {
        RegisterPropertyAsInputParameter(p, k++);
      }

      Name = constructor.GetCustomAttribute<SchemaInfo>().Name;
      Description = constructor.GetCustomAttribute<SchemaInfo>().Description;

      Message = constructor.DeclaringType?.FullName?.Split('.')[0];
      SelectedConstructor = constructor;
      Params.Output[0].NickName = constructor.DeclaringType?.Name;
    }

    /// <summary>
    /// Adds a property to the component's inputs.
    /// </summary>
    /// <param name="param"></param>
    private void RegisterPropertyAsInputParameter(ParameterInfo param, int index)
    {
      // get property name and value
      Type propType = param.ParameterType;
      if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
        propType = Nullable.GetUnderlyingType(propType);

      string propName = param.Name;
      object propValue = param;

      var inputDesc = param.GetCustomAttribute<SchemaParamInfo>();
      var d = inputDesc != null ? inputDesc.Description : "";
      if (param.IsOptional)
      {
        if (!string.IsNullOrEmpty(d))
          d += ", ";
        var def = param.DefaultValue == null ? "null" : param.DefaultValue.ToString();
        d += "default = " + def;
      }

      // Create new param based on property name
      SpeckleStatefulParam newInputParam = new SpeckleStatefulParam();
      newInputParam.Name = propName;
      newInputParam.NickName = propName;
      newInputParam.MutableNickName = false;
      newInputParam.Detachable = false;
      
      newInputParam.Description = $"({propType.Name}) {d}";
      newInputParam.Optional = param.IsOptional;
      if (param.IsOptional)
        newInputParam.SetPersistentData(param.DefaultValue);

      // check if input needs to be a list or item access
      bool isCollection = typeof(System.Collections.IEnumerable).IsAssignableFrom(propType) &&
                          propType != typeof(string) && !propType.Name.ToLower().Contains("dictionary");
      if (isCollection == true)
      {
        newInputParam.Access = GH_ParamAccess.list;
      }
      else
      {
        newInputParam.Access = GH_ParamAccess.item;
      }

      Params.RegisterInputParam(newInputParam, index);

      //add dropdown
      if (propType.IsEnum)
      {
        //expire solution so that node gets proper size
        ExpireSolution(true);

        var instance = Activator.CreateInstance(propType);

        var vals = Enum.GetValues(propType).Cast<Enum>().Select(x => x.ToString()).ToList();
        var options = CreateDropDown(propName, vals, Attributes.Bounds.X, Params.Input[index].Attributes.Bounds.Y);
        OnPingDocument().AddObject(options, false);
        Params.Input[index].AddSource(options);
      }
    }

    public static GH_ValueList CreateDropDown(string name, List<string> values, float x, float y)
    {
      var valueList = new GH_ValueList();
      valueList.CreateAttributes();
      valueList.Name = name;
      valueList.NickName = name + ":";
      valueList.Description = "Select an option...";
      valueList.ListMode = GH_ValueListMode.DropDown;
      valueList.ListItems.Clear();

      for (int i = 0; i < values.Count; i++)
      {
        valueList.ListItems.Add(new GH_ValueListItem(values[i], i.ToString()));
      }

      valueList.Attributes.Pivot = new PointF(x - 200, y - 10);

      return valueList;
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (readFailed)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
          "This component has changed or cannot be found, please create a new one");
        return;
      }

      if (SelectedConstructor is null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No schema has been selected.");
        return;
      }

      if (DA.Iteration == 0)
        Tracker.TrackNodeRun("Create Schema Object", Name);


      var units = Units.GetUnitsFromString(Rhino.RhinoDoc.ActiveDoc.GetUnitSystemName(true, false, false, false));

      List<object> cParamsValues = new List<object>();
      var cParams = SelectedConstructor.GetParameters();
      object mainSchemaObj = null;
      for (var i = 0; i < cParams.Length; i++)
      {
        var cParam = cParams[i];
        var param = Params.Input[i];
        object objectProp = null;
        if (param.Access == GH_ParamAccess.list)
        {
          var inputValues = new List<object>();
          DA.GetDataList(i, inputValues);
          if (!inputValues.Any() && !param.Optional)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input list `" + param.Name + "` is empty.");
            return;
          }

          try
          {
            inputValues = inputValues.Select(x => ExtractRealInputValue(x)).ToList();
            objectProp = GetObjectListProp(param, inputValues, cParam.ParameterType);
          }
          catch (Exception e)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.InnerException?.Message ?? e.Message);
            return;
          }
        }
        else if (param.Access == GH_ParamAccess.item)
        {
          object inputValue = null;
          DA.GetData(i, ref inputValue);
          var extractRealInputValue = ExtractRealInputValue(inputValue);
          objectProp = GetObjectProp(param, extractRealInputValue, cParam.ParameterType);
        }

        cParamsValues.Add(objectProp);
        if (CustomAttributeData.GetCustomAttributes(cParam)
          ?.Where(o => o.AttributeType.IsEquivalentTo(typeof(SchemaMainParam)))?.Count() > 0)
          mainSchemaObj = objectProp;
      }


      object schemaObject = null;
      try
      {
        schemaObject = SelectedConstructor.Invoke(cParamsValues.ToArray());
        ((Base)schemaObject).applicationId = $"{Seed}-{SelectedConstructor.DeclaringType.FullName}-{DA.Iteration}";
        if (((Base)schemaObject)["units"] == null || ((Base)schemaObject)["units"] == "")
          ((Base)schemaObject)["units"] = units;
      }
      catch (Exception e)
      {

        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.InnerException?.Message ?? e.Message);
        return;
      }

      // create commit obj from main geometry param and try to attach schema obj. use schema obj if no main geom param was found.
      Base commitObj = (Base)schemaObject;
      if (UseSchemaTag)
      {
        commitObj = commitObj.ShallowCopy();
        try
        {
          if (mainSchemaObj == null)
          {
            UseSchemaTag = false;
            ((SpeckleBaseParam)Params.Output[0]).UseSchemaTag = UseSchemaTag;
            throw new Exception("Schema tag is not supported for this object type, will return Schema object instead.");
          }

          commitObj = ((Base)mainSchemaObj).ShallowCopy();
          commitObj["@SpeckleSchema"] = schemaObject;
          if (commitObj["units"] == null || commitObj["units"] == "")
            commitObj["units"] = units;
        }
        catch (Exception e)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, e.Message);
        }
      }

      // Finally, add any custom props created by the user.
      for (var j = cParams.Length; j < Params.Input.Count; j++)
      {
        // Additional props added to the object
        var ghParam = Params.Input[j];
        if (ghParam.Access == GH_ParamAccess.item)
        {
          object input = null;
          DA.GetData(j, ref input);

          commitObj[ghParam.Name] = Utilities.TryConvertItemToSpeckle(input, Converter);
        }
        else if (ghParam.Access == GH_ParamAccess.list)
        {
          List<object> input = new List<object>();
          DA.GetDataList(j, input);
          commitObj[ghParam.Name] = input.Select(i => Utilities.TryConvertItemToSpeckle(i, Converter)).ToList();
        }
      }

      DA.SetData(0, new GH_SpeckleBase() { Value = commitObj });
    }

    private object ExtractRealInputValue(object inputValue)
    {
      if (inputValue == null)
        return null;

      if (inputValue is Grasshopper.Kernel.Types.IGH_Goo)
      {
        var type = inputValue.GetType();
        var propertyInfo = type.GetProperty("Value");
        var value = propertyInfo.GetValue(inputValue);
        return value;
      }

      return inputValue;
    }

    //list input
    private object GetObjectListProp(IGH_Param param, List<object> values, Type t)
    {
      if (!values.Any()) return null;

      var list = (IList)Activator.CreateInstance(t);
      var listElementType = list.GetType().GetGenericArguments().Single();
      foreach (var value in values)
      {
        list.Add(ConvertType(listElementType, value, param.Name));
      }

      return list;
    }

    private object GetObjectProp(IGH_Param param, object value, Type t)
    {
      var convertedValue = ConvertType(t, value, param.Name);
      return convertedValue;
    }

    private object ConvertType(Type type, object value, string name)
    {
      if (value == null)
      {
        return null;
      }

      var typeOfValue = value.GetType();
      if (value == null || typeOfValue == type || type.IsAssignableFrom(typeOfValue))
        return value;

      //needs to be before IsSimpleType
      if (type.IsEnum)
      {
        try
        {
          return Enum.Parse(type, value.ToString());
        }
        catch
        {
        }
      }

      // int, doubles, etc
      if (Speckle.Core.Models.Utilities.IsSimpleType(value.GetType()))
      {
        try
        {
          return Convert.ChangeType(value, type);
        }
        catch (Exception e)
        {
          throw new Exception($"Cannot convert {value.GetType()} to {type}");
        }
      }

      if (Converter.CanConvertToSpeckle(value))
      {
        var converted = Converter.ConvertToSpeckle(value);
        //in some situations the converted type is not exactly the type needed by the constructor
        //even if an implicit casting is defined, invoking the constructor will fail because the type is boxed
        //to convert the boxed type, it seems the only valid solution is to use Convert.ChangeType
        //currently, this is to support conversion of Polyline to Polycurve in Objects
        if (converted.GetType() != type && !type.IsAssignableFrom(converted.GetType()))
        {
          try
          {
            return Convert.ChangeType(converted, type);
          }
          catch (Exception e)
          {
            throw new Exception($"Cannot convert {converted.GetType()} to {type}");
          }
        }

        return converted;
      }
      else
      {
        // Log conversion error?
      }

      //tries an explicit casting, given that the required type is a variable, we need to use reflection
      //not really sure this method is needed
      try
      {
        MethodInfo castIntoMethod = this.GetType().GetMethod("CastObject").MakeGenericMethod(type);
        return castIntoMethod.Invoke(null, new[] { value });
      }
      catch
      {
      }

      AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to set " + name + ".");
      throw new Logging.SpeckleException($"Could not covert object to {type}");
    }

    //keep public so it can be picked by reflection
    public static T CastObject<T>(object input)
    {
      return (T)input;
    }

    public bool CanInsertParameter(GH_ParameterSide side, int index)
    {
      var intPtr = SelectedConstructor.GetParameters().Length;
      var canInsertParameter = side == GH_ParameterSide.Input && index >= intPtr;
      return canInsertParameter;
    }

    public bool CanRemoveParameter(GH_ParameterSide side, int index)
    {
      var intPtr = SelectedConstructor.GetParameters().Length;
      return side == GH_ParameterSide.Input && index >= intPtr;
    }

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      var myParam = new GenericAccessParam
      {
        Name = GH_ComponentParamServer.InventUniqueNickname("ABCD", Params.Input),
        MutableNickName = true,
        Optional = false
      };

      myParam.NickName = myParam.Name;
      //myParam.ObjectChanged += (sender, e) => Debouncer.Start();

      return myParam;
    }

    public bool DestroyParameter(GH_ParameterSide side, int index)
    {
      return true;
    }

    public void VariableParameterMaintenance()
    {
    }

    protected override void BeforeSolveInstance()
    {
      Converter?.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);
      base.BeforeSolveInstance();
    }
  }
}
