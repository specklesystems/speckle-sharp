using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ConnectorGrasshopper.Extras;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Objects
{
  // TODO: Convert to task capable component / async so as to not block the ffffing ui
  public class ExpandSpeckleObject : SelectKitComponentBase, IGH_VariableParameterComponent
  {
    protected override Bitmap Icon => Properties.Resources.ExpandSpeckleObject;
    public override bool Obsolete => true;

    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override Guid ComponentGuid => new Guid("C3BC3130-97C9-4DDE-9D4F-7A7FB82F7F2E");

    public ExpandSpeckleObject()
      : base("Expand Speckle Object", "ESO",
          "Allows you to decompose a Speckle object in its constituent parts.\nKeys will be sorted in alphabetical order.\nIf a key is removed from the object, it's output will be automatically deleted; unless the output had existing connections.",
          ComponentCategories.PRIMARY_RIBBON, ComponentCategories.OBJECTS)
    {
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
      pManager.AddParameter(new SpeckleBaseParam("Speckle Object", "O", "Speckle object to deconstruct into it's properties.", GH_ParamAccess.tree));
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      // All output params are dynamically generated!
    }

    protected override void BeforeSolveInstance()
    {
      Converter.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);
      Tracker.TrackPageview("objects", "expand", "variableoutput");

      if (speckleObjects != null && hasSetData)
        AutoCreateOutputs();
      base.BeforeSolveInstance();
    }

    private bool hasSetData;
    private GH_Structure<GH_SpeckleBase> speckleObjects;
    private List<string> outputList = new List<string>();

    private List<string> GetOutputList()
    {
      // Get the full list of output parameters
      var fullProps = new List<string>();
      foreach (var path in speckleObjects.Paths)
      {
        if (speckleObjects.get_Branch(path).Count == 0) continue;
        var obj = speckleObjects.get_DataItem(path, 0);
        var b = (obj as GH_SpeckleBase)?.Value;
        var props = b?.GetMemberNames().ToList();
        props?.ForEach(prop =>
        {
          if (!fullProps.Contains(prop) && b[prop] != null)
            fullProps.Add(prop);
          else if (fullProps.Contains(prop) && b[prop] == null)
            fullProps.Remove(prop);
        });
      }
      fullProps.Sort();
      return fullProps;
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (!hasSetData)
      {
        // First run: Save the tree and expire solution to force an update in the output params.

        // Get the data or abort.
        if (!DA.GetDataTree(0, out speckleObjects)) return;

        if (!speckleObjects.Any())
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The provided input has no data.");
          return;
        }
        // Ensure only one object per path.
        speckleObjects.Graft(GH_GraftMode.GraftAll);

        // Update the output list
        outputList = GetOutputList();

        // Once data has been set, expire solution to update output params.
        hasSetData = true;
        ExpireSolution(true);
      }
      else
      {
        // Second run: Parameter output should have been updated in `beforeSolveInstance` with latest state.

        // Build the output dictionary
        var outputDict = CreateOutputDictionary();

        // Assign outputs.
        foreach (var key in outputDict.Keys)
          DA.SetDataTree(Params.IndexOfOutputParam(key), outputDict[key]);

        // Reset state
        hasSetData = false;
        speckleObjects = null;
      }
    }

    private Dictionary<string, GH_Structure<GH_ObjectWrapper>> CreateOutputDictionary()
    {
      // Create empty data tree placeholders for output.
      var outputDict = outputList.ToDictionary(outParam => outParam, _ => new GH_Structure<GH_ObjectWrapper>());

      // Assign all values to it's corresponding dictionary entry and branch path.
      foreach (var path in speckleObjects.Paths)
      {
        if (speckleObjects.get_Branch(path).Count == 0) continue;
        // Loop through all dynamic properties
        var baseGoo = speckleObjects.get_DataItem(path, 0) as GH_SpeckleBase;
        if (baseGoo == null)
        {
          continue;
        }
        var obj = baseGoo.Value;
        foreach (var prop in obj.GetMembers())
        {
          // Convert and add to corresponding output structure
          var value = prop.Value;
          if (!outputDict.ContainsKey(prop.Key)) continue;
          switch (value)
          {
            case null:
              continue;
            case System.Collections.IList list:
              var index = 0;
              foreach (var x in list)
              {
                var wrapper = new GH_ObjectWrapper();
                wrapper.Value = Utilities.TryConvertItemToNative(x, Converter);
                outputDict[prop.Key].Append(wrapper, path);
                index++;
              }
              break;
            // TODO: Not clear how we handle "tree" inner props. They can only be set by sender components,
            // so perhaps this is not an issue. Below a simple stopgap so we can actually see what data is
            // inside a sender-created object.
            case Dictionary<string, List<Base>> dict:
              foreach (var kvp in dict)
              {
                var wrapper = new GH_ObjectWrapper();
                foreach (var b in kvp.Value)
                {
                  wrapper.Value = Utilities.TryConvertItemToNative(b, Converter);
                }
                outputDict[prop.Key].Append(wrapper, path);
              }
              break;
            default:
              outputDict[prop.Key].Append(
                new GH_ObjectWrapper(Utilities.TryConvertItemToNative(obj[prop.Key], Converter)),
                path);
              break;
          }
        }
      }

      return outputDict;
    }

    public bool CanInsertParameter(GH_ParameterSide side, int index) => false;

    public bool CanRemoveParameter(GH_ParameterSide side, int index) => false;

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      var myParam = new GenericAccessParam
      {
        Name = GH_ComponentParamServer.InventUniqueNickname("ABCD", Params.Input),
        MutableNickName = true,
        Optional = true
      };

      myParam.NickName = myParam.Name;
      myParam.ObjectChanged += (sender, e) => { };

      return myParam;
    }

    public bool DestroyParameter(GH_ParameterSide side, int index)
    {
      return true;
    }

    public void VariableParameterMaintenance()
    {
      if (outputList.Count == 0) return;

      for (var i = 0; i < Params.Output.Count; i++)
      {
        string name;
        string description;
        if (i < outputList.Count)
        {
          name = outputList[i];
          description = $"Data from property: {name}";
        }
        else
        {
          name = "-";
          description = "Data output no longer exists";
        }

        Params.Output[i].Name = name;
        Params.Output[i].NickName = name;
        Params.Output[i].Description = description;
        Params.Output[i].MutableNickName = false;
        Params.Output[i].Access = GH_ParamAccess.tree;
      }
    }

    private bool OutputMismatch()
    {
      bool countMatch = outputList.Count == Params.Output.Count;
      if (!countMatch)
        return true;

      for (var i = 0; i < outputList.Count; i++)
        if (Params.Output[i].NickName != outputList[i])
          return true;

      return false;
    }

    private void AutoCreateOutputs()
    {
      int tokenCount = outputList.Count;
      if (tokenCount == 0 || !OutputMismatch()) return;
      RecordUndoEvent("Creating Outputs");

      if (Params.Output.Count < tokenCount)
        while (Params.Output.Count < tokenCount)
        {
          var newParam = CreateParameter(GH_ParameterSide.Output, Params.Output.Count);
          Params.RegisterOutputParam(newParam);
        }
      else if (Params.Output.Count > tokenCount)
        while (Params.Output.Count > tokenCount)
        {
          var ghParam = Params.Output[Params.Output.Count - 1];
          if (ghParam.Recipients.Count == 0)
          {
            // No output recipients, param is safe to delete.
            Params.UnregisterOutputParameter(ghParam);
          }
          else
          {
            // Param has recipients so it should be kept.
            tokenCount++; // Add +1 to the tokenCount to account for non-deleted param.
          }
        }

      Params.OnParametersChanged();
      VariableParameterMaintenance();
    }

  }
}
