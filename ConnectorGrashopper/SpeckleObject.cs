using System;
using System.Diagnostics;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using ConnectorGrashopper.Extras;
using Speckle.Core.Models;

namespace ConnectorGrashopper
{
  public class SpeckleObject : GH_Component, IGH_VariableParameterComponent
  {
    public override Guid ComponentGuid { get => new Guid("cfa4e9b4-3ae4-4bb9-90d8-801c34e9a37e"); }

    protected override System.Drawing.Bitmap Icon { get => null; }

    public SpeckleObject()
      : base("Create Speckle Object", "CSO",
          "Allows you to create a Speckle object from scratch.",
          "Speckle 2", "Object Management")
    {
    }

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
    }

    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Debug", "d", "debug output, please ignore", GH_ParamAccess.list);
      pManager.AddGenericParameter("Object", "O", "The created object", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      var res = new List<string>();
      var @base = new Base();

      for (int i = 0; i < Params.Input.Count; i++)
      {
        var param = Params.Input[i] as Param_GenericAccess;
        var type = param.Access.ToString();
        var detachable = param.Detachable;

        var key = detachable ? "@" + param.NickName : param.NickName;

        object wtf = null;

        switch (param.Access)
        {
          case GH_ParamAccess.item:
            object value = null;
            DA.GetData(i, ref value);
            wtf = value;
            break;

          case GH_ParamAccess.list:
            var values = new List<object>();
            DA.GetDataList(i, values);
            wtf = values;
            break;
        }

        res.Add($"{key} ({type}, detach {detachable}) {wtf?.ToString()} \n");
        @base[key] = "test";
      }

      var kits = Speckle.Core.Kits.KitManager.Kits;
      var x = kits;

      var types = Speckle.Core.Kits.KitManager.Types;

      DA.SetDataList(0, res);
      DA.SetData(1, new GH_SpeckleBase() { Value = @base });
    }

    public bool CanInsertParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Input;

    public bool CanRemoveParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Input;

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      var myParam = new Param_GenericAccess
      {
        Name = GH_ComponentParamServer.InventUniqueNickname("ABCDEFGHIJKLMNOPQRSTUVWXYZ", Params.Input),
        MutableNickName = true,
        Optional = true
      };

      myParam.NickName = myParam.Name;

      myParam.AttributesChanged += (sender, e) => { Debug.WriteLine($"Attributes Changes."); };
      myParam.ObjectChanged += (sender, e) => { Debug.WriteLine($"Object Changes."); };

      return myParam;
    }

    public bool DestroyParameter(GH_ParameterSide side, int index)
    {
      return true;
    }

    public void VariableParameterMaintenance()
    {
    }

  }
}
