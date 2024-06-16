using Speckle.Connectors.Utils.Conversion;
using Speckle.Core.Models.Instances;

namespace Speckle.Connectors.Utils.Instances;

/// <summary>
/// A utility class that helps manage host application blocks in send/receive operations. This expects to be a scoped dependendency per send/receive operation.
/// POC: could be split into two - instance unpacker and instance baker.
/// </summary>
/// <typeparam name="THostObjectType">Host application object type, e.g. RhinoObject</typeparam>
/// <typeparam name="TAppIdMapValueType">The type of the applicationIdMap values.</typeparam>
public interface IInstanceObjectsManager<THostObjectType, TAppIdMapValueType>
{
  /// <summary>
  /// Given a list of host application objects, it will unpack them into TODO: comment
  /// </summary>
  /// <param name="objects"></param>
  UnpackResult<THostObjectType> UnpackSelection(IEnumerable<THostObjectType> objects);

  BakeResult BakeInstances(
    List<(string[] layerPath, IInstanceComponent obj)> instanceComponents,
    Dictionary<string, TAppIdMapValueType> applicationIdMap,
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
