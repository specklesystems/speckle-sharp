// https://github.com/johot/WebView2-better-bridge/blob/master/web-ui/src/betterBridge.ts
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Wpf;

namespace ConnectorRhinoWebUI
{
  public class WebView2Bridge
  {
    private readonly WebView2 webView2;
    private readonly object bridgeClass;
    private Type bridgeClassType;

    public WebView2Bridge(object bridgeClass, WebView2 webView2)
    {
      this.webView2 = webView2;
      this.bridgeClass = bridgeClass;
      this.bridgeClassType = this.bridgeClass.GetType();
    }

    public string[] GetMethods()
    {
      return bridgeClassType.GetMethods().Select(m => m.Name).ToArray();
    }

    public async Task<string> RunMethod(string methodName, string args)
    {
      var jsonDataArray = JsonSerializer.Deserialize<string[]>(args);
      var method = bridgeClassType.GetMethod(methodName);
      var parameters = method.GetParameters();

      if (parameters.Length != jsonDataArray.Length)
        throw new Exception("Wrong number of arguments, expected: " + parameters.Length + " but got: " + jsonDataArray.Length);

      var typedArgs = new object[jsonDataArray.Length];

      for (int i = 0; i < typedArgs.Length; i++)
      {
        var typedObj = JsonSerializer.Deserialize(jsonDataArray[i], parameters[i].ParameterType);
        typedArgs[i] = typedObj;
      }
      var resultTyped = method.Invoke(this.bridgeClass, typedArgs);

      // Was it an async method (in bridgeClass?)
      var resultTypedTask = resultTyped as Task;

      string resultJson = null;

      // Was the method called async?
      if (resultTypedTask == null)
      {
        // Regular method: no need to await things
        resultJson = JsonSerializer.Serialize(resultTyped);
      }
      else
      {
        // Async method:
        await resultTypedTask;

        // If has a "Result" property return the value otherwise null (Task<void> etc)
        var resultProperty = resultTypedTask.GetType().GetProperty("Result");
        var taskResult = resultProperty != null ? resultProperty.GetValue(resultTypedTask) : null;

        resultJson = JsonSerializer.Serialize(taskResult);
      }

      return resultJson;
    }

    public void SendToBrowser(string eventName, object data)
    {
      this.webView2.ExecuteScriptAsync("console.log('hello')");
      var serializedPayload = JsonSerializer.Serialize(data);
      this.webView2.ExecuteScriptAsync($"bindings.emit('{eventName}', '{serializedPayload}')");
    }

  }
}
