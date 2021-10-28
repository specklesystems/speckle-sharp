using ConnectorGSA.Models;
using ConnectorGSA.Utilities;
using Microsoft.Win32;
using Newtonsoft.Json;
using Speckle.ConnectorGSA.Proxy;
using Speckle.ConnectorGSA.Proxy.Cache;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ConnectorGSA
{
  public static class Commands
  {
    public static object Assert { get; private set; }

    public static async Task<bool> InitialLoad(TabCoordinator coordinator, IProgress<MessageEventArgs> loggingProgress)
    {
      coordinator.Init();
      try
      {
        //This will throw an exception if there is no default account
        var account = AccountManager.GetDefaultAccount();
        if (account == null)
        {
          return false;
        }
        ((GsaModel)Instance.GsaModel).Account = account;
        return await CompleteLogin(coordinator, new SpeckleAccountForUI(), loggingProgress);
      }
      catch
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "No default account found - press the Login button to login/select an account"));
        return false;
      }
    }

    public static async Task<bool> CompleteLogin(TabCoordinator coordinator, SpeckleAccountForUI accountCandidate, IProgress<MessageEventArgs> loggingProgress)
    {
      var messenger = new ProgressMessenger(loggingProgress);

      if (accountCandidate != null && accountCandidate.IsValid)
      {
        var streamsForAccount = new List<Stream>();
        var client = new Client(((GsaModel)Instance.GsaModel).Account);
        try
        {
          streamsForAccount = await client.StreamsGet(50);  //Undocumented limitation servers cannot seem to return more than 50 items
        }
        catch (Exception ex)
        {
          loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Error, "Unable to get stream list"));
        }
        

        coordinator.Account = accountCandidate;
        coordinator.ServerStreamList.StreamListItems.Clear();

        foreach (var sd in streamsForAccount)
        {
          coordinator.ServerStreamList.StreamListItems.Add(new StreamListItem(sd.id, sd.name));
        }

        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Logged into account at: " + coordinator.Account.ServerUrl));
        return true;
      }
      else
      {
        return false;
      }
    }

    public static bool OpenFile(TabCoordinator coordinator, IProgress<MessageEventArgs> loggingProgress)
    {
      var openFileDialog = new OpenFileDialog();
      if (openFileDialog.ShowDialog() == true)
      {
        try
        {
          Commands.OpenFile(openFileDialog.FileName, true);
        }
        catch (Exception ex)
        {
          loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Error, "Unable to load " + openFileDialog.FileName + " - refer to logs for more information"));
          loggingProgress.Report(new MessageEventArgs(MessageIntent.TechnicalLog, MessageLevel.Error, ex, "Unable to load file"));
          return false;
        }
        if (!string.IsNullOrEmpty(openFileDialog.FileName))
        {
          coordinator.FilePath = openFileDialog.FileName;
        }

        coordinator.FileStatus = GsaLoadedFileType.ExistingFile;
        return true;
      }
      else
      {
        return false;
      }
    }

    public static bool OpenFile(string filePath, bool visible)
    {
      Instance.GsaModel.Proxy = new GsaProxy(); //Use a real proxy
      var opened = ((GsaProxy)Instance.GsaModel.Proxy).OpenFile(filePath, visible);
      if (!opened)
      {
        return false;
      }
      return true;
    }

    public static bool ExtractSavedReceptionStreamInfo(bool? receive, bool? send, out List<StreamState> streamStates)
    { 
      List<StreamState> allSaved;
      try
      {
        var sid = ((GsaProxy)Instance.GsaModel.Proxy).GetTopLevelSid();
        allSaved = JsonConvert.DeserializeObject<List<StreamState>>(sid);
        if (allSaved == null) {
          allSaved = new List<StreamState>();
        }
      }
      catch
      {
        allSaved = new List<StreamState>();
      }      
      
      var userId = ((GsaModel)Instance.GsaModel).Account.userInfo.id;
      var restApi = ((GsaModel)Instance.GsaModel).Account.serverInfo.url;

      //So currently it assumes that a new user for this file will have a new stream created for them, even if other users saved this file with their stream info
      var accountStreamStates = allSaved.Where(ss => ((ss.UserId == userId) && ss.ServerUrl.Equals(restApi, StringComparison.InvariantCultureIgnoreCase))).ToList();
      streamStates = new List<StreamState>();
      if (receive.HasValue)
      {
        streamStates.AddRange(accountStreamStates.Where(ss => ss.IsReceiving == receive.Value));
      }
      if (send.HasValue)
      {
        streamStates.AddRange(accountStreamStates.Where(ss => ss.IsSending == send.Value));
      }
      return (streamStates != null && streamStates.Count > 0);
    }

    public static bool UpsertSavedReceptionStreamInfo(bool? receive, bool? send, params StreamState[] streamStates)
    {
      var sid = ((GsaProxy)Instance.GsaModel.Proxy).GetTopLevelSid();
      List<StreamState> allSs = null;
      try
      {
        allSs = JsonConvert.DeserializeObject<List<StreamState>>(sid);
      }
      catch (JsonException ex)
      {
        //Could not deserialise, probably because it has a v1-format of stream information.  In this case, ignore the info

        //TO DO: write technical long line here
      }

      if (allSs == null || allSs.Count() == 0)
      {
        allSs = streamStates.ToList();
      }
      else
      {
        var merged = new List<StreamState>();
        foreach (var ss in streamStates)
        {
          var matching = allSs.FirstOrDefault(s => s.Equals(ss));
          if (matching != null)
          {
            if (matching.IsReceiving != ss.IsReceiving)
            {
              matching.IsReceiving = true;  //This is merging of two booleans, where a true value is to be set if any are true
            }
            if (matching.IsSending != ss.IsSending)
            {
              matching.IsSending = true;  //This is merging of two booleans, where a true value is to be set if any are true
            }
            merged.Add(ss);
          }
        }

        allSs = allSs.Union(streamStates.Except(merged)).ToList();
      }

      var newSid = JsonConvert.SerializeObject(allSs);
      return ((GsaProxy)Instance.GsaModel.Proxy).SetTopLevelSid(newSid);
    }

    public static bool CloseFile(string filePath, bool visible)
    {
      ((GsaProxy)Instance.GsaModel.Proxy).Close();
      return ((GsaProxy)Instance.GsaModel.Proxy).Clear();
    }

    public static bool LoadDataFromFile(IProgress<string> gwaLoggingProgress = null, IEnumerable<ResultGroup> resultGroups = null, IEnumerable<ResultType> resultTypes = null)
    {
      ((GsaProxy)Instance.GsaModel.Proxy).Clear();
      var loadedCache = UpdateCache(gwaLoggingProgress);
      int cumulativeErrorRows = 0;

      if (resultGroups != null && resultGroups.Any() && resultTypes != null && resultTypes.Any())
      {
        if (!Instance.GsaModel.Proxy.PrepareResults(resultTypes, Instance.GsaModel.Result1DNumPosition + 2))
        {
          return false;
        }
        foreach (var g in resultGroups)
        {
          if (!((GsaProxy)Instance.GsaModel.Proxy).LoadResults(g, out int numErrorRows) || numErrorRows > 0)
          {
            return false;
          }
          cumulativeErrorRows += numErrorRows;
        }
      }

      return (loadedCache && (cumulativeErrorRows == 0));
    }

    public static bool ConvertToNative(ISpeckleConverter converter, IProgress<MessageEventArgs> loggingProgress) //Includes writing to Cache
    {
      var speckleDependencyTree = ((GsaModel)Instance.GsaModel).SpeckleDependencyTree();

      //With the attached objects in speckle objects, there is no type dependency needed on the receive side, so just convert each object

      if (Instance.GsaModel.Cache.GetSpeckleObjects(out var speckleObjects))
      {
        var objectsByType = speckleObjects.GroupBy(t => t.GetType()).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var gen in speckleDependencyTree)
        {
          //foreach (var t in gen)
          Parallel.ForEach(gen, t =>
          {
            if (objectsByType.ContainsKey(t))
            {
              foreach (Base so in objectsByType[t])
              //Parallel.ForEach(objectsByType[t].Cast<Base>(), so =>
              {
                string appId = "";
                try
                {
                  if (converter.CanConvertToNative(so))
                  {
                    var nativeObjects = converter.ConvertToNative(new List<Base> { so }).Cast<GsaRecord>().ToList();
                    appId = string.IsNullOrEmpty(so.applicationId) ? so.id : so.applicationId;
                    Instance.GsaModel.Cache.SetNatives(so.GetType(), appId, nativeObjects);
                  }
                }
                catch (Exception ex)
                {
                  loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Error, "Unable to convert " + t.Name + " " + appId + " - refer to logs for more information"));
                  loggingProgress.Report(new MessageEventArgs(MessageIntent.TechnicalLog, MessageLevel.Error, ex, "Unable to load file"));
                }
              }
              //);
            }
          }
          );
        }
      }

      return true;
    }

    public static List<Base> ConvertToSpeckle(ISpeckleConverter converter)
    {
      if (!Instance.GsaModel.Cache.GetNatives(out List<GsaRecord> gsaRecords))
      {
        return null;
      }

      //This converts all the natives ONCE and THEN assigns them into the correct layer-specific Model object(s)
      var convertedObjs = converter.ConvertToSpeckle(gsaRecords.Cast<object>().ToList());

      return convertedObjs;
    }

    public static async Task<bool> SendCommit(Base commitObj, StreamState state, string parent, params ITransport[] transports)
    {
      var commitObjId = await Operations.Send(
        @object: commitObj,
        transports: transports.ToList(),
        onErrorAction: (s, e) =>
        {
          state.Errors.Add(e);
        }
        );

      if (transports.Any(t => t is ServerTransport))
      {
        var actualCommit = new CommitCreateInput
        {
          streamId = state.Stream.id,
          objectId = commitObjId,
          branchName = "main",
          message = "Pushed data from GSA",
          sourceApplication = Applications.GSA
        };

        if (!string.IsNullOrEmpty(parent))
        {
          actualCommit.parents = new List<string>() { parent };
        }

        //if (state.PreviousCommitId != null) { actualCommit.parents = new List<string>() { state.PreviousCommitId }; }

        try
        {
          var commitId = await state.Client.CommitCreate(actualCommit);
          ((GsaModel)Instance.GsaModel).LastCommitId = commitId;
        }
        catch (Exception e)
        {
          state.Errors.Add(e);
        }
      }

      return (state.Errors.Count == 0);
    }

    internal static async Task<bool> Receive(TabCoordinator coordinator, IProgress<MessageEventArgs> loggingProgress, IProgress<string> statusProgress, IProgress<double> percentageProgress)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Applications.GSA);
      var percentage = 0;

      Instance.GsaModel.StreamLayer = coordinator.ReceiverTab.TargetLayer;
      Instance.GsaModel.Units = UnitEnumToString(coordinator.ReceiverTab.CoincidentNodeUnits);
      Instance.GsaModel.LoggingMinimumLevel = (int)coordinator.LoggingMinimumLevel;

      //A simplified one just for use by the proxy class
      var proxyLoggingProgress = new Progress<string>();
      proxyLoggingProgress.ProgressChanged += (object o, string e) =>
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Error, e));
        loggingProgress.Report(new MessageEventArgs(MessageIntent.TechnicalLog, MessageLevel.Error, e));
      };

      var perecentageProgressLock = new object();

      var account = ((GsaModel)Instance.GsaModel).Account;
      var client = new Client(account);

      var startTime = DateTime.Now;

      statusProgress.Report("Reading GSA data into cache");
      //Load data to cause merging
      Commands.LoadDataFromFile(proxyLoggingProgress);

      double factor = 1;
      if (Instance.GsaModel.Cache.GetNatives(typeof(GsaUnitData), out var gsaUnitDataRecords))
      {
        var lengthUnitData = (GsaUnitData)gsaUnitDataRecords.FirstOrDefault(r => ((GsaUnitData)r).Option == UnitDimension.Length);
        if (lengthUnitData != null)
        {
          var fromStr = coordinator.ReceiverTab.CoincidentNodeUnits.GetStringValue();
          var toStr = lengthUnitData.Name;
          factor = (lengthUnitData == null) ? 1 : Units.GetConversionFactor(fromStr, toStr);
        }
      }
      Instance.GsaModel.CoincidentNodeAllowance = coordinator.ReceiverTab.CoincidentNodeAllowance * factor;

      percentage = 10;
      percentageProgress.Report(percentage);

      TimeSpan duration = DateTime.Now - startTime;
      if (duration.Seconds > 0)
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Loaded data into cache"));
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Duration of reading GSA model into cache: " + duration.ToString(@"hh\:mm\:ss")));
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Telemetry, MessageLevel.Information, "receive", "update-cache", "duration", duration.ToString(@"hh\:mm\:ss")));
      }
      startTime = DateTime.Now;


      statusProgress.Report("Accessing streams");
      var streamIds = coordinator.ReceiverTab.StreamList.StreamListItems.Select(i => i.StreamId).ToList();
      var receiveTasks = new List<Task>();
      foreach (var streamId in streamIds)
      {
        var streamState = new StreamState(account.userInfo.id, account.serverInfo.url)
        {
          Stream = new Stream() { id = streamId },
          IsReceiving = true
        };
        var transport = new ServerTransport(streamState.Client.Account, streamState.Stream.id);

        receiveTasks.Add(streamState.RefreshStream(loggingProgress)
          .ContinueWith(async (refreshed) =>
            {
              if (refreshed.Result)
              {
                streamState.Stream.branch = streamState.Client.StreamGetBranches(streamId, 1).Result.First();
                if (streamState.Stream.branch.commits == null || streamState.Stream.branch.commits.totalCount == 0)
                {
                  loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Error, "This branch has no commits"));
                  loggingProgress.Report(new MessageEventArgs(MessageIntent.TechnicalLog, MessageLevel.Error, "This branch has no commits"));
                  percentageProgress.Report(0);
                  return;
                }
                var commitId = streamState.Stream.branch.commits.items.FirstOrDefault().referencedObject;

                var received = await Commands.Receive(commitId, streamState, transport, converter.CanConvertToNative);
                if (received)
                {
                  loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Received data from " + streamId + " stream"));
                }

                if (streamState.Errors != null && streamState.Errors.Count > 0)
                {
                  foreach (var se in streamState.Errors)
                  {
                    loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Error, se.Message));
                    loggingProgress.Report(new MessageEventArgs(MessageIntent.TechnicalLog, MessageLevel.Error, se, se.Message));
                  }
                }

                lock (perecentageProgressLock)
                {
                  percentage += (50 / streamIds.Count);
                  percentageProgress.Report(percentage);
                }
              }
            }));
      }
      await Task.WhenAll(receiveTasks.ToArray());

      duration = DateTime.Now - startTime;
      loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Duration of reception from Speckle and scaling: " + duration.ToString(@"hh\:mm\:ss")));
      loggingProgress.Report(new MessageEventArgs(MessageIntent.Telemetry, MessageLevel.Information, "receive", "reception and scaling", "duration", duration.ToString(@"hh\:mm\:ss")));

      startTime = DateTime.Now;

      statusProgress.Report("Converting");
      var numToConvert = ((GsaCache)Instance.GsaModel.Cache).NumSpeckleObjects;
      int numConverted = 0;
      int totalConversionPercentage = 90 - percentage;
      Instance.GsaModel.ConversionProgress = new Progress<bool>((bool success) =>
      {
        lock (perecentageProgressLock)
        {
          numConverted++;
        }
        percentageProgress.Report(percentage + Math.Round(((double)numConverted / (double)numToConvert) * totalConversionPercentage, 0));
      });

      Commands.ConvertToNative(converter, loggingProgress);

      if (converter.ConversionErrors != null && converter.ConversionErrors.Count > 0)
      {
        foreach (var ce in converter.ConversionErrors)
        {
          loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Error, ce.Message));
          loggingProgress.Report(new MessageEventArgs(MessageIntent.TechnicalLog, MessageLevel.Error, ce, ce.Message));
        }
      }

      loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Converted Speckle to GSA objects"));

      duration = DateTime.Now - startTime;
      if (duration.Seconds > 0)
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Duration of conversion from Speckle: " + duration.ToString(@"hh\:mm\:ss")));
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Telemetry, MessageLevel.Information, "receive", "conversion", "duration", duration.ToString(@"hh\:mm\:ss")));
      }
      startTime = DateTime.Now;

      //The cache is filled with natives
      if (Instance.GsaModel.Cache.GetNatives(out var gsaRecords))
      {
        ((GsaProxy)Instance.GsaModel.Proxy).WriteModel(gsaRecords, proxyLoggingProgress);
      }

      percentageProgress.Report(100);

      ((GsaProxy)Instance.GsaModel.Proxy).UpdateViews();

      duration = DateTime.Now - startTime;
      if (duration.Seconds > 0)
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Duration of writing converted objects to GSA: " + duration.ToString(@"hh\:mm\:ss")));
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Telemetry, MessageLevel.Information, "receive", "write-model", "duration", duration.ToString(@"hh\:mm\:ss")));
      }

      statusProgress.Report("Ready");
      Console.WriteLine("Receiving complete");

      return true;
    }

    public static async Task<bool> Receive(string commitId, StreamState state, ITransport transport, Func<Base, bool> IsSingleObjectFn)
    {
      var commitObject = await Operations.Receive(
          commitId,
          transport,
          onErrorAction: (s, e) =>
          {
            state.Errors.Add(e);
          },
          disposeTransports: true
          );

      if (commitObject != null)
      {
        var receivedObjects = FlattenCommitObject(commitObject, IsSingleObjectFn);

        var receivedByType = receivedObjects.GroupBy(ro => ro.GetType()).ToDictionary(ro => ro.Key, ro => ro.ToList());
        //var receivedByTypeAppId = new Dictionary<Type, Dictionary<string, List<object>>>();

        int index = 0;
        bool found = false;
        do
        {
          foreach (var t in receivedByType.Keys)
          {
            var receivedByTypeAppId = receivedByType[t].GroupBy(o => o.applicationId).ToDictionary(g => g.Key, g => g.ToList());
            found = receivedByTypeAppId.Any(kvp => kvp.Value.Count > index);
            if (found)
            {
              if (!Instance.GsaModel.Cache.Upsert(receivedByTypeAppId.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value[index])))
              {
                return false;
              }
            }
          }
          index++;
        } while (found);

        //var task = (Instance.GsaModel.Cache.Upsert(objDict)
        //  && receivedObjects != null && receivedObjects.Any() && state.Errors.Count == 0);
        return true;
      }
      return false;
    }

    private static bool UpdateCache(IProgress<string> gwaLoggingProgress = null, bool onlyNodesWithApplicationIds = true)
    {
      var errored = new Dictionary<int, GsaRecord>();

      try
      {
        if (((GsaProxy)Instance.GsaModel.Proxy).GetGwaData(Instance.GsaModel.StreamLayer, gwaLoggingProgress, out var records))
        {
          for (int i = 0; i < records.Count(); i++)
          {
            if (!Instance.GsaModel.Cache.Upsert(records[i]))
            {
              errored.Add(i, records[i]);
            }
          }
        }
        return true;
      }
      catch
      {
        return false;
      }
    }

    private static List<Base> FlattenCommitObject(object obj, Func<Base, bool> IsSingleObjectFn)
    {
      //This is needed because with GSA models, there could be a design and analysis layer with objects appearing in both, so only include the first
      //occurrence of each object (distinguished by the ID returned by the Base.GetId() method) in the list returned
      var uniques = new Dictionary<Type, HashSet<string>>();
      return FlattenCommitObject(obj, IsSingleObjectFn, uniques);
    }


    private static List<Base> FlattenCommitObject(object obj, Func<Base, bool> IsSingleObjectFn, Dictionary<Type, HashSet<string>> uniques)
    {
      List<Base> objects = new List<Base>();

      if (obj is Base @base)
      {
        if (IsSingleObjectFn(@base))
        {
          var t = obj.GetType();
          var id = (string.IsNullOrEmpty(@base.id)) ? @base.GetId() : @base.id;
          if (!uniques.ContainsKey(t))
          {
            uniques.Add(t, new HashSet<string>() { id });
            objects.Add(@base);
          }
          if (!uniques[t].Contains(id))
          {
            uniques[t].Add(id);
            objects.Add(@base);
          }

          return objects;
        }
        else
        {
          foreach (var prop in @base.GetDynamicMembers())
          {
            objects.AddRange(FlattenCommitObject(@base[prop], IsSingleObjectFn, uniques));
          }
          foreach (var kvp in @base.GetMembers())
          {
            var prop = kvp.Key;
            objects.AddRange(FlattenCommitObject(@base[prop], IsSingleObjectFn, uniques));
          }
          return objects;
        }
      }

      if (obj is List<object> list)
      {
        foreach (var listObj in list)
        {
          objects.AddRange(FlattenCommitObject(listObj, IsSingleObjectFn, uniques));
        }
        return objects;
      }
      else if (obj is List<Base> baseObjList)
      {
        foreach (var baseObj in baseObjList)
        {
          objects.AddRange(FlattenCommitObject(baseObj, IsSingleObjectFn, uniques));
        }
        return objects;
      }
      else if (obj is IDictionary dict)
      {
        foreach (DictionaryEntry kvp in dict)
        {
          objects.AddRange(FlattenCommitObject(kvp.Value, IsSingleObjectFn, uniques));
        }
        return objects;
      }

      return objects;
    }

    internal static async Task<List<StreamState>> GetStreamList(TabCoordinator coordinator, SpeckleAccountForUI account, Progress<MessageEventArgs> loggingProgress)
    {
      return new List<StreamState>();
    }

    internal static bool NewFile(TabCoordinator coordinator, IProgress<MessageEventArgs> loggingProgress)
    {
      ((GsaProxy)Instance.GsaModel.Proxy).NewFile(true);

      coordinator.ReceiverTab.ReceiverStreamStates.Clear();
      coordinator.SenderTab.SenderStreamStates.Clear();

      loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Created new file."));

      return true;
    }

    public static async Task<bool> ReadSavedStreamInfo(TabCoordinator coordinator, IProgress<MessageEventArgs> loggingProgress)
    {
      if (coordinator.FileStatus == GsaLoadedFileType.ExistingFile && coordinator.Account != null && coordinator.Account.IsValid)
      {
        var retrieved = ExtractSavedReceptionStreamInfo(true, true, out List<StreamState> steamStates);
        if (!retrieved)
        {
          return false;
        }
        var receivingStreamStates = steamStates.Where(ss => ss.IsReceiving);
        var sendingStreamStates = steamStates.Where(ss => ss.IsSending);
        if (receivingStreamStates.Any())
        {
          coordinator.ReceiverTab.ReceiverStreamStates.Clear();
          coordinator.ReceiverTab.ReceiverStreamStates.AddRange(receivingStreamStates);
          if (coordinator.ReceiverTab.ReceiverStreamStates.Count() > 0)
          {
            var invalidStreamStates = new List<StreamState>();
            //Since the buckets are stored in the SID tags, but not the stream names, get the stream names
            foreach (var r in coordinator.ReceiverTab.ReceiverStreamStates)
            {
              if (!(await r.RefreshStream(loggingProgress)))
              {
                invalidStreamStates.Add(r);
              }
            }
            invalidStreamStates.ForEach(r => coordinator.ReceiverTab.RemoveStreamState(r));
          }
          coordinator.ReceiverTab.StreamStatesToStreamList();

          loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Found streams from the same server stored in file for receiving: "
             + string.Join(", ", coordinator.ReceiverTab.ReceiverStreamStates.Select(r => r.StreamId))));
        }
        if (sendingStreamStates.Any())
        {
          coordinator.SenderTab.SenderStreamStates.Clear();
          coordinator.SenderTab.SenderStreamStates.AddRange(sendingStreamStates);
          if (coordinator.SenderTab.SenderStreamStates.Count() > 0)
          {
            var invalidStreamStates = new List<StreamState>();
            //Since the buckets are stored in the SID tags, but not the stream names, get the stream names
            foreach (var r in coordinator.SenderTab.SenderStreamStates)
            {
              if (!(await r.RefreshStream(loggingProgress)))
              {
                invalidStreamStates.Add(r);
              }
            }
            invalidStreamStates.ForEach(r => coordinator.SenderTab.RemoveStreamState(r));
          }

          coordinator.SenderTab.StreamStatesToStreamList();

          loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Found streams from the same server stored in file for sending: "
             + string.Join(", ", coordinator.SenderTab.SenderStreamStates.Select(r => r.StreamId))));
        }
      }
      return true;
    }

    internal static bool SaveFile(TabCoordinator coordinator)
    {
      if (coordinator.FileStatus == GsaLoadedFileType.NewFile)
      {
        var saveFileDialog = new SaveFileDialog
        {
          Filter = "GSA files (*.gwb)|*.gwb",
          DefaultExt = "gwb",
          AddExtension = true
        };
        if (saveFileDialog.ShowDialog() == true)
        {
          ((GsaProxy)Instance.GsaModel.Proxy).SaveAs(saveFileDialog.FileName);
          coordinator.FilePath = saveFileDialog.FileName;
        }
      }
      else if (coordinator.FileStatus == GsaLoadedFileType.ExistingFile)
      {
        ((GsaProxy)Instance.GsaModel.Proxy).SaveAs(coordinator.FilePath);
      }
      return true;
    }

    internal static async Task<bool> RenameStream(TabCoordinator coordinator, string streamId, string newStreamName, Progress<MessageEventArgs> loggingProgress)
    {
      var messenger = new ProgressMessenger(loggingProgress);

      var streamState = coordinator.SenderTab.SenderStreamStates.FirstOrDefault(ss => ss.StreamId == streamId);

      if (streamState == null)
      {
        return false;
      }

      var changed = await streamState.Client.StreamUpdate(new StreamUpdateInput() { id = streamId, name = newStreamName });

      //var changed = await SpeckleInterface.SpeckleStreamManager.UpdateStreamName(coordinator.Account.ServerUrl, coordinator.Account.Token, streamId, newStreamName, messenger);

      return changed;
    }

    internal static async Task<bool> CloneStream(TabCoordinator coordinator, string streamId, Progress<MessageEventArgs> loggingProgress)
    {
      var messenger = new ProgressMessenger(loggingProgress);

      //var clonedStreamId = await SpeckleInterface.SpeckleStreamManager.CloneStream(coordinator.Account.ServerUrl, coordinator.Account.Token, streamId, messenger);

      //return (!string.IsNullOrEmpty(clonedStreamId));
      return true;
    }

    internal static async Task<bool> SendTriggered(TabCoordinator coordinator, IProgress<MessageEventArgs> loggingProgress, 
      IProgress<string> statusProgress, IProgress<double> percentageProgress)
    {
      var result = await Send(coordinator, coordinator.SenderTab.SenderStreamStates.First(), loggingProgress, statusProgress, percentageProgress);
      return result;
    }

    private static async Task<bool> Send(TabCoordinator coordinator, StreamState ss, IProgress<MessageEventArgs> loggingProgress, IProgress<string> statusProgress, IProgress<double> percentageProgress)
    {
      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Applications.GSA);
      var account = ((GsaModel)Instance.GsaModel).Account;
      var percentage = 0;
      var perecentageProgressLock = new object();

      //A simplified one just for use by the proxy class
      var proxyLoggingProgress = new Progress<string>();
      proxyLoggingProgress.ProgressChanged += (object o, string e) =>
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Error, e));
        loggingProgress.Report(new MessageEventArgs(MessageIntent.TechnicalLog, MessageLevel.Error, e));
      };

      var startTime = DateTime.Now;

      statusProgress.Report("Preparing cache");
      Commands.LoadDataFromFile(proxyLoggingProgress); //Ensure all nodes
      loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Loaded data from file into cache"));

      percentage += 20;
      percentageProgress.Report(percentage);

      var resultsToSend = coordinator.SenderTab.ResultSettings.ResultSettingItems.Where(rsi => rsi.Selected).ToList();
      if (resultsToSend != null && resultsToSend.Count() > 0 && !string.IsNullOrEmpty(coordinator.SenderTab.LoadCaseList)
        && (Instance.GsaModel.ResultCases == null || Instance.GsaModel.ResultCases.Count() == 0))
      {
        try
        {
          statusProgress.Report("Preparing results");
          var analIndices = new List<int>();
          var comboIndices = new List<int>();
          if (((GsaCache)Instance.GsaModel.Cache).GetNatives<GsaAnal>(out var analRecords) && analRecords != null && analRecords.Count() > 0)
          {
            analIndices.AddRange(analRecords.Select(r => r.Index.Value));
          }
          if (((GsaCache)Instance.GsaModel.Cache).GetNatives<GsaAnal>(out var comboRecords) && comboRecords != null && comboRecords.Count() > 0)
          {
            comboIndices.AddRange(comboRecords.Select(r => r.Index.Value));
          }
          var expanded = ((GsaProxy)Instance.GsaModel.Proxy).ExpandLoadCasesAndCombinations(coordinator.SenderTab.LoadCaseList, analIndices, comboIndices);
          if (expanded != null && expanded.Count() > 0)
          {
            loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Resolved load cases"));

            Instance.GsaModel.ResultCases = expanded;
            Instance.GsaModel.ResultTypes = resultsToSend.Select(rts => rts.ResultType).ToList();
          }
        }
        catch
        {

        }
      }

      TimeSpan duration = DateTime.Now - startTime;
      if (duration.Seconds > 0)
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Duration of reading GSA model into cache: " + duration.ToString(@"hh\:mm\:ss")));
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Telemetry, MessageLevel.Information, "send", "update-cache", "duration", duration.ToString(@"hh\:mm\:ss")));
      }
      startTime = DateTime.Now;

      if (Instance.GsaModel.SendResults)
      {
        try
        {
          Instance.GsaModel.Proxy.PrepareResults(Instance.GsaModel.ResultTypes);
          foreach (var rg in Instance.GsaModel.ResultGroups)
          {
            ((GsaProxy)Instance.GsaModel.Proxy).LoadResults(rg, out int numErrorRows);
          }

          percentage += 20;
          percentageProgress.Report(percentage);

          duration = DateTime.Now - startTime;
          if (duration.Seconds > 0)
          {
            loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Duration of preparing results: " + duration.ToString(@"hh\:mm\:ss")));
            loggingProgress.Report(new MessageEventArgs(MessageIntent.Telemetry, MessageLevel.Information, "send", "prepare-results", "duration", duration.ToString(@"hh\:mm\:ss")));
          }
        } catch
        {

        }
        startTime = DateTime.Now;
      }

      var numToConvert = ((GsaCache)Instance.GsaModel.Cache).NumNatives;
      statusProgress.Report("Converting");
      int numConverted = 0;
      int totalConversionPercentage = 80 - percentage;
      Instance.GsaModel.ConversionProgress = new Progress<bool>((bool success) =>
      {
        lock (perecentageProgressLock)
        {
          numConverted++;
        }
        percentageProgress.Report(percentage + Math.Round(((double)numConverted / (double)numToConvert) * totalConversionPercentage, 0));
      });

      List<Base> objs = null;
      try
      {
        objs = Commands.ConvertToSpeckle(converter);
      }
      catch (Exception ex)
      {

      }
      if (converter.ConversionErrors != null && converter.ConversionErrors.Count > 0)
      {
        foreach (var ce in converter.ConversionErrors)
        {
          loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Error, ce.Message));
          loggingProgress.Report(new MessageEventArgs(MessageIntent.TechnicalLog, MessageLevel.Error, ce, ce.Message));
        }
      }

      loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Converted cache data to Speckle"));

      duration = DateTime.Now - startTime;
      if (duration.Seconds > 0)
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Duration of conversion to Speckle: " + duration.ToString(@"hh\:mm\:ss")));
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Telemetry, MessageLevel.Information, "send", "conversion", "duration", duration.ToString(@"hh\:mm\:ss")));
      }
      startTime = DateTime.Now;

      //The converter itself can't give anything back other than Base objects, so this is the first time it can be adorned with any
      //info useful to the sending in streams
      statusProgress.Report("Sending to Server");

      var commitObj = new Base();
      foreach (var obj in objs)
      {
        var typeName = obj.GetType().Name;
        string name = "";
        if (typeName.ToLower().Contains("model"))
        {
          try
          {
            name = string.Join(" ", (string)obj["layerDescription"], "Model");
          }
          catch
          {
            name = typeName;
          }
        }
        else if (typeName.ToLower().Contains("result"))
        {
          name = "Results";
        }

        commitObj['@' + name] = obj;
      }

      var serverTransport = new ServerTransport(account, ss.Stream.id);
      var sent = await Commands.SendCommit(commitObj, ss, ((GsaModel)Instance.GsaModel).LastCommitId, serverTransport);

      if (sent)
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Successfully sent data to stream"));
        Commands.UpsertSavedReceptionStreamInfo(true, null, ss);
      }
      else
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Error, "Unable to send data to stream"));
      }

      if (ss.Errors != null && ss.Errors.Count > 0)
      {
        foreach (var se in ss.Errors)
        {
          loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Error, se.Message));
          loggingProgress.Report(new MessageEventArgs(MessageIntent.TechnicalLog, MessageLevel.Error, se, se.Message));
        }
      }

      percentageProgress.Report(100);

      duration = DateTime.Now - startTime;
      if (duration.Seconds > 0)
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Information, "Duration of sending to Speckle: " + duration.ToString(@"hh\:mm\:ss")));
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Telemetry, MessageLevel.Information, "send", "sending", "duration", duration.ToString(@"hh\:mm\:ss")));
      }
      startTime = DateTime.Now;

      Console.WriteLine("Sending complete");

      percentageProgress.Report(0);

      return true;
    }

    internal static async Task<bool> SendInitial(TabCoordinator coordinator, IProgress<StreamState> streamCreationProgress, IProgress<StreamState> streamDeletionProgress, 
      IProgress<MessageEventArgs> loggingProgress, IProgress<string> statusProgress, IProgress<double> percentageProgress)
    {
      Instance.GsaModel.StreamLayer = coordinator.SenderTab.TargetLayer;
      Instance.GsaModel.StreamSendConfig = coordinator.SenderTab.StreamContentConfig;
      Instance.GsaModel.Result1DNumPosition = coordinator.SenderTab.AdditionalPositionsFor1dElements; //end points (2) plus additional
      Instance.GsaModel.LoggingMinimumLevel = (int)coordinator.LoggingMinimumLevel;
      Instance.GsaModel.SendOnlyMeaningfulNodes = coordinator.SenderTab.SendMeaningfulNodes;
#if !DEBUG
      ((GsaProxy)Instance.GsaModel.Proxy).SetAppVersionForTelemetry(getRunningVersion().ToString());
#endif

      var account = ((GsaModel)Instance.GsaModel).Account;
      //var client = new Client(account);
      StreamState streamState;
      if (coordinator.SenderTab.SenderStreamStates == null || coordinator.SenderTab.SenderStreamStates.Count == 0)
      {
        streamState = new StreamState(account.userInfo.id, account.serverInfo.url);
        streamState.Stream = await NewStream(streamState.Client, "GSA data", "GSA data");
        streamState.IsSending = true;
        ((GsaModel)Instance.GsaModel).LastCommitId = "";
      }
      else
      {
        streamState = coordinator.SenderTab.SenderStreamStates.First();
        var branches = streamState.Client.StreamGetBranches(streamState.StreamId).Result;
        var mainBranch = branches.FirstOrDefault(b => b.name == "main");
        if (mainBranch != null && mainBranch.commits.items.Any())
        {
          ((GsaModel)Instance.GsaModel).LastCommitId = mainBranch.commits.items[0].id;
        }
      }
      
      streamCreationProgress.Report(streamState); //This will add it to the sender tab's streamState list

      await Send(coordinator, streamState, loggingProgress, statusProgress, percentageProgress);

      coordinator.SenderTab.SetDocumentName(((GsaProxy)Instance.GsaModel.Proxy).GetTitle());


      coordinator.WriteStreamInfo();

      return true;
    }

    private static async Task<Stream> NewStream(Client client, string streamName, string streamDesc)
    {
      string streamId = "";

      try
      {
        streamId = await client.StreamCreate(new StreamCreateInput()
        {
          name = streamName,
          description = streamDesc,
          isPublic = false
        });

        return await client.StreamGet(streamId);

      }
      catch (Exception e)
      {
        try
        {
          if (!string.IsNullOrEmpty(streamId))
          {
            await client.StreamDelete(streamId);
          }
        }
        catch
        {
          // POKEMON! (server is prob down)
        }
      }

      return null;
    }

    private static string UnitEnumToString(GsaUnit unit)
    {
      switch (unit)
      {
        case GsaUnit.Inches: return "in";
        case GsaUnit.Metres: return "m";
        default: return "mm";
      }
    }

    private static Version getRunningVersion()
    {
      try
      {
        return ApplicationDeployment.CurrentDeployment.CurrentVersion;
      }
      catch (Exception)
      {
        return Assembly.GetExecutingAssembly().GetName().Version;
      }
    }
  }
}
