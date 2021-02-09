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

namespace ConnectorGrasshopper
{
  // TODO: Convert to task capable component / async so as to not block the ffffing ui
  public class CreateSpeckleObject : SelectKitComponentBase, IGH_VariableParameterComponent
  {
    public override Guid ComponentGuid => new Guid("cfa4e9b4-3ae4-4bb9-90d8-801c34e9a37e");
    protected override Bitmap Icon => Properties.Resources.CreateSpeckleObject;
    public override bool Obsolete => true;
    public override GH_Exposure Exposure => GH_Exposure.hidden;

    private System.Timers.Timer Debouncer;

    public CreateSpeckleObject()
      : base("Create Speckle Object", "CSO",
          "Allows you to create a Speckle object by setting its keys and values.\nIn each individual parameter, you can select between 'item' and 'list' access type via the right-click menu.\n",
          ComponentCategories.PRIMARY_RIBBON, ComponentCategories.OBJECTS)
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
      base.AddedToDocument(document);
      Debouncer = new System.Timers.Timer(2000) { AutoReset = false };
      Debouncer.Elapsed += (s, e) => Rhino.RhinoApp.InvokeOnUiThread((Action)delegate { this.ExpireSolution(true); });

      foreach (var param in Params.Input)
        param.ObjectChanged += (s, e) => Debouncer.Start();
    }

    public override bool Read(GH_IReader reader)
    {
      // TODO: Read kit name and instantiate converter
      return base.Read(reader);
    }

    public override bool Write(GH_IWriter writer)
    {
      // TODO: Write kit name to disk
      return base.Write(writer);
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

      var res = new List<string>();
      var @base = new Base();

      for (int i = 0; i < Params.Input.Count; i++)
      {
        var param = Params.Input[i] as GenericAccessParam;
        var type = param.Access.ToString();
        var detachable = param.Detachable;

        var key = detachable ? "@" + param.NickName : param.NickName;

        object result = null;

        switch (param.Access)
        {
          case GH_ParamAccess.item:
            object value = null;
            DA.GetData(i, ref value);

            if (value == null) break;

            result = Utilities.TryConvertItemToSpeckle(value, Converter);

            if (result == null)
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Data of type {value.GetType().Name} in {param.NickName} could not be converted.");
            break;

          case GH_ParamAccess.list:
            var myList = new List<object>();
            var values = new List<object>();
            var j = 0;
            DA.GetDataList(i, values);

            if (values == null) break;

            foreach (var item in values)
            {
              var conv = Utilities.TryConvertItemToSpeckle(item, Converter);
              myList.Add(conv);
              if (conv == null)
              {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Data of type {item.GetType().Name} in {param.NickName} at index {j} could not be converted.");
              }
              j++;
            }

            result = myList;
            break;
        }

        res.Add($"{key} ({type}, detach {detachable}) {result?.ToString()} \n");

        if (result != null)
          @base[key] = result;
      }

      DA.SetData(0, new GH_SpeckleBase() { Value = @base });
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
      Converter.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);
      Tracker.TrackPageview("objects", "create", "variableinput");
      base.BeforeSolveInstance();
    }

  }

}
