using System.DoubleNumerics;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Connectors.Autocad.HostApp.Extensions;
using Speckle.Connectors.Autocad.Operations.Send;
using Speckle.Connectors.Utils.Instances;
using Speckle.Core.Models.Instances;

namespace Speckle.Connectors.Autocad.HostApp;

public class AutocadInstanceObjectManager : IInstanceObjectsManager<AutocadRootObject>
{
  private Dictionary<string, InstanceProxy> InstanceProxies { get; set; } = new();
  private Dictionary<string, List<InstanceProxy>> InstanceProxiesByDefinitionId { get; set; } = new();
  private Dictionary<string, InstanceDefinitionProxy> DefinitionProxies { get; set; } = new();
  private Dictionary<string, AutocadRootObject> FlatAtomicObjects { get; set; } = new();

  public UnpackResult<AutocadRootObject> UnpackSelection(IEnumerable<AutocadRootObject> objects)
  {
    // POC: Dim enjoys controlling transactions clearly, it's not immediate how we're dealing with stuff in TransactionContext
    // where does it start, can we batch more stuff in it?, performance implications? document locking?
    using var transaction = Application.DocumentManager.CurrentDocument.Database.TransactionManager.StartTransaction();

    foreach (var obj in objects)
    {
      // TODO: maybe get the dynamic blocks out as "exploded", or handle them separately anyway - for now excluding
      // let's solve the simple case for now
      // TODO: idea: dynamic blocks could fallback to a rhino "group" - could we fake it with a displayValue[] hack
      if (obj.Root is BlockReference blockReference && !blockReference.IsDynamicBlock)
      {
        UnpackInstance(blockReference, 0, transaction);
      }

      FlatAtomicObjects[obj.ApplicationId] = obj;
    }
    return new(FlatAtomicObjects.Values.ToList(), InstanceProxies, DefinitionProxies.Values.ToList());
  }

  private void UnpackInstance(BlockReference instance, int depth, Transaction transaction)
  {
    var instanceIdString = instance.Handle.Value.ToString();
    var definitionId = instance.BlockTableRecord;

    InstanceProxies[instanceIdString] = new InstanceProxy()
    {
      applicationId = instanceIdString,
      DefinitionId = definitionId.ToString(),
      MaxDepth = depth,
      Transform = GetMatrix(instance.BlockTransform.ToArray()),
      Units = Application.DocumentManager.CurrentDocument.Database.Insunits.ToSpeckleString()
    };

    // For each block instance that has the same definition, we need to keep track of the "maximum depth" at which is found.
    // This will enable on receive to create them in the correct order (descending by max depth, interleaved definitions and instances).
    // We need to interleave the creation of definitions and instances, as some definitions may depend on instances.
    if (
      !InstanceProxiesByDefinitionId.TryGetValue(
        definitionId.ToString(),
        out List<InstanceProxy> instanceProxiesWithSameDefinition
      )
    )
    {
      instanceProxiesWithSameDefinition = new List<InstanceProxy>();
      InstanceProxiesByDefinitionId[definitionId.ToString()] = instanceProxiesWithSameDefinition;
    }

    // We ensure that all previous instance proxies that have the same definition are at this max depth. I kind of have a feeling this can be done more elegantly, but YOLO
    foreach (var instanceProxy in instanceProxiesWithSameDefinition)
    {
      instanceProxy.MaxDepth = depth;
    }

    instanceProxiesWithSameDefinition.Add(InstanceProxies[instanceIdString]);

    if (DefinitionProxies.TryGetValue(definitionId.ToString(), out InstanceDefinitionProxy value))
    {
      value.MaxDepth = depth;
      return; // exit fast - we've parsed this one so no need to go further
    }

    var definition = (BlockTableRecord)transaction.GetObject(definitionId, OpenMode.ForRead);
    // definition.Origin
    var definitionProxy = new InstanceDefinitionProxy()
    {
      applicationId = definitionId.ToString(),
      Objects = new(),
      MaxDepth = depth,
      ["name"] = definition.Name,
      ["comments"] = definition.Comments,
      ["units"] = definition.Units // ? not sure needed?
    };

    // Go through each definition object
    foreach (ObjectId id in definition)
    {
      var obj = transaction.GetObject(id, OpenMode.ForRead);
      var handleIdString = obj.Handle.Value.ToString();
      definitionProxy.Objects.Add(handleIdString);

      if (obj is BlockReference blockReference && !blockReference.IsDynamicBlock)
      {
        UnpackInstance(blockReference, depth + 1, transaction);
      }
      FlatAtomicObjects[handleIdString] = new(obj, handleIdString);
    }

    DefinitionProxies[definitionId.ToString()] = definitionProxy;
  }

  // TODO: units? i think not here
  private Matrix4x4 GetMatrix(double[] t)
  {
    return new Matrix4x4(
      t[0],
      t[1],
      t[2],
      t[3],
      t[4],
      t[5],
      t[6],
      t[7],
      t[8],
      t[9],
      t[10],
      t[11],
      t[12],
      t[13],
      t[14],
      t[15]
    );
  }
}
