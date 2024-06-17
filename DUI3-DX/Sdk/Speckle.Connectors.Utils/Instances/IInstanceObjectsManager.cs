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
  /// Given a list of host application objects, it will unpack them into atomic objects, instance proxies and instance proxy definitions.
  /// </summary>
  /// <param name="objects">Raw selection from the host application.</param>
  UnpackResult<THostObjectType> UnpackSelection(IEnumerable<THostObjectType> objects);

  /// <summary>
  /// Will bake a set of instance components (instances and instance definitions) in the host app.
  /// </summary>
  /// <param name="instanceComponents"></param>
  /// <param name="applicationIdMap"></param>
  /// <param name="baseLayerName"></param>
  /// <param name="onOperationProgressed"></param>
  /// <returns></returns>
  BakeResult BakeInstances(
    List<(string[] layerPath, IInstanceComponent obj)> instanceComponents,
    Dictionary<string, TAppIdMapValueType> applicationIdMap,
    string baseLayerName,
    Action<string, double?>? onOperationProgressed
  );

  /// <summary>
  /// <para>Cleans up previously baked instances and associated definitions containing the `namePrefix` in their name.</para>
  /// <para>Note: this is based on the convention that all defintions have their name set to a model based prefix.</para>
  /// </summary>
  /// <param name="namePrefix">The name prefix to search and delete by.</param>
  void PurgeInstances(string namePrefix);
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
