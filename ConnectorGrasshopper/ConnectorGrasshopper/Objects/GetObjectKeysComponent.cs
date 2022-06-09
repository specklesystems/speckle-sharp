using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using Speckle.Core.Models;

namespace ConnectorGrasshopper.Objects
{
  public class GetObjectKeysComponent : GH_Component
  {

    public GetObjectKeysComponent()
      : base("Speckle Object Keys", "SOK",
          "Get a list of keys available in a speckle object",
           ComponentCategories.PRIMARY_RIBBON, ComponentCategories.OBJECTS)
    {
    }

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Speckle Object", "O", "Speckle object to deconstruct into it's properties.", GH_ParamAccess.item));
    }

    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Keys", "K", "The keys available on this speckle object", GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (perObject)
      {
        GH_SpeckleBase speckleObject = null;
        if (!DA.GetData(0, ref speckleObject)) return;

        if (speckleObject.Value == null)
          return;

        var keys = speckleObject.Value.GetMemberNames();

        DA.SetDataList(0, keys);
      }
      else
      {
        if (!DA.GetDataTree(0, out GH_Structure<GH_SpeckleBase> objectTree)) return;

        var keys = new List<string>();

        foreach (var branch in objectTree.Branches)
          foreach (var item in branch)
          {
            var speckleObject = item?.Value;
            if (speckleObject == null)
              return;

            var objKeys = speckleObject.GetMemberNames();
            foreach (var key in objKeys)
              if (!keys.Contains(key))
                keys.Add(key); 
          }


        DA.SetDataList(0, keys);
      }
    }

    private bool perObject = true;

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendSeparator(menu);

      var item = Menu_AppendItem(menu, "Per Object", (o, e) => {
        perObject = !perObject;
        Message = perObject ? "Per Object" : "All objects";
        var input = Params.Input[0];
        input.Access = perObject ? GH_ParamAccess.item : GH_ParamAccess.tree;
        input.OnObjectChanged(GH_ObjectEventType.DataMapping);
        input.ExpireSolution(true);
      }, true, perObject);
      Menu_AppendSeparator(menu);

      base.AppendAdditionalMenuItems(menu);

    }

    public override void AddedToDocument(GH_Document document)
    {
      Message = perObject ? "Per Object" : "All objects";
      base.AddedToDocument(document);
    }

    protected override System.Drawing.Bitmap Icon => Properties.Resources.SpeckleObjectKeysLogo;
    
    public override GH_Exposure Exposure => GH_Exposure.primary;

    public override Guid ComponentGuid => new Guid("16E28D2D-EA9F-4F59-96CA-045A32EA130C");
  }
}
