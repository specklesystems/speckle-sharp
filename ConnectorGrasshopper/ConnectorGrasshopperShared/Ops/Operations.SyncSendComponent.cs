using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ConnectorGrasshopper.Objects;
using ConnectorGrasshopper.Properties;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using Speckle.Core.Transports;
using Utilities = ConnectorGrasshopper.Extras.Utilities;

namespace ConnectorGrasshopper.Ops;

public class SyncSendComponent : SelectKitTaskCapableComponentBase<List<StreamWrapper>>
{
  private Base converted;

  public SyncSendComponent()
    : base(
      "Synchronous Sender",
      "SS",
      "Send data to a Speckle server Synchronously. This will block GH until all the data are received which can be used to safely trigger other processes downstream",
      ComponentCategories.SECONDARY_RIBBON,
      ComponentCategories.SEND_RECEIVE
    ) { }

  public List<StreamWrapper> OutputWrappers { get; private set; } = new();
  public bool UseDefaultCache { get; set; } = true;
  public override GH_Exposure Exposure => GH_Exposure.secondary | GH_Exposure.obscure;
  public override bool CanDisableConversion => false;

  protected override Bitmap Icon => Resources.SynchronousSender;
  public override Guid ComponentGuid => new("A6ED7A5F-D013-4086-A4BB-F08B42B2A6B8");

  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddGenericParameter("Data", "D", "The data to send.", GH_ParamAccess.tree);
    pManager.AddGenericParameter("Stream", "S", "Stream(s) and/or transports to send to.", GH_ParamAccess.item);
    pManager.AddTextParameter(
      "Message",
      "M",
      "Commit message. If left blank, one will be generated for you.",
      GH_ParamAccess.item,
      ""
    );

    Params.Input[2].Optional = true;
  }

  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddGenericParameter(
      "Stream",
      "S",
      "Stream or streams pointing to the created commit",
      GH_ParamAccess.list
    );
  }

  public override void SetConverterFromKit(string kitName)
  {
    base.SetConverterFromKit(kitName);
    ExpireSolution(true);
  }

  public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
  {
    base.AppendAdditionalMenuItems(menu);

    Menu_AppendSeparator(menu);
    if (OutputWrappers != null && OutputWrappers.Count != 0)
    {
      Menu_AppendSeparator(menu);
      foreach (var ow in OutputWrappers)
      {
        Menu_AppendItem(
          menu,
          $"View commit {ow.CommitId} @ {ow.ServerUrl} online ↗",
          (s, e) => Open.Url($"{ow.ServerUrl}/streams/{ow.StreamId}/commits/{ow.CommitId}")
        );
      }
    }
  }

  public override bool Write(GH_IWriter writer)
  {
    writer.SetBoolean("UseDefaultCache", UseDefaultCache);
    var owSer = string.Join("\n", OutputWrappers.Select(ow => $"{ow.ServerUrl}\t{ow.StreamId}\t{ow.CommitId}"));
    writer.SetString("OutputWrappers", owSer);
    return base.Write(writer);
  }

  public override bool Read(GH_IReader reader)
  {
    UseDefaultCache = reader.GetBoolean("UseDefaultCache");
    var wrappersRaw = reader.GetString("OutputWrappers");
    var wrapperLines = wrappersRaw.Split('\n');
    if (wrapperLines.Length != 0 && wrappersRaw != "")
    {
      foreach (var line in wrapperLines)
      {
        var pieces = line.Split('\t');
        OutputWrappers.Add(
          new StreamWrapper
          {
            ServerUrl = pieces[0],
            StreamId = pieces[1],
            CommitId = pieces[2]
          }
        );
      }
    }

    return base.Read(reader);
  }

  public override void SolveInstanceWithLogContext(IGH_DataAccess DA)
  {
    DA.DisableGapLogic();
    if (RunCount == 1)
    {
      OutputWrappers = new List<StreamWrapper>();
      DA.GetDataTree(0, out GH_Structure<IGH_Goo> dataInput);
      // Note: this method actually converts the objects to speckle too
      converted = Utilities.DataTreeToSpeckle(dataInput, Converter, CancelToken);
    }

    if (InPreSolve)
    {
      var messageInput = "";

      IGH_Goo transportInput = null;
      DA.GetData(1, ref transportInput);
      DA.GetData(2, ref messageInput);
      var transportsInput = new List<IGH_Goo> { transportInput };

      var task = Task.Run(
        async () =>
        {
          if (converted == null)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Zero objects converted successfully. Send stopped.");
            return null;
          }

          var objectToSend = new Base();
          objectToSend["@data"] = converted;
          var totalObjectCount = objectToSend.GetTotalChildrenCount();

          if (CancelToken.IsCancellationRequested)
          {
            Message = "Out of time";
            return null;
          }

          // Part 2: create transports
          var transports = new List<ITransport>();

          if (transportsInput.Count == 0)
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
              catch (Exception e) when (!e.IsFatal())
              {
                // TODO: Check this with team.
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, e.ToFormattedString());
              }
            }

            if (transport is StreamWrapper sw)
            {
              switch (sw.Type)
              {
                case StreamWrapperType.Undefined:
                  AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Input stream is invalid.");
                  continue;
                case StreamWrapperType.Commit:
                  AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Cannot push to a specific commit stream url.");
                  continue;
                case StreamWrapperType.Object:
                  AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Cannot push to a specific object stream url.");
                  continue;
              }

              Account acc;
              try
              {
                acc = sw.GetAccount().Result;
              }
              catch (SpeckleException e)
              {
                SpeckleLog.Logger.Warning(e, "Failed to get account from stream wrapper");
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, e.ToFormattedString());
                continue;
              }

              Speckle.Core.Logging.Analytics.TrackEvent(
                acc,
                Speckle.Core.Logging.Analytics.Events.Send,
                new Dictionary<string, object> { { "sync", true } }
              );

              var serverTransport = new ServerTransport(acc, sw.StreamId) { TransportName = $"T{t}" };
              transportBranches.Add(serverTransport, sw.BranchName ?? "main");
              transports.Add(serverTransport);
            }
            else if (transport is ITransport otherTransport)
            {
              otherTransport.TransportName = $"T{t}";
              transports.Add(otherTransport);
            }

            t++;
          }

          if (transports.Count == 0)
          {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not identify any valid transports to send to.");
            return null;
          }

          // Part 3: actually send stuff!
          if (CancelToken.IsCancellationRequested)
          {
            Message = "Out of time";
            return null;
          }

          // Part 3.1: persist the objects
          var BaseId = await Operations
            .Send(objectToSend, CancelToken, transports, UseDefaultCache, y => { }, (x, z) => { }, true)
            .ConfigureAwait(false);

          var message = messageInput; //.get_FirstItem(true).Value;
          if (message == "")
          {
            message = $"Pushed {totalObjectCount} elements from Grasshopper.";
          }

          var prevCommits = new List<StreamWrapper>();

          foreach (var transport in transports)
          {
            if (CancelToken.IsCancellationRequested)
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
                sourceApplication = Utilities.GetVersionedAppName()
              };

              // Check to see if we have a previous commit; if so set it.
              var prevCommit = prevCommits.FirstOrDefault(c =>
                c.ServerUrl == client.ServerUrl && c.StreamId == ((ServerTransport)transport).StreamId
              );
              if (prevCommit != null)
              {
                commitCreateInput.parents = new List<string> { prevCommit.CommitId };
              }

              var commitId = await client.CommitCreate(commitCreateInput, CancelToken);

              var wrapper = new StreamWrapper(
                $"{client.Account.serverInfo.url}/streams/{((ServerTransport)transport).StreamId}/commits/{commitId}?u={client.Account.userInfo.id}"
              );
              prevCommits.Add(wrapper);
            }
            catch (Exception e) when (!e.IsFatal())
            {
              SpeckleLog.Logger.Warning(e, "Failed to send synchronously");
              AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.ToFormattedString());
              return null;
            }
          }

          if (CancelToken.IsCancellationRequested)
          {
            Message = "Out of time";
            return null;
          }

          return prevCommits;
        },
        CancelToken
      );

      TaskList.Add(task);
      return;
    }

    if (CancelToken.IsCancellationRequested)
    {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Run out of time!");
    }
    else if (!GetSolveResults(DA, out var outputWrappers))
    {
      AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not running multithreading");
    }
    else
    {
      OutputWrappers.AddRange(outputWrappers);
      DA.SetDataList(0, outputWrappers);
    }
  }
}
