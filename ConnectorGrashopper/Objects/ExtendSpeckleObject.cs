using System;
using System.Collections.Generic;
using System.Drawing;
using ConnectorGrashopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Speckle.Core.Models;

namespace ConnectorGrashopper.Objects
{
    public class ExtendSpeckleObject: SelectKitComponentBase
    {
        public override Guid ComponentGuid => new Guid("F208013C-AF46-4643-AF89-62B1A2435493");
        
        protected override Bitmap Icon => Properties.Resources.ExtendSpeckleObject;

        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        public ExtendSpeckleObject() : base("Extend Speckle Object", "ESO", "Extend a current object with key/value pairs", "Speckle 2", "Object Management")
        {
        }
        
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Speckle Object", "O", "Speckle object to deconstruct into it's properties.", GH_ParamAccess.item);
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Speckle Object", "O", "Speckle object to deconstruct into it's properties.", GH_ParamAccess.item);
            pManager.AddTextParameter("Keys", "K", "List of keys", GH_ParamAccess.list);
            pManager.AddGenericParameter("Values", "V", "List of values", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Init local variables
            GH_SpeckleBase ghBase = null;
            List<string> keys = new List<string>();
            GH_Structure<IGH_Goo> valueTree = null;
            
            // Grab data from input
            if (!DA.GetData(0, ref ghBase)) return;
            if (!DA.GetDataList(1, keys)) return;
            if (!DA.GetDataTree(2, out valueTree)) return;
            
            // TODO: Handle data validation
            
            // Assign keys and values to the base object
            // TODO: Must check if it should override
            Base b = ghBase.Value;
            keys.ForEach(key =>
            {
                // TODO: Assign real value
                b[key] = "This is a test value!";
            });

            DA.SetData(0, b);
        }
    }
}