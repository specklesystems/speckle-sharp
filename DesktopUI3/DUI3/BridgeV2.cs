using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Speckle.Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Speckle.Core.Logging;

namespace DUI3
{
  /// <summary>
  /// Wraps a binding class, and manages its calls from the Frontend to .NET, and sending events from .NET to the the Frontend.
  /// <para>Initially inspired by: https://github.com/johot/WebView2-better-bridge</para>
  /// </summary>
  [ClassInterface(ClassInterfaceType.AutoDual)]
  [ComVisible(true)]
  public class BrowserBridge : IBridge
  {
    /// <summary>
    /// The name under which we expect the frontend to hoist this bindings class to the global scope.
    /// e.g., `receiveBindings` should be available as `window.receiveBindings`. 
    /// </summary>
    public string FrontendBoundName { get; }
    public object Browser { get; }
    public IBinding Binding { get; }
    public Action<string> ExecuteScriptAsync { get; set; }
    public Action ShowDevToolsAction { get; set; }
    private Type BindingType { get; set; }
    private Dictionary<string, MethodInfo> BindingMethodCache { get; set; }
    private JsonSerializerSettings _serializerOptions = DUI3.Utils.SerializationSettingsFactory.GetSerializerSettings();
    private ActionBlock<RunMethodArgs> _actionBlock;

    private struct RunMethodArgs
    {
      public string MethodName;
      public string RequestId;
      public string MethodArgs;
    }
    
    /// <summary>
    /// Creates a new bridge.
    /// </summary>
    /// <param name="browser">The host browser instance.</param>
    /// <param name="binding">The actual binding class.</param>
    /// <param name="executeScriptAsync">A simple action that does the browser's version of executeScriptAsync(string).</param>
    public BrowserBridge(object browser, IBinding binding, Action<string> executeScriptAsync, Action showDevToolsAction)
    {
      FrontendBoundName = binding.Name;
      Browser = browser;
      Binding = binding;
      
      BindingType = Binding.GetType(); 
      BindingMethodCache = new Dictionary<string, MethodInfo>();
      // Note: we need to filter out getter and setter methods here because they are not really nicely
      // supported across browsers, hence the !method.IsSpecialName. 
      foreach(var m in BindingType.GetMethods().Where(method => !method.IsSpecialName))
      {
        BindingMethodCache[m.Name] = m;
      }

      Binding.Parent = this;

      ExecuteScriptAsync = executeScriptAsync;
      ShowDevToolsAction = showDevToolsAction;
      
      // Whenever the ui will call run method inside .net, it will post a message to this action block.
      // This conveniently executes the code outside the UI thread and does not block during long operations (such as sending).
      _actionBlock = new ActionBlock<RunMethodArgs>(
        args => ExecuteMethod(args.MethodName, args.RequestId, args.MethodArgs),
        new ExecutionDataflowBlockOptions
        {
          MaxDegreeOfParallelism = 1000,
          CancellationToken = new CancellationTokenSource(TimeSpan.FromHours(3)).Token // Not sure we need such a long time.
        });
    }

    /// <summary>
    /// Used by the Frontend bridge logic to understand which methods are available.
    /// </summary>
    /// <returns></returns>
    public string[] GetBindingsMethodNames()
    {
      return BindingMethodCache.Keys.ToArray();
    }
    
    /// <summary>
    /// This method posts the requested call to our action block executor. 
    /// </summary>
    /// <param name="methodName"></param>
    /// <param name="requestId"></param>
    /// <param name="args"></param>
    public void RunMethod(string methodName, string requestId, string args)
    {
      _actionBlock.Post(new RunMethodArgs { MethodName = methodName, RequestId = requestId, MethodArgs = args });
    }
    
    /// <summary>
    /// Used by the action block to invoke the actual method called by the UI. 
    /// </summary>
    /// <param name="methodName"></param>
    /// <param name="requestId"></param>
    /// <param name="args"></param>
    /// <exception cref="SpeckleException"></exception>
    private void ExecuteMethod(string methodName, string requestId, string args)
    {
      // Note: You might be tempted to make this method async Task<string> to prevent the task.Wait() below. 
      // Do not do that! Cef65 doesn't like waiting for async .NET methods.
      // Note: we have this pokemon catch 'em all here because throwing errors in .NET is 
      // very risky, and we might crash the host application. Behaviour seems also to differ
      // between various browser controls (e.g.: cefsharp handles things nicely - basically 
      // passing back the exception to the browser, but webview throws an access violation
      // error that kills Rhino.). 
      try
      {
        if (!BindingMethodCache.ContainsKey(methodName))
          throw new SpeckleException($"Cannot find method {methodName} in bindings class {BindingType.AssemblyQualifiedName}.");

        var method = BindingMethodCache[methodName];
        var parameters = method.GetParameters();
        var jsonArgsArray = JsonConvert.DeserializeObject<string[]>(args);

        if (parameters.Length != jsonArgsArray.Length)
          throw new SpeckleException($"Wrong number of arguments when invoking binding function {methodName}, expected {parameters.Length}, but got {jsonArgsArray.Length}.");

        var typedArgs = new object[jsonArgsArray.Length];

        for (int i = 0; i < typedArgs.Length; i++)
        {
          var ccc = JsonConvert.DeserializeObject(jsonArgsArray[i], parameters[i].ParameterType, _serializerOptions);
          typedArgs[i] = ccc;
        }

        var resultTyped = method.Invoke(Binding, typedArgs);

        // Was it an async method (in bridgeClass?)
        var resultTypedTask = resultTyped as Task;

        string resultJson;

        // Was the method called async?
        if (resultTypedTask == null)
        {
          // Regular method: no need to await things
          resultJson = JsonConvert.SerializeObject(resultTyped, _serializerOptions);
        }
        else // It's an async call
        {
          // See note at start of function. Do not asyncify!
          resultTypedTask.Wait();

          // If has a "Result" property return the value otherwise null (Task<void> etc)
          var resultProperty = resultTypedTask.GetType().GetProperty("Result");
          var taskResult = resultProperty != null ? resultProperty.GetValue(resultTypedTask) : null;
          resultJson = JsonConvert.SerializeObject(taskResult, _serializerOptions);
        }
        
        NotifyUIMethodCallResultReady(requestId, resultJson);
      }
      catch (Exception e)
      {
        // TODO: properly log the exeception.
        var serializedError = JsonConvert.SerializeObject(new { Error = e.Message, InnerError = e.InnerException?.Message }, _serializerOptions);
        
        NotifyUIMethodCallResultReady(requestId, serializedError);
      }
    }
    
    /// <summary>
    /// NOTE: This method suffers from a limitation: returning strings to the ui via a script invocation is not the same
    /// as returning strings from a method. The more reliable approach was the latter. Keeping it here for the sake of
    /// "do not do things this way" demo. 
    /// </summary>
    /// <param name="requestId"></param>
    /// <param name="serializedData"></param>
    private void ReturnResultToBinding_old(string requestId, string serializedData = null)
    {
      string script=  $"{FrontendBoundName}.receiveResponse('{requestId}', '{serializedData}')"; // sending the string this way makes for some strange deserialization issues. 
      ExecuteScriptAsync(script);
    }

    private Dictionary<string, string> _resultsStore = new();
    /// <summary>
    /// Notifies the UI that the method call is ready. We do not give the result back to the ui here via ExecuteScriptAsync
    /// because of limitations we discovered along the way (e.g, / chars need to be escaped). 
    /// </summary>
    /// <param name="requestId"></param>
    /// <param name="serializedData"></param>
    private void NotifyUIMethodCallResultReady(string requestId, string serializedData = null)
    {
      _resultsStore[requestId] = serializedData;
      string script=  $"{FrontendBoundName}.responseReady('{requestId}')";
      ExecuteScriptAsync(script);
    }
    
    /// <summary>
    /// Called by the ui to get back the serialized result of the method. See comments above for why.
    /// </summary>
    /// <param name="requestId"></param>
    /// <returns></returns>
    public string GetCallResult(string requestId)
    {
      var res = _resultsStore[requestId];
      _resultsStore.Remove(requestId);
      return res;
    }
    
    /// <summary>
    /// Notifies the Frontend about something by doing the browser specific way for `browser.ExecuteScriptAsync("window.FrontendBoundName.on(eventName, etc.)")`. 
    /// </summary>
    /// <param name="eventData"></param>
    public void SendToBrowser(string eventName, object data = null)
    {
      string script;
      if (data != null)
      {
        var payload = JsonConvert.SerializeObject(data, _serializerOptions);
        script = $"{FrontendBoundName}.emit('{eventName}', '{payload}')";
      } 
      else
      {
        script = $"{FrontendBoundName}.emit('{eventName}')";
      }
      ExecuteScriptAsync(script);
    }

    /// <summary>
    /// Shows the dev tools. This is currently only needed for CefSharp - other browser
    /// controls allow for right click + inspect. 
    /// </summary>
    public void ShowDevTools()
    {
      ShowDevToolsAction();
    }

    public void OpenUrl(string url)
    {
      System.Diagnostics.Process.Start(url);
    }
  }

}
