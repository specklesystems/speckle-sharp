using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Transports;
using Speckle.GSA.API;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Deployment.Application;
using System.Threading.Tasks;
using Speckle.GSA.API.GwaSchema;
using Speckle.ConnectorGSA.Proxy;

namespace ConnectorGSA
{
  public class Headless
  {
    //public static Func<string, string, SpeckleInterface.IStreamReceiver> streamReceiverCreationFn
    //  = ((url, token) => new SpeckleInterface.StreamReceiver(url, token, ProgressMessenger));
    ////public static Func<string, string, SpeckleInterface.IStreamSender> streamSenderCreationFn = ((url, token) => new SpeckleInterface.StreamSender(url, token, ProgressMessenger));
    //public static Func<string, string, SpeckleInterface.IStreamSender> streamSenderCreationFn;
    //public static IProgress<MessageEventArgs> loggingProgress = new Progress<MessageEventArgs>();
    //public static SpeckleInterface.ISpeckleAppMessenger ProgressMessenger = new ProgressMessenger(loggingProgress);

    //private Dictionary<string, string> arguments = new Dictionary<string, string>();
    private string cliMode = "";

    public string EmailAddress { get; private set; }
    public string RestApi { get; private set; }
    public string ApiToken { get; private set; }

    private UserInfo userInfo;

    public bool RunCLI(params string[] args)
    {
      CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
      CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

      var argPairs = new Dictionary<string, string>();

      var kit = KitManager.GetDefaultKit();
      var converter = kit.LoadConverter(Applications.GSA);

      IProgress<MessageEventArgs> loggingProgress = new Progress<MessageEventArgs>();
      //TO DO: add logging to console

      //A simplified one just for use by the proxy class
      var proxyLoggingProgress = new Progress<string>();
      proxyLoggingProgress.ProgressChanged += (object o, string e) =>
      {
        loggingProgress.Report(new MessageEventArgs(MessageIntent.Display, MessageLevel.Error, e));
        loggingProgress.Report(new MessageEventArgs(MessageIntent.TechnicalLog, MessageLevel.Error, e));
      };

      Instance.GsaModel = new GsaModel();

      cliMode = args[0];
      if (cliMode == "-h")
      {
        Console.WriteLine("\n");
        Console.WriteLine("Usage: ConnectorGSA.exe <command>\n\n" +
          "where <command> is one of: receiver, sender\n\n");
        Console.Write("ConnectorGSA.exe <command> -h\thelp on <command>\n");
        return true;
      }
      if (cliMode != "receiver" && cliMode != "sender")
      {
        Console.WriteLine("Unable to parse command");
        return false;
      }

      var sendReceive = (cliMode == "receiver") ? SendReceive.Receive : SendReceive.Send;

      #region display_h_info
      if (sendReceive == SendReceive.Receive && argPairs.ContainsKey("h"))
      {
        Console.WriteLine("\n");
        Console.WriteLine("Usage: ConnectorGSA.exe receiver\n");
        Console.WriteLine("\n");
        Console.Write("Required arguments:\n");
        Console.Write("--server <server>\t\tAddress of Speckle server\n");
        Console.Write("--email <email>\t\t\tEmail of account\n");
        Console.Write("--token <token>\t\tJWT token\n");
        Console.Write("--file <path>\t\t\tFile to save to. If file does not exist, a new one will be created\n");
        Console.Write("--streamIDs <streamIDs>\t\tComma-delimited ID of streams to be received\n");
        Console.WriteLine("\n");
        Console.Write("Optional arguments:\n");
        Console.Write("--nodeAllowance <distance>\tMax distance before nodes are not merged\n");
        return true;
      }
      else if (sendReceive == SendReceive.Send && argPairs.ContainsKey("h"))
      {
        Console.WriteLine("\n");
        Console.WriteLine("Usage: ConnectorGSA.exe sender\n");
        Console.WriteLine("\n");
        Console.Write("Required arguments:\n");
        Console.Write("--server <server>\t\tAddress of Speckle server\n");
        Console.Write("--email <email>\t\t\tEmail of account\n");
        Console.Write("--token <token>\t\tJWT token\n");
        Console.Write("--file <path>\t\t\tFile path to open\n");        
        Console.WriteLine("\n");
        Console.Write("Optional arguments:\n");
        Console.Write("--saveAs <path>\t\t\tFile path to save file with stream information.  Default is to use file\n");
        Console.Write("--designLayerOnly\t\tIgnores analysis information.  Default is to send all data from both layers\n");
        Console.Write("--sendAllNodes\t\t\tSend all nodes in model. Default is to send only 'meaningful' nodes\n");
        Console.Write("--result <options>\t\tType of result to send. Each input should be in quotation marks. Comma-delimited\n");
        Console.Write("--resultCases <cases>\t\tCases to extract results from. Comma-delimited\n");
        Console.Write("--resultInLocalAxis\t\tSend results calculated at the local axis. Default is global\n");
        Console.Write("--result1DNumPosition <num>\tNumber of additional result points within 1D elements\n");
        return true;
      }
      #endregion

      #region create_argpairs
      for (int index = 1; index < args.Length; index += 2)
      {
        string arg = args[index].Replace("-", "");
        if (args.Length <= index + 1 || args[index + 1].StartsWith("-"))
        {
          argPairs.Add(arg, "true");
          index--;
        }
        else
        {
          argPairs.Add(arg, args[index + 1].Trim(new char[] { '"' }));
        }
      }

      foreach (var a in (new [] { "server", "file" }))
      {
        if (!argPairs.ContainsKey(a))
        {
          Console.WriteLine("Missing -" + a + " argument");
          return false;
        }
      }
      #endregion

      // Login
      if (argPairs.ContainsKey("email"))
      {
        EmailAddress = argPairs["email"];
      }
      if (argPairs.ContainsKey("server"))
      {
        RestApi = argPairs["server"];
      }
      if (argPairs.ContainsKey("token"))
      {
        ApiToken = argPairs["token"];
      }

      Account account;
      if (string.IsNullOrEmpty(RestApi) || string.IsNullOrEmpty(ApiToken))
      {
        account = AccountManager.GetDefaultAccount();
        userInfo = account.userInfo;
      }
      else
      {
        userInfo = AccountManager.GetUserInfo(ApiToken, RestApi).Result;
        account = AccountManager.GetAccounts().FirstOrDefault(a => a.userInfo.id == userInfo.id);
      }
      
      var client = new Client(account);

      #region file
      // GSA File
      var fileArg = argPairs["file"];
      var filePath = fileArg.StartsWith(".") ? Path.Combine(AssemblyDirectory, fileArg) : fileArg;
      
      //If receiving, then it's valid for a file name not to exist - in this case, it's the file name that a new file should be saved as
      if (!File.Exists(filePath) && sendReceive == SendReceive.Send)
      {
        Console.WriteLine("Could not locate file: " + filePath);
        //sending needs the file to exist
        return false;
      }

      var saveAsFilePath = (argPairs.ContainsKey("saveAs")) ? argPairs["saveAs"] : filePath;

      if (sendReceive == SendReceive.Receive)
      {
        ((GsaProxy)Instance.GsaModel.Proxy).NewFile(false);

        //Instance.GsaModel.Messenger.Message(MessageIntent.Display, MessageLevel.Information, "Created new file.");

        //Ensure this new file has a file name, and internally sets the file name in the proxy
        ((GsaProxy)Instance.GsaModel.Proxy).SaveAs(saveAsFilePath);
      }
      else
      {
        Commands.OpenFile(filePath, false);
      }
      #endregion

      var calibrateNodeAtTask = Task.Run(() => Instance.GsaModel.Proxy.CalibrateNodeAt());
      calibrateNodeAtTask.Wait();

      if (!ArgsToSettings(sendReceive, argPairs))
      {
        return false;
      }

      var streamStates = new List<StreamState>();
      bool cliResult = false;
      if (sendReceive == SendReceive.Receive)
      {
        var streamIds = argPairs["streamIDs"].Split(new char[] { ',' });

        //There seem to be some issues with HTTP requests down the line if this is run on the initial (UI) thread, so this ensures it runs on another thread
        cliResult = Task.Run(() =>
        {
          //Load data to cause merging
          Commands.LoadDataFromFile(null); //Ensure all nodes

          foreach (var streamId in streamIds)
          {
            var streamState = new StreamState(userInfo.id, RestApi) 
            { 
              Stream = new Speckle.Core.Api.Stream() { id = streamId }, 
              IsReceiving = true 
            };
            streamState.RefreshStream(loggingProgress).Wait();
            streamState.Stream.branch = client.StreamGetBranches(streamId, 1).Result.First();
            var commitId = streamState.Stream.branch.commits.items.FirstOrDefault().referencedObject;
            var transport = new ServerTransport(streamState.Client.Account, streamState.Stream.id);

            Commands.Receive(commitId, streamState, transport, converter.CanConvertToNative).Wait();

            streamStates.Add(streamState);
          }

          Commands.ConvertToNative(converter, loggingProgress);

          //The cache is filled with natives
          if (Instance.GsaModel.Cache.GetNatives(out var gsaRecords))
          {
            ((GsaProxy)Instance.GsaModel.Proxy).WriteModel(gsaRecords, null, Instance.GsaModel.StreamLayer);
          }

          Console.WriteLine("Receiving complete");

          return true;
        }).Result;
      }
      else //Send
      {
        //There seem to be some issues with HTTP requests down the line if this is run on the initial (UI) thread, so this ensures it runs on another thread
        cliResult = Task.Run(() =>
        {
          if (Instance.GsaModel.SendResults)
          {
            Instance.GsaModel.Proxy.PrepareResults(Instance.GsaModel.ResultTypes);
            foreach (var rg in Instance.GsaModel.ResultGroups)
            {
              ((GsaProxy)Instance.GsaModel.Proxy).LoadResults(rg, out int numErrorRows);
            }
          }

          Commands.LoadDataFromFile(proxyLoggingProgress); //Ensure all nodes

          var objs = Commands.ConvertToSpeckle(converter);

          objs.Reverse();

          //The converter itself can't give anything back other than Base objects, so this is the first time it can be adorned with any
          //info useful to the sending in streams

          var commitObj = new Speckle.Core.Models.Base();
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

            commitObj[name] = obj;
          }


          var stream = NewStream(client, "GSA data", "GSA data").Result;
          var streamState = new StreamState(userInfo.id, RestApi) { Stream = stream, IsSending = true };
          streamStates.Add(streamState);

          var serverTransport = new ServerTransport(account, streamState.Stream.id);
          var sent = Commands.SendCommit(commitObj, streamState, "", serverTransport).Result;

          Console.WriteLine("Sending complete");
          return true;
        }).Result;
      }

      Commands.UpsertSavedReceptionStreamInfo(true, null, streamStates.ToArray());
      ((GsaProxy)Instance.GsaModel.Proxy).SaveAs(saveAsFilePath);
      ((GsaProxy)Instance.GsaModel.Proxy).Close();

      return cliResult;
    }

    private async Task<Speckle.Core.Api.Stream> NewStream(Client client, string streamName, string streamDesc)
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

    private enum SendReceive
    {
      Send,
      Receive
    }

    private bool ArgsToSettings(SendReceive sendReceive, Dictionary<string, string> argPairs)
    {
      //This will create the logger
      Instance.GsaModel.LoggingMinimumLevel = 4; //Debug
      Instance.GsaModel.StreamLayer = GSALayer.Both;  //Unless overridden below
      //TO DO: enable is as a command line argument
      Instance.GsaModel.Units = "m";

      if (sendReceive == SendReceive.Receive)
      {
        if (!argPairs.ContainsKey("streamIDs"))
        {
          Console.WriteLine("Missing -streamIDs argument");
          return false;
        }

        if (argPairs.ContainsKey("nodeAllowance") && double.TryParse(argPairs["nodeAllowance"], out double nodeAllowance))
        {
          Instance.GsaModel.CoincidentNodeAllowance = nodeAllowance;
        }
      }
      else if (sendReceive == SendReceive.Send)
      {
        if (argPairs.ContainsKey("sendAllNodes"))
        {
          Instance.GsaModel.SendOnlyMeaningfulNodes = false;
        }

        if (argPairs.ContainsKey("designLayerOnly"))
        {
          Instance.GsaModel.StreamLayer = GSALayer.Design;
        }
        else
        {
          Instance.GsaModel.StreamLayer = GSALayer.Both;
        }

        if (argPairs.ContainsKey("result"))
        {
          Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelAndResults;

          var resultStrings = argPairs["result"].Split(new char[] { ',' }).Select(x => x.Replace("\"", ""));
          Instance.GsaModel.ResultTypes = new List<ResultType>();
          foreach (var rs in resultStrings)
          {
            if (Enum.TryParse(rs.Replace(" ", ""), true, out ResultType rt) && !Instance.GsaModel.ResultTypes.Contains(rt))
            {
              Instance.GsaModel.ResultTypes.Add(rt);
            }
          }

          if (argPairs.ContainsKey("resultCases"))
          {
            var validPrefixes = new[] { 'A', 'C' };
            var cases = argPairs["resultCases"].Split(',').Select(c => c.Trim()).Where(c => validPrefixes.Any(vp => vp == char.ToUpper(c[0]))).ToList();
            if (cases != null && cases.Count > 0)
            {
              Instance.GsaModel.ResultCases = cases;
            }
          }

          if (argPairs.ContainsKey("resultInLocalAxis"))
          {
            Instance.GsaModel.ResultInLocalAxis = true;
          }
          if (argPairs.ContainsKey("result1DNumPosition"))
          {
            try
            {
              Instance.GsaModel.Result1DNumPosition = Convert.ToInt32(argPairs["result1DNumPosition"]);
            }
            catch { }
          }
        }
        else
        {
          Instance.GsaModel.StreamSendConfig = StreamContentConfig.ModelOnly;
        }
      }
      return true;
    }

    #region Log
    [DllImport("Kernel32.dll")]
    public static extern bool AttachConsole(int processId);

    /// <summary>
    /// Message handler.
    /// </summary>
    private void ProcessMessage(object sender, MessageEventArgs e)
    {
      if (e.Level == MessageLevel.Debug || e.Level == MessageLevel.Information)
      {
        Console.WriteLine("[" + DateTime.Now.ToString("h:mm:ss tt") + "] " + string.Join(" ", e.MessagePortions.Where(mp => !string.IsNullOrEmpty(mp))));
      }
      else
      {
        Console.WriteLine("[" + DateTime.Now.ToString("h:mm:ss tt") + "] ERROR: " + string.Join(" ", e.MessagePortions.Where(mp => !string.IsNullOrEmpty(mp))));
      }
    }

    /// <summary>
    /// Change status handler.
    /// </summary>
    private void ChangeStatus(object sender, StatusEventArgs e)
    {
      if (e.Percent >= 0 & e.Percent <= 100)
      {
        Console.WriteLine("[" + DateTime.Now.ToString("h:mm:ss tt") + "] " + e.Name + " : " + e.Percent);
      }
      else
      {
        Console.WriteLine("[" + DateTime.Now.ToString("h:mm:ss tt") + "] " + e.Name + "...");
      }
    }
    #endregion

    private Version getRunningVersion()
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

    private static string AssemblyDirectory
    {
      get
      {
        string codeBase = Assembly.GetExecutingAssembly().CodeBase;
        UriBuilder uri = new UriBuilder(codeBase);
        string path = Uri.UnescapeDataString(uri.Path);
        return Path.GetDirectoryName(path);
      }
    }
  }

}
