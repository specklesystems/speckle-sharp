using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.DesignScript.Runtime;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace Speckle.ConnectorDynamo.Functions;

public static class Auto
{
  /// <summary>
  /// Send data to a Speckle server automatically, withiut having to click on a send button
  /// </summary>
  /// <param name="data">The data to send</param>
  /// <param name="stream">The stream or streams to send to</param>
  /// <param name="message">Commit message. If left blank, one will be generated for you.</param>
  /// <param name="enabled">Enable or disable this node</param>
  /// <returns></returns>
  public static List<string> AutoSend(
    [ArbitraryDimensionArrayImport] object data,
    object stream,
    string message = "",
    bool enabled = true
  )
  {
    var result = new List<string>();
    if (!enabled)
    {
      return result;
    }

    var converter = new BatchConverter();
    converter.OnError += (sender, args) => throw args.Error;

    var @base = converter.ConvertRecursivelyToSpeckle(data);

    // .Result is Thread Blocking inRevit
    Task.Run(() =>
      {
        var transportsDict = Utils.TryConvertInputToTransport(stream);
        result = Functions.Send(
          @base,
          transportsDict.Keys.ToList(),
          new System.Threading.CancellationToken(),
          transportsDict,
          message
        );
      })
      .Wait();

    return result;
  }

  /// <summary>
  /// Receive data from a Speckle server automatically, without having to click on a receive button
  /// </summary>
  /// <param name="stream">The stream or streams to receive from</param>
  /// <param name="enabled">Enable or disable this node</param>
  /// <returns></returns>
  public static Dictionary<string, object> AutoReceive(object stream, bool enabled = true)
  {
    var result = new Dictionary<string, object>();

    if (!enabled)
    {
      return result;
    }

    StreamWrapper sw = null;

    // .Result is Thread Blocking inRevit
    Task.Run(() =>
      {
        //try parse as streamWrapper
        if (stream is StreamWrapper wrapper)
        {
          sw = wrapper;
        }
        //try parse as Url
        else if (stream is string s)
        {
          try
          {
            sw = new StreamWrapper(s);
          }
          catch (Exception ex) when (ex is NotSupportedException or SpeckleException)
          {
            // ignored
          }
        }

        result = Functions.Receive(sw, new CancellationToken());
      })
      .Wait();

    return result;
  }
}
