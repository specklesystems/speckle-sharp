using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Speckle.Newtonsoft.Json;

namespace Archicad.Communication
{
  internal static class HttpCommandExecutor
  {
    #region --- Functions ---

    private static string SerializeRequest<TRequest>(TRequest request)
    {
      JsonSerializerSettings settings = new JsonSerializerSettings
      {
        NullValueHandling = NullValueHandling.Ignore,
        Context = new StreamingContext(StreamingContextStates.Remoting)
      };

      return JsonConvert.SerializeObject(request, settings);
    }

    private static TResponse DeserializeResponse<TResponse>(string obj)
    {
      JsonSerializerSettings settings = new JsonSerializerSettings
      {
        Context = new StreamingContext(StreamingContextStates.Remoting)
      };

      return JsonConvert.DeserializeObject<TResponse>(obj, settings);
    }

    public static async Task<TResult> Execute<TParameters, TResult>(string commandName, TParameters parameters) where TParameters : class where TResult : class
    {
      AddOnCommandRequest<TParameters> request = new AddOnCommandRequest<TParameters>(commandName, parameters);

      string requestMsg = SerializeRequest(request);
      //Console.WriteLine(requestMsg);
      string responseMsg = await ConnectionManager.Instance.Send(requestMsg);
      //Console.WriteLine(responseMsg);
      AddOnCommandResponse<TResult> response = DeserializeResponse<AddOnCommandResponse<TResult>>(responseMsg);

      // TODO
      //if (!response.Succeeded)
      //{
      //	throw new CommandFailedException (response.ErrorStatus.Code, response.ErrorStatus.Message);
      //}

      return response.Result;
    }

    #endregion
  }
}
