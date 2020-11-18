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

    public void SwitchToType(Type myType)
    {
      int k = 0;
      foreach (var p in myType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
        .Where(x => x.Name != "Type" && x.Name != "Item" && x.GetCustomAttribute(typeof(SchemaIgnoreAttribute)) == null))
      {
        RegisterPropertyAsInputParameter(p, k++);
      }

      Message = myType.Name;
      SelectedType = myType;
      Params.Output[0].NickName = myType.Name;
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

      // Create new param based on property name
      Param_GenericObject newInputParam = new Param_GenericObject();
      newInputParam.Name = propName;
      newInputParam.NickName = propName;
      newInputParam.MutableNickName = false;
      newInputParam.Description = propName + " as " + propType.Name;
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

      var outputObject = Activator.CreateInstance(SelectedType);

      for (int i = 0; i < Params.Input.Count; i++)
      {
        if (Params.Input[i].Access == GH_ParamAccess.list)
        {
          var ObjectsList = new List<object>();
          DA.GetDataList(i, ObjectsList);

          if (ObjectsList.Count == 0) continue;

          var listForSetting = (IList)Activator.CreateInstance(outputObject.GetType().GetProperty(Params.Input[i].Name).PropertyType);
          foreach (var item in ObjectsList)
          {
            object innerVal = null;
            try
            {
              innerVal = item.GetType().GetProperty("Value").GetValue(item);
            }
            catch
            {
              innerVal = item;
            }

            listForSetting.Add(innerVal);
          }

          outputObject.GetType().GetProperty(Params.Input[i].Name).SetValue(outputObject, listForSetting, null);
        }
        else if (Params.Input[i].Access == GH_ParamAccess.item)
        {
          object ghInput = null; // INPUT OBJECT ( PROPERTY )
          DA.GetData(i, ref ghInput);

          if (ghInput == null) continue;

          object innerValue = null;
          try
          {
            innerValue = ghInput.GetType().GetProperty("Value").GetValue(ghInput);
          }
          catch
          {
            innerValue = ghInput;
          }

          if (innerValue == null) continue;

          PropertyInfo prop = outputObject.GetType().GetProperty(Params.Input[i].Name);
          if (prop.PropertyType.IsEnum)
          {
            try
            {
              prop.SetValue(outputObject, Enum.Parse(prop.PropertyType, (string)innerValue));
              continue;
            }
            catch { }

            try
            {
              prop.SetValue(outputObject, (int)innerValue);
              continue;
            }
            catch { }

            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to set " + Params.Input[i].Name + ".");
          }

          else if (innerValue.GetType() != prop.PropertyType)
          {
            try
            {
              if (Converter.CanConvertToSpeckle(innerValue))
              {
                var conv = Converter.ConvertToSpeckle(innerValue);
                prop.SetValue(outputObject, conv);
              }
              continue;
            }
            catch { }

            try
            {
              prop.SetValue(outputObject, innerValue);
              continue;
            }
            catch { }

            try
            {
              var deserialised = Operations.Deserialize((string)innerValue);
              if (Converter.CanConvertToSpeckle(deserialised))
              {
                var conv = Converter.ConvertToSpeckle(deserialised);
                prop.SetValue(outputObject, conv);
                continue;
              }
            }
            catch { }

            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unable to set " + Params.Input[i].Name + ".");
          }

          else
          {
            prop.SetValue(outputObject, innerValue);
          }
        }
      }

      DA.SetData(0, new GH_SpeckleBase() { Value = outputObject as Base });
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
