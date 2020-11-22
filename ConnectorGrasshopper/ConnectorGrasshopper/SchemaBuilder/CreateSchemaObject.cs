using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;
using ConnectorGrasshopper.Extras;
using Speckle.Core.Models;
using GH_IO.Serialization;
using Speckle.Core.Kits;
using System.Windows.Forms;
using System.Linq;
using ConnectorGrasshopper.Objects;
using Utilities = ConnectorGrasshopper.Extras.Utilities;
using Speckle.Core.Logging;
using Grasshopper.GUI.Canvas;
using System.Reflection;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Speckle.Core.Api;

namespace ConnectorGrasshopper
{


  public class CreateSchemaObject : SelectKitComponentBase, IGH_VariableParameterComponent
  {
    private Type SelectedType;
    private GH_Document _document;

    public override Guid ComponentGuid => new Guid("4dc285e3-810d-47db-bfb5-cd96fe459fdd");
    protected override Bitmap Icon => Properties.Resources.CreateSpeckleObject;

    public override GH_Exposure Exposure => GH_Exposure.primary;

    private System.Timers.Timer Debouncer;

    public CreateSchemaObject()
      : base("Create Schema Object", "CsO",
          "Allows you to create a Speckle object from a schema class.",
          "Speckle 2", "Object Management")
    {
      Kit = KitManager.GetDefaultKit();
      try
      {
        Converter = Kit.LoadConverter(Applications.Rhino);
        Message = $"{Kit.Name} Kit";
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
      }
    }

    public override void AddedToDocument(GH_Document document)
    {
      if (SelectedType != null)
      {
        base.AddedToDocument(document);
        return;
      }
      //base.AddedToDocument(document);
      _document = document;

      var dialog = new CreateSchemaObjectDialog();
      dialog.Owner = Grasshopper.Instances.EtoDocumentEditor;
      var mouse = GH_Canvas.MousePosition;
      dialog.Location = new Eto.Drawing.Point(mouse.X - 200, mouse.Y - 200); //approx the dialog half-size

      dialog.ShowModal();

      if (dialog.HasResult)
      {
        base.AddedToDocument(document);
        SwitchToType(dialog.model.SelectedItem.Tag as Type);
      }
      else
      {
        document.RemoveObject(this.Attributes, true);
      }
    }

    public void SwitchToType(Type type)
    {
      int k = 0;
      var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(x => x.GetCustomAttribute<SchemaIgnoreAttribute>() == null && x.Name != "Item");

      //put optional props at the bottom
      var optionalProps = props.Where(x => x.GetCustomAttribute<SchemaOptionalAttribute>() != null).OrderBy(x => x.PropertyType.ToString()).ThenBy(x => x.Name);
      var nonOptionalProps = props.Where(x => x.GetCustomAttribute<SchemaOptionalAttribute>() == null).OrderBy(x => x.PropertyType.ToString()).ThenBy(x => x.Name);
      props = nonOptionalProps;
      props = props.Concat(optionalProps);

      foreach (var p in props)
      {
        RegisterPropertyAsInputParameter(p, k++);
      }

      Message = type.Name;
      SelectedType = type;
      Params.Output[0].NickName = type.Name;
      Params.OnParametersChanged();
      ExpireSolution(true);
    }

    /// <summary>
    /// Adds a property to the component's inputs.
    /// </summary>
    /// <param name="prop"></param>
    void RegisterPropertyAsInputParameter(PropertyInfo prop, int index)
    {
      // get property name and value
      Type propType = prop.PropertyType;

      string propName = prop.Name;
      object propValue = prop;

      var inputDesc = prop.GetCustomAttribute<SchemaDescriptionAttribute>();
      var d = inputDesc != null ? inputDesc.Description : "";

      // Create new param based on property name
      Param_GenericObject newInputParam = new Param_GenericObject();
      newInputParam.Name = propName;
      newInputParam.NickName = propName;
      newInputParam.MutableNickName = false;

      newInputParam.Description = $"({propType.Name}) {d}";
      newInputParam.Optional = prop.GetCustomAttribute<SchemaOptionalAttribute>() != null;

      // check if input needs to be a list or item access
      bool isCollection = typeof(System.Collections.IEnumerable).IsAssignableFrom(propType) && propType != typeof(string) && !propType.Name.ToLower().Contains("dictionary");
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
        _document.AddObject(options, false);
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

    public override bool Read(GH_IReader reader)
    {
      try
      {
        SelectedType = ByteArrayToObject<Type>(reader.GetByteArray("SelectedType"));
      }
      catch { }

      return base.Read(reader);
    }

    public override bool Write(GH_IWriter writer)
    {
      if (SelectedType != null)
      {
        writer.SetByteArray("SelectedType", ObjectToByteArray(SelectedType));
      }

      return base.Write(writer);
    }

    private static byte[] ObjectToByteArray(object obj)
    {
      BinaryFormatter bf = new BinaryFormatter();
      using (var ms = new MemoryStream())
      {
        bf.Serialize(ms, obj);
        return ms.ToArray();
      }
    }

    private static T ByteArrayToObject<T>(byte[] arrBytes)
    {
      using (var memStream = new MemoryStream())
      {
        var binForm = new BinaryFormatter();
        memStream.Write(arrBytes, 0, arrBytes.Length);
        memStream.Seek(0, SeekOrigin.Begin);
        var obj = binForm.Deserialize(memStream);
        return (T)obj;
      }
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      //pManager.AddGenericParameter("Debug", "d", "debug output, please ignore", GH_ParamAccess.list);
      pManager.AddParameter(new SpeckleBaseParam("Speckle Object", "O", "Created speckle object", GH_ParamAccess.item));
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (SelectedType is null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No schema has been selected.");
        return;
      }

      var g = Rhino.RhinoDoc.ActiveDoc.ModelUnitSystem;

      var outputObject = Activator.CreateInstance(SelectedType);

      for (int i = 0; i < Params.Input.Count; i++)
      {
        var param = Params.Input[i];
        if (param.Access == GH_ParamAccess.list)
        {
          var inputValues = new List<object>();
          DA.GetDataList(i, inputValues);
          inputValues = inputValues.Select(x => ExtractRealInputValue(x)).ToList();
          SetObjectListProp(param, outputObject, inputValues);
        }
        else if (param.Access == GH_ParamAccess.item)
        {
          object inputValue = null;
          DA.GetData(i, ref inputValue);
          SetObjectProp(param, outputObject, ExtractRealInputValue(inputValue));
        }

      }

      DA.SetData(0, new GH_SpeckleBase() { Value = outputObject as Base });
    }



    private object ExtractRealInputValue(object inputValue)
    {
      if (inputValue == null)
        return null;

      object realInputValue;
      try
      {
        realInputValue = inputValue.GetType().GetProperty("Value").GetValue(inputValue);
      }
      catch
      {
        realInputValue = inputValue;
      }
      return realInputValue;
    }

    //list input
    private void SetObjectListProp(IGH_Param param, object @object, List<object> values)
    {
      if (!values.Any()) return;

      PropertyInfo prop = @object.GetType().GetProperty(param.Name);
      var list = (IList)Activator.CreateInstance(prop.PropertyType);
      var listElementType = list.GetType().GetGenericArguments().Single();
      foreach (var value in values)
      {
        list.Add(ConvertType(listElementType, value, param.Name));
      }

      prop.SetValue(@object, list, null);
    }

    private void SetObjectProp(IGH_Param param, object @object, object value)
    {
      PropertyInfo prop = @object.GetType().GetProperty(param.Name);
      var convertedValue = ConvertType(prop.PropertyType, value, param.Name);
      prop.SetValue(@object, convertedValue);
    }

    private object ConvertType(Type type, object value, string name)
    {
      if (value == null || value.GetType() == type || type.IsAssignableFrom(value.GetType()))
        return value;
      try
      {
        return Convert.ChangeType(value, type);
      }
      catch { }
      try
      {
        return Enum.Parse(type, value.ToString());
      }
      catch { }
      try
      {
        if (Converter.CanConvertToSpeckle(value))
        {
          return Converter.ConvertToSpeckle(value);
        }
      }
      catch { }

      try
      {
        var deserialised = Operations.Deserialize((string)value);
        if (Converter.CanConvertToSpeckle(deserialised))
        {
          return Converter.ConvertToSpeckle(deserialised);
        }
      }
      catch { }

      this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to set " + name + ".");
      throw new Exception($"Could not covert object to {type}");

    }



    public bool CanInsertParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Input;

    public bool CanRemoveParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Input;

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      var myParam = new GenericAccessParam
      {
        Name = GH_ComponentParamServer.InventUniqueNickname("ABCD", Params.Input),
        MutableNickName = true,
        Optional = true
      };

      myParam.NickName = myParam.Name;
      myParam.ObjectChanged += (sender, e) => Debouncer.Start();

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
      Tracker.TrackPageview("objects", "create", "variableinput");
      base.BeforeSolveInstance();
    }

  }

}
