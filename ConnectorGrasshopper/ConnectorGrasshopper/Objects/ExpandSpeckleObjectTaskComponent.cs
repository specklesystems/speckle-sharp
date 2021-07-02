using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Objects
{
  public class ExpandSpeckleObjectTaskComponent : SelectKitTaskCapableComponentBase<Dictionary<string, object>>,
    IGH_VariableParameterComponent
  {
    public override Guid ComponentGuid => new Guid("4884856A-BCA4-43F8-B665-331F51CF4A39");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override Bitmap Icon => Properties.Resources.ExpandSpeckleObject;

    public ExpandSpeckleObjectTaskComponent() : base("Expand Speckle Object", "ESO",
      "Allows you to decompose a Speckle object in its constituent parts.",
      ComponentCategories.PRIMARY_RIBBON, ComponentCategories.OBJECTS)
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      var pObj = pManager.AddParameter(new SpeckleBaseParam("Speckle Object", "O",
        "Speckle object to deconstruct into it's properties.", GH_ParamAccess.item));
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      // INFO -> All output params are dynamically generated!
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
 
      if (InPreSolve)
      {
        GH_SpeckleBase ghSpeckleBase = null;
        if(!DA.GetData(0, ref ghSpeckleBase)) return;
        var @base = ghSpeckleBase.Value;

        var task = Task.Run(() => DoWork(@base));
        TaskList.Add(task);
        return;
      }
      if(Converter != null)
      {
        foreach (var error in Converter.ConversionErrors)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
            error.Message + ": " + error.InnerException?.Message);
        }
        Converter.ConversionErrors.Clear();
      }
      
      if (!GetSolveResults(DA, out Dictionary<string, object> result))
      {
        // Normal mode not supported
        return;
      }

      if (result != null)
      {
        foreach (var key in result.Keys)
        {
          var indexOfOutputParam = Params.IndexOfOutputParam(key);
          
          if(indexOfOutputParam != -1)
          {
            var obj = result[key];
            switch (obj)
            {
              case IList list:
                DA.SetDataList(indexOfOutputParam, list);
                break;
              default:
                DA.SetDataList(indexOfOutputParam, new List<object>{ obj });
                break;
            }
          }
        }
      }
    }

    public bool CanInsertParameter(GH_ParameterSide side, int index) => false;

    public bool CanRemoveParameter(GH_ParameterSide side, int index) => false;

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      var myParam = new GenericAccessParam
      {
        Name = GH_ComponentParamServer.InventUniqueNickname("ABCD", Params.Input),
        MutableNickName = true,
        Optional = true,
      };
      myParam.NickName = myParam.Name;
      return myParam;
    }

    public bool DestroyParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Output;

    public void VariableParameterMaintenance()
    {
      // Perform parameter maintenance here!
    }

    public List<string> outputList = new List<string>();

    private bool OutputMismatch() =>
      outputList.Count != Params.Output.Count
      || outputList.Where((t, i) => Params.Output[i].NickName != t).Any();

    private void AutoCreateOutputs()
    {
      var tokenCount = outputList?.Count ?? 0;

      if (tokenCount == 0 || !OutputMismatch()) return;
      RecordUndoEvent("Creating Outputs");

      // Check what params must be deleted, and do so when safe.
      var remove = Params.Output.Select((p, i) =>
      {
        var res = outputList.Find(o => o == p.Name);
        return res == null ? i : -1;
      }).ToList();
      remove.Reverse();
      remove.ForEach(b =>
      {
        if (b != -1 && Params.Output[b].Recipients.Count == 0)
          Params.UnregisterOutputParameter(Params.Output[b]);
      });

      outputList.Sort();
      outputList.ForEach(s =>
      {
        var param = Params.Output.Find(p => p.Name == s);
        if (param == null)
        {
          var newParam = CreateParameter(GH_ParameterSide.Output, Params.Output.Count);
          newParam.Name = s;
          newParam.NickName = s;
          newParam.Description = $"Data from property: {s}";
          newParam.MutableNickName = false;
          newParam.Access = GH_ParamAccess.list;
          Params.RegisterOutputParam(newParam);
        }
      });
      var paramNames = Params.Output.Select(p => p.Name).ToList();
      paramNames.Sort();
      var sortOrder = Params.Output.Select(p => paramNames.IndexOf(p.Name)).ToArray();
      Params.SortOutput(sortOrder);
      Params.OnParametersChanged();
      VariableParameterMaintenance();
    }

    protected override void BeforeSolveInstance()
    {
      if (RunCount == -1)
      {
        Console.WriteLine("No iter has run");
        var x = Params.Input[0].VolatileData;
        var tree = x as GH_Structure<GH_SpeckleBase>;
        outputList = GetOutputList(tree);
        AutoCreateOutputs();
      }
      base.BeforeSolveInstance();
    }

    protected override void AfterSolveInstance()
    {
      base.AfterSolveInstance();
    }

    private List<string> GetOutputList(GH_Structure<GH_SpeckleBase> speckleObjects)
    {
      // Get the full list of output parameters
      var fullProps = new List<string>();
      
      foreach (var ghGoo in speckleObjects.AllData(true))
      {
        var b = (ghGoo as GH_SpeckleBase).Value;
        b?.GetMemberNames().ToList().ForEach(prop =>
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
    
    public Dictionary<string, object> DoWork(Base @base)
    {
      try
      {
        return CreateOutputDictionary(@base);
      }
      catch (Exception e)
      {
        // If we reach this, something happened that we weren't expecting...
        Log.CaptureException(e);
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something went terribly wrong... " + e.Message);
        return null;
      }
    }
    
    private Dictionary<string, object> CreateOutputDictionary(Base @base)
    {
      // Create empty data tree placeholders for output.
      var outputDict = new Dictionary<string, object>();

      // Assign all values to it's corresponding dictionary entry and branch path.
      var obj = @base;
      foreach (var prop in obj.GetMembers())
      {
        // Convert and add to corresponding output structure
        var value = prop.Value;
        //if (!outputDict.ContainsKey(prop.Key)) continue;
        switch (value)
        {
          case null:
            continue;
          case System.Collections.IList list:
            var result = new List<IGH_Goo>();
            foreach (var x in list)
            {
              var wrapper = Utilities.TryConvertItemToNative(x, Converter);
              result.Add(wrapper);
            }
            outputDict[prop.Key] = result;

            break;
          // TODO: Not clear how we handle "tree" inner props. They can only be set by sender components,
          // so perhaps this is not an issue. Below a simple stopgap so we can actually see what data is
          // inside a sender-created object.
          case Dictionary<string, List<Base>> dict:
            foreach (var kvp in dict)
            {
              IGH_Goo wrapper = new GH_ObjectWrapper();
              foreach (var b in kvp.Value)
              {
                wrapper = Utilities.TryConvertItemToNative(b, Converter);
              }

              outputDict[prop.Key] = wrapper;
            }

            break;
          default:
            outputDict[prop.Key] = Utilities.TryConvertItemToNative(obj[prop.Key], Converter);
            break;
        }
      }
      
      return outputDict;
    }
  }
}
