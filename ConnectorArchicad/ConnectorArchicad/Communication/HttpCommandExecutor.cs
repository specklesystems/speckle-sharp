using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication;

internal static class HttpCommandExecutor
{
  #region --- Functions ---

  private static string SerializeRequest<TRequest>(TRequest request)
  {
    JsonSerializerSettings settings =
      new()
      {
        NullValueHandling = NullValueHandling.Ignore,
        Context = new StreamingContext(StreamingContextStates.Remoting)
      };

    return JsonConvert.SerializeObject(request, settings);
  }

  private static TResponse DeserializeResponse<TResponse>(string obj)
  {
    JsonSerializerSettings settings = new() { Context = new StreamingContext(StreamingContextStates.Remoting) };

    return JsonConvert.DeserializeObject<TResponse>(obj, settings);
  }

  public static async Task<TResult> Execute<TParameters, TResult>(string commandName, TParameters parameters)
    where TParameters : class
    where TResult : class
  {
    var context = Archicad.Helpers.Timer.Context.Peek;
    using (
      context?.cumulativeTimer?.Begin(
        ConnectorArchicad.Properties.OperationNameTemplates.HttpCommandExecute,
        commandName
      )
    )
    {
      bool log = false;

      AddOnCommandRequest<TParameters> request = new(commandName, parameters);

      string requestMsg = SerializeRequest(request);

      if (log)
      {
        Console.WriteLine(requestMsg);
      }

      string responseMsg;

      using (
        context?.cumulativeTimer?.Begin(ConnectorArchicad.Properties.OperationNameTemplates.HttpCommandAPI, commandName)
      )
      {
        responseMsg = await ConnectionManager.Instance.Send(requestMsg);
      }

      if (log)
      {
        Console.WriteLine(responseMsg);
      }

      AddOnCommandResponse<TResult> response = DeserializeResponse<AddOnCommandResponse<TResult>>(responseMsg);

      // TODO
      //if (!response.Succeeded)
      //{
      //	throw new CommandFailedException (response.ErrorStatus.Code, response.ErrorStatus.Message);
      //}

      return response.Result;
    }
  }

  #endregion
}
