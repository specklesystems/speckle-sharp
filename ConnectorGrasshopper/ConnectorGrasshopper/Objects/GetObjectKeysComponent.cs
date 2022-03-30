using System;
using System.Collections.Generic;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Speckle.Core.Models;

namespace ConnectorGrasshopper.Objects
{
  public class GetObjectKeysComponent : GH_Component
  {
    /// <summary>
    /// Initializes a new instance of the MyComponent1 class.
    /// </summary>
    public GetObjectKeysComponent()
      : base("Speckle - Get Object Keys", "SGOK",
          "Get a list of keys available in a speckle object",
           ComponentCategories.PRIMARY_RIBBON, ComponentCategories.OBJECTS)
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Speckle Object", "O", "Speckle object to deconstruct into it's properties.", GH_ParamAccess.item));
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Keys", "K", "The keys available on this speckle object", GH_ParamAccess.list);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      Base speckleObject = null;
      if (!DA.GetData(0, ref speckleObject)) return;

      if (speckleObject == null)
      {
        return;
      }

      var keys = speckleObject.GetMemberNames();

      DA.SetDataList(0, keys);

    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon => null;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid => new Guid("16E28D2D-EA9F-4F59-96CA-045A32EA130C");
  }
}
