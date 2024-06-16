using Speckle.Connectors.Utils.Conversion;
using Speckle.Core.Models.Instances;

namespace Speckle.Connectors.Utils.Instances;

/// <summary>
/// A utility class that helps manage host application blocks in send/receive operations. This expects to be a transient dependendency.
/// POC: could be split into two - instance unpacker and instance baker.
/// </summary>
/// <typeparam name="T">Host application object type, e.g. RhinoObject</typeparam>
public interface IInstanceObjectsManager<T>
{
  /// <summary>
  /// Given a list of host application objects, it will unpack them into TODO: comment
  /// </summary>
  /// <param name="objects"></param>
  UnpackResult<T> UnpackSelection(IEnumerable<T> objects);

  BakeResult BakeInstances(
    List<(string[] layerPath, IInstanceComponent obj)> instanceComponents,
    Dictionary<string, List<string>> applicationIdMap,
    string baseLayerName,
    Action<string, double?>? onOperationProgressed
  );
}

public record UnpackResult<T>(
  List<T> AtomicObjects,
  Dictionary<string, InstanceProxy> InstanceProxies,
  List<InstanceDefinitionProxy> InstanceDefinitionProxies
);

public record BakeResult(
  List<string> CreatedInstanceIds,
  List<string> ConsumedObjectIds,
  List<ReceiveConversionResult> InstanceConversionResults
);
