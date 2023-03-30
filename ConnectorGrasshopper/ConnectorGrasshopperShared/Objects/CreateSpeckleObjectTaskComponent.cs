using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ConnectorGrasshopper.Extras;
using Grasshopper.Kernel;
using Serilog;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using Logging = Speckle.Core.Logging;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Objects
{
  public class CreateSpeckleObjectTaskComponent : SelectKitTaskCapableComponentBase<Base>,
    IGH_VariableParameterComponent
  {
    public CreateSpeckleObjectTaskComponent() : base("Create Speckle Object", "CSO",
      "Allows you to create a Speckle object by setting its keys and values.\nIn each individual parameter, you can select between 'item' and 'list' access type via the right-click menu.\n",
      ComponentCategories.PRIMARY_RIBBON, ComponentCategories.OBJECTS)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.primary;
    public override Guid ComponentGuid => new Guid("DC561A9D-BF12-4EB3-8412-4B7FC6ECB291");
    protected override Bitmap Icon => Properties.Resources.CreateSpeckleObject;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      //throw new NotImplementedException();
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleBaseParam("Speckle Object", "O", "Created speckle object", GH_ParamAccess.item));
    }

    public override void SolveInstanceWithLogContext(IGH_DataAccess DA)
    {
      if (InPreSolve)
      {
        // Process input and queue up task
        Dictionary<string, object> inputData = new Dictionary<string, object>();

        if (Params.Input.Count == 0)
          return;
        var hasErrors = false;

        var duplicateKeys = Params.Input
          .Select(p => p.NickName)
          .GroupBy(x => x).Count(group => @group.Count<string>() > 1);
        if (duplicateKeys > 0)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Cannot have duplicate keys in object.");
          return;
        }

        var allOptional = Params.Input.FindAll(p => p.Optional).Count == Params.Input.Count;
        if (Params.Input.Count > 0 && allOptional)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "You cannot set all parameters as optional");
          return;
        }

        //TODO: Original node
        if (DA.Iteration == 0)
          Tracker.TrackNodeRun("Create Object");


        Params.Input.ForEach(ighParam =>
        {
          var param = ighParam as GenericAccessParam;
          var index = Params.IndexOfInputParam(param.Name);
          var detachable = param.Detachable;
          var key = detachable ? "@" + param.NickName : param.NickName;

          switch (param.Access)
          {
            case GH_ParamAccess.item:
              object value = null;
              DA.GetData(index, ref value);
              if (!param.Optional && value == null)
              {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                  $"Non-optional parameter {param.NickName} cannot be null");
                hasErrors = true;
              }

              inputData[key] = value;
              break;
            case GH_ParamAccess.list:
              var values = new List<object>();
              DA.GetDataList(index, values);
              if (!param.Optional)
              {
                if (values.Count == 0)
                {
                  AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"Non-optional parameter {param.NickName} cannot be null or empty.");
                  hasErrors = true;
                }
              }

              inputData[key] = values;
              break;
            case GH_ParamAccess.tree:
              break;
            default:
              throw new ArgumentOutOfRangeException();
          }
        });
        if (hasErrors)
        {
          inputData = null;
          return;
        }

        var task = Task.Run(() => DoWork(inputData));
        TaskList.Add(task);
        return;
      }

      if (Converter != null)
      {
        foreach (var error in Converter.Report.ConversionErrors)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
            error.ToFormattedString());
        }
        Converter.Report.ConversionErrors.Clear();
      }

      if (!GetSolveResults(DA, out Base result))
      {
        // Not running on multi threaded, handle this properly
        return;
      }

      if (result != null)
      {
        DA.SetData(0, result);
      }
    }

    public async Task<Base> DoWork(Dictionary<string, object> inputData)
    {
      try
      {
        var @base = new Base();
        var hasErrors = false;
        if (inputData == null)
        {
          @base = null;
        }

        inputData?.Keys.ToList().ForEach(key =>
        {
          var value = inputData[key];
          if (value is SpeckleObjectGroup group) value = group.Value;
          if (value is List<object> list)
          {

            // Value is a list of items, iterate and convert.
            List<object> converted = null;
            try
            {

              converted = list.Select(item =>
              {
                var result = Converter != null ? Utilities.TryConvertItemToSpeckle(item, Converter) : item;
                return result;
              }).ToList();
            }
            catch (Exception ex)
            {
              Logging.SpeckleLog.Logger.Error(ex, ex.Message);
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"{ex.ToFormattedString()}");
              hasErrors = true;
            }

            try
            {
              @base[key] = converted;
            }
            catch (Exception ex)
            {
              Logging.SpeckleLog.Logger.Error(ex, ex.Message);
              AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{ex.ToFormattedString()}");
              hasErrors = true;
            }
          }
          else
          {
            // If value is not list, it is a single item.

            try
            {
              if (Converter != null)
                @base[key] = value == null ? null : Utilities.TryConvertItemToSpeckle(value, Converter);
              else
                @base[key] = value;
            }
            catch (Exception e)
            {
              Logging.SpeckleLog.Logger.Error(e, e.Message);
              AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"{e.ToFormattedString()}");
              hasErrors = true;
            }
          }
        });

        if (hasErrors)
        {
          @base = null;
        }

        return @base;
      }
      catch (Exception e)
      {
        // If we reach this, something happened that we weren't expecting...
        Logging.SpeckleLog.Logger.Error(e, e.Message);
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something went terribly wrong... " + e.ToFormattedString());
      }

      return new Base();
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
      myParam.Optional = false;
      myParam.ObjectChanged += (sender, e) => { };
      myParam.Attributes = new GenericAccessParamAttributes(myParam, Attributes);
      return myParam;
    }

    public bool DestroyParameter(GH_ParameterSide side, int index) => side == GH_ParameterSide.Input;

    public void VariableParameterMaintenance()
    {
      Params.Input
        .Where(param => !(param.Attributes is GenericAccessParamAttributes))
        .ToList()
        .ForEach(param => param.Attributes = new GenericAccessParamAttributes(param, Attributes)
      );
    }

    private DebounceDispatcher nicknameChangeDebounce = new DebounceDispatcher();

    public override void AddedToDocument(GH_Document document)
    {
      base.AddedToDocument(document); // This would set the converter already.
      Params.ParameterChanged += (sender, args) =>
      {
        if (args.ParameterSide != GH_ParameterSide.Input) return;
        switch (args.OriginalArguments.Type)
        {
          case GH_ObjectEventType.NickName:
            // This means the user is typing characters, debounce until it stops for 400ms before expiring the solution.
            // Prevents UI from locking too soon while writing new names for inputs.
            args.Parameter.Name = args.Parameter.NickName;
            nicknameChangeDebounce.Debounce(400, (e) => ExpireSolution(true));
            break;
          case GH_ObjectEventType.NickNameAccepted:
            args.Parameter.Name = args.Parameter.NickName;
            ExpireSolution(true);
            break;
        }
      };
    }
  }
}
