using Autodesk.AutoCAD.DatabaseServices;
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
    var instanceId = instance.Id.ToString();
    var defintionId = instance.BlockTableRecord;

    // TODO: magical depth assignment for:
    // - instances
    // - defs



    if (DefinitionProxies.TryGetValue(defintionId.ToString(), out InstanceDefinitionProxy value))
    {
      value.MaxDepth = depth;
      return; // exit fast - we've parsed this one so no need to go further
    }

    var definition = (BlockTableRecord)transaction.GetObject(defintionId, OpenMode.ForRead);
    // definition.Origin
    var definitionProxy = new InstanceDefinitionProxy()
    {
      applicationId = defintionId.ToString(),
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
      if (obj is BlockReference blockReference && !blockReference.IsDynamicBlock)
      {
        UnpackInstance(blockReference, depth + 1, transaction);
      }
      FlatAtomicObjects[id.ToString()] = new(obj, id.ToString());
    }

    DefinitionProxies[defintionId.ToString()] = definitionProxy;
  }
}
