using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ConnectorGrashopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace ConnectorGrashopper.Objects
{
    public class CreateSpeckleObjectByKeyValue: GH_Component
    {
        private ISpeckleConverter Converter;

        private ISpeckleKit Kit;

        public CreateSpeckleObjectByKeyValue() : base("Create object by key/value", "K/V", "Create an Speckle object by key/value pairs", "Speckle 2", "Objects")
        {
            Kit = KitManager.GetDefaultKit();
            try
            {
                Converter = Kit.LoadConverter(Applications.Rhino);
                Message = $"Using the \n{Kit.Name}\n Kit Converter";
            }
            catch
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
            }
        }
        
        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, "Select the converter you want to use:");

            var kits = KitManager.GetKitsWithConvertersForApp(Applications.Rhino);

            foreach (var kit in kits)
            {
                Menu_AppendItem(menu, $"{kit.Name} ({kit.Description})", (s, e) => { SetConverterFromKit(kit.Name); }, true, kit.Name == Kit.Name);
            }

            Menu_AppendSeparator(menu);
        }
        
        private object TryConvertItem(object value)
        {
            if (value is IGH_Goo)
            {
                value = value.GetType().GetProperty("Value")?.GetValue(value);
            }
            if (value is Base @base && Converter.CanConvertToNative(@base))
            {
                return Converter.ConvertToNative(@base);
            }
            if (value.GetType().IsSimpleType())
            {
                return value;
            }
            return null;
        }
        
        private void SetConverterFromKit(string kitName)
        {
            if (kitName == Kit.Name) return;

            Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
            Converter = Kit.LoadConverter(Applications.Rhino);

            Message = $"Using the {Kit.Name} Converter";
            ExpireSolution(true);
        }
        public override Guid ComponentGuid => new Guid("2AF52F83-7269-410D-B0BD-9CCA8C556B9F");
        
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Keys", "K", "List of keys", GH_ParamAccess.list);
            pManager.AddGenericParameter("Values", "V", "List of values", GH_ParamAccess.tree);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Object", "O", "Speckle object", GH_ParamAccess.item);
            pManager.AddGenericParameter("debug", "d", "debug Speckle object", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Initialize local variables
            var valueTree = new GH_Structure<IGH_Goo>();
            var keys = new List<string>();

            // Get data from inputs
            if (!DA.GetDataList(0, keys)) return;
            if (!DA.GetDataTree(1, out valueTree)) return;
            
            // Create a path from the current iteration
            var searchPath = new GH_Path(DA.Iteration);
            
            // Grab the corresponding subtree from the value input tree.
            var subTree = GetSubTree(valueTree, searchPath);
            Base speckleObj = new Base();
            // Find the list or subtree belonging to that path
            if (valueTree.PathExists(searchPath) || valueTree.Paths.Count == 1)
            {
                IList list = valueTree.Paths.Count == 1 ? valueTree.Branches[0] : valueTree.get_Branch(searchPath);
                // We got a list of values
                var ind = 0;
                keys.ForEach(key =>
                {
                    if(ind < list.Count)
                        speckleObj[key] = list[ind];
                    ind++;
                });
            }
            else
            {
                // We got a tree of values
            
                // Create the speckle object with the specified keys
                var index = 0;
                keys.ForEach(key =>
                {
                    var itemPath = new GH_Path(index);
                    //TODO: Grab conversion methods and implement branch handling.
                    var branch = subTree.get_Branch(itemPath);
                    if(branch != null)
                        speckleObj[key] = branch;
                    index++;
                });
            }
            
            
            // Set output
            DA.SetData(0,new GH_SpeckleBase{Value = speckleObj});
            DA.SetDataTree(1, subTree);
        }

        private static GH_Structure<IGH_Goo> GetSubTree(GH_Structure<IGH_Goo> valueTree, GH_Path searchPath)
        {
            var subTree = new GH_Structure<IGH_Goo>();
            var gen = 0;
            foreach (var path in valueTree.Paths)
            {
                var branch = valueTree.get_Branch(path) as IEnumerable<IGH_Goo>;
                if (path.IsAncestor(searchPath, ref gen))
                {
                    subTree.AppendRange(branch, path);
                }
                else if (path.IsCoincident(searchPath))
                {
                    subTree.AppendRange(branch,path);
                    break;
                }
            }

            subTree.Simplify(GH_SimplificationMode.CollapseLeadingOverlaps);
            return subTree;
        }
    }
}