using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConnectorGrasshopper.Properties;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Logging = Speckle.Core.Logging;

namespace ConnectorGrasshopper.Ops
{
  public class SendComponentSync : GH_TaskCapableComponent<List<StreamWrapper>>
  {
    CancellationTokenSource source;
    const int delay = 100000;
    public ISpeckleConverter Converter;
    public ISpeckleKit Kit;
    public List<StreamWrapper> OutputWrappers { get; private set; } = new List<StreamWrapper>();
    public bool UseDefaultCache { get; set; } = true;
    private GH_Structure<IGH_Goo> dataInput;
    private List<object> converted;
    public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;

    /// <summary>
    /// Initializes a new instance of the SendComponentSync class.
    /// </summary>
    public SendComponentSync()
      : base("Synchronous Sender", "SS",
          "Send data to a Speckle server Synchronously. This will block GH untill all the data are received which can be used to safely trigger other processes downstream",
          ComponentCategories.SECONDARY_RIBBON, ComponentCategories.SEND_RECEIVE)
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Data", "D", "The data to send.",
  GH_ParamAccess.tree);
      pManager.AddGenericParameter("Stream", "S", "Stream(s) and/or transports to send to.", GH_ParamAccess.item);
      pManager.AddTextParameter("Message", "M", "Commit message. If left blank, one will be generated for you.",
        GH_ParamAccess.item, "");

      Params.Input[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Stream", "S",
        "Stream or streams pointing to the created commit", GH_ParamAccess.list);
    }

    public void SetConverterFromKit(string kitName)
    {
      if (Kit == null) return;
      if (kitName == Kit.Name)
      {
        return;
      }

      Kit = KitManager.Kits.FirstOrDefault(k => k.Name == kitName);
      Converter = Kit.LoadConverter(Extras.Utilities.GetVersionedAppName());
      Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);
      SpeckleGHSettings.OnMeshSettingsChanged +=
        (sender, args) => Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);

      Message = $"Using the {Kit.Name} Converter";
      ExpireSolution(true);
    }

    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendSeparator(menu);
      var menuItem = Menu_AppendItem(menu, "Select the converter you want to use:", null, false);
      menuItem.Enabled = false;
      var kits = KitManager.GetKitsWithConvertersForApp(Extras.Utilities.GetVersionedAppName());

      foreach (var kit in kits)
      {
        Menu_AppendItem(menu, $"{kit.Name} ({kit.Description})", (s, e) => { SetConverterFromKit(kit.Name); }, true,
          kit.Name == Kit.Name);
      }

      Menu_AppendSeparator(menu);

      if (OutputWrappers != null)
        if (OutputWrappers.Count != 0)
        {
          Menu_AppendSeparator(menu);
          foreach (var ow in OutputWrappers)
          {
            Menu_AppendItem(menu, $"View commit {ow.CommitId} @ {ow.ServerUrl} online ↗",
              (s, e) => System.Diagnostics.Process.Start($"{ow.ServerUrl}/streams/{ow.StreamId}/commits/{ow.CommitId}"));
          }
        }
      Menu_AppendSeparator(menu);


      base.AppendAdditionalComponentMenuItems(menu);
    }

    public override bool Write(GH_IWriter writer)
    {
      writer.SetBoolean("UseDefaultCache", UseDefaultCache);
      writer.SetString("KitName", Kit.Name);
      var owSer = string.Join("\n", OutputWrappers.Select(ow => $"{ow.ServerUrl}\t{ow.StreamId}\t{ow.CommitId}"));
      writer.SetString("OutputWrappers", owSer);
      return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
      UseDefaultCache = reader.GetBoolean("UseDefaultCache");

      var wrappersRaw = reader.GetString("OutputWrappers");
      var wrapperLines = wrappersRaw.Split('\n');
      if (wrapperLines != null && wrapperLines.Length != 0 && wrappersRaw != "")
      {
        foreach (var line in wrapperLines)
        {
          var pieces = line.Split('\t');
          OutputWrappers.Add(new StreamWrapper { ServerUrl = pieces[0], StreamId = pieces[1], CommitId = pieces[2] });
        }
      }

      var kitName = "";
      reader.TryGetString("KitName", ref kitName);

      if (kitName != "")
      {
        try
        {
          SetConverterFromKit(kitName);
        }
        catch (Exception)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
            $"Could not find the {kitName} kit on this machine. Do you have it installed? \n Will fallback to the default one.");
          SetDefaultKitAndConverter();
        }
      }
      else
      {
        SetDefaultKitAndConverter();
      }

      return base.Read(reader);
    }

    public override void AddedToDocument(GH_Document document)
    {
      SetDefaultKitAndConverter();
      base.AddedToDocument(document);
    }

    private bool foundKit;
    private void SetDefaultKitAndConverter()
    {
      try
      {
        Kit = KitManager.GetDefaultKit();
        Converter = Kit.LoadConverter(Extras.Utilities.GetVersionedAppName());
        Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);
        SpeckleGHSettings.OnMeshSettingsChanged +=
          (sender, args) => Converter.SetConverterSettings(SpeckleGHSettings.MeshSettings);
        Converter.SetContextDocument(Rhino.RhinoDoc.ActiveDoc);
        foundKit = true;
      }
      catch
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No default kit found on this machine.");
        foundKit = false;
      }
    }

    //GH_Structure<IGH_Goo> transportsInput;
    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      DA.DisableGapLogic();

      if (!foundKit)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No kit found on this machine.");
        return;
      }

      if (RunCount == 1)
      {
        CreateCancelationToken();
        OutputWrappers = new List<StreamWrapper>();
        DA.GetDataTree(0, out dataInput);

        //the active document may have changed
        Converter.SetContextDocument(RhinoDoc.ActiveDoc);

        // Note: this method actually converts the objects to speckle too
        converted = Extras.Utilities.DataTreeToNestedLists(dataInput, Converter, source.Token, () =>
        {
          //ReportProgress("Conversion", Math.Round(convertedCount++ / (double)DataInput.DataCount, 2));
        });
      }

      //if (RunCount > 1)
      //  return;

      if (InPreSolve)
      {
        string messageInput = "";

        IGH_Goo transportInput = null;
        DA.GetData(1, ref transportInput);
        DA.GetData(2, ref messageInput);
        var transportsInput = new List<IGH_Goo> { transportInput };
        //var transportsInput = Params.Input[1].VolatileData.AllData(true).Select(x => x).ToList();


        var task = Task.Run(async () =>
        {

          if (converted.Count == 0)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Zero objects converted successfully. Send stopped.");
            return null;
          }

          var ObjectToSend = new Base();
          ObjectToSend["@data"] = converted;
          var TotalObjectCount = ObjectToSend.GetTotalChildrenCount();

          if (source.Token.IsCancellationRequested)
          {
            Message = "Out of time";
            return null;
          }

          // Part 2: create transports
          var Transports = new List<ITransport>();

          if (transportsInput.Count() == 0)
          {
            // TODO: Set default account + "default" user stream
          }

          var transportBranches = new Dictionary<ITransport, string>();
          int t = 0;
          foreach (var data in transportsInput)
          {
            var transport = data.GetType().GetProperty("Value").GetValue(data);

            if (transport is string s)
            {
              try
              {
                transport = new StreamWrapper(s);
              }
              catch (Exception e)
              {
                // TODO: Check this with team.
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, e.Message);
              }
            }

            if (transport is StreamWrapper sw)
            {
              if (sw.Type == StreamWrapperType.Undefined)
              {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input stream is invalid.");
                continue;
              }

              if (sw.Type == StreamWrapperType.Commit)
              {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Cannot push to a specific commit stream url.");
                continue;
              }

              if (sw.Type == StreamWrapperType.Object)
              {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Cannot push to a specific object stream url.");
                continue;
              }

              Account acc;
              try
              {
                acc = sw.GetAccount().Result;
              }
              catch (Exception e)
              {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, e.InnerException?.Message ?? e.Message);
                continue;
              }

              Logging.Analytics.TrackEvent(acc, Logging.Analytics.Events.Send, new Dictionary<string, object>() { { "sync", true } });

              var serverTransport = new ServerTransport(acc, sw.StreamId) { TransportName = $"T{t}" };
              transportBranches.Add(serverTransport, sw.BranchName ?? "main");
              Transports.Add(serverTransport);
            }
            else if (transport is ITransport otherTransport)
            {
              otherTransport.TransportName = $"T{t}";
              Transports.Add(otherTransport);
            }

            t++;
          }

          if (Transports.Count == 0)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not identify any valid transports to send to.");
            return null;
          }

          // Part 3: actually send stuff!
          if (source.Token.IsCancellationRequested)
          {
            Message = "Out of time";
            return null;
          }

          // Part 3.1: persist the objects
          var BaseId = await Operations.Send(
            ObjectToSend,
            source.Token,
            Transports,
            useDefaultCache: UseDefaultCache,
            onProgressAction: y => { },
            onErrorAction: (x, z) => { },
            disposeTransports: true);

          var message = messageInput;//.get_FirstItem(true).Value;
          if (message == "")
          {
            message = $"Pushed {TotalObjectCount} elements from Grasshopper.";
          }


          var prevCommits = new List<StreamWrapper>();

          foreach (var transport in Transports)
          {
            if (source.Token.IsCancellationRequested)
            {
              Message = "Out of time";
              return null;
            }

            if (!(transport is ServerTransport))
            {
              continue; // skip non-server transports (for now)
            }

            try
            {
              var client = new Client(((ServerTransport)transport).Account);
              var branch = transportBranches.ContainsKey(transport) ? transportBranches[transport] : "main";

              var commitCreateInput = new CommitCreateInput
              {
                branchName = branch,
                message = message,
                objectId = BaseId,
                streamId = ((ServerTransport)transport).StreamId,
                sourceApplication = Extras.Utilities.GetVersionedAppName()
              };

              // Check to see if we have a previous commit; if so set it.
              var prevCommit = prevCommits.FirstOrDefault(c =>
                c.ServerUrl == client.ServerUrl && c.StreamId == ((ServerTransport)transport).StreamId);
              if (prevCommit != null)
              {
                commitCreateInput.parents = new List<string>() { prevCommit.CommitId };
              }

              var commitId = await client.CommitCreate(source.Token, commitCreateInput);

              var wrapper = new StreamWrapper($"{client.Account.serverInfo.url}/streams/{((ServerTransport)transport).StreamId}/commits/{commitId}?u={client.Account.userInfo.id}");
              prevCommits.Add(wrapper);
            }
            catch (Exception e)
            {
              AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
              return null;
            }
          }

          if (source.Token.IsCancellationRequested)
          {
            Message = "Out of time";
            return null;
          }

          return prevCommits;
        }, source.Token);

        TaskList.Add(task);
        return;
      }

      if (source.IsCancellationRequested)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Run out of time!");
      }
      else if (!GetSolveResults(DA, out List<StreamWrapper> outputWrappers))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not running multithread");
      }
      else
      {
        OutputWrappers.AddRange(outputWrappers);
        DA.SetDataList(0, outputWrappers);
        return;
      }
    }

    private void CreateCancelationToken()
    {
      source = new CancellationTokenSource(delay);
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon => Resources.SynchronousSender;

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("049e78bf-a400-4551-ac45-21a28843a222"); }
    }

  }
}
