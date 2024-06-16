using System.DoubleNumerics;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Speckle.Connectors.Autocad.HostApp.Extensions;
using Speckle.Connectors.Autocad.Operations.Send;
using Speckle.Connectors.Utils.Conversion;
using Speckle.Connectors.Utils.Instances;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Instances;

namespace Speckle.Connectors.Autocad.HostApp;

/// <summary>
/// <inheritdoc/>
///  Expects to be a scoped dependency per send or receive operation.
/// </summary>
public class AutocadInstanceObjectManager : IInstanceObjectsManager<AutocadRootObject, List<Entity>>
{
  private Dictionary<string, InstanceProxy> InstanceProxies { get; set; } = new();
  private Dictionary<string, List<InstanceProxy>> InstanceProxiesByDefinitionId { get; set; } = new();
  private Dictionary<string, InstanceDefinitionProxy> DefinitionProxies { get; set; } = new();
  private Dictionary<string, AutocadRootObject> FlatAtomicObjects { get; set; } = new();

  public UnpackResult<AutocadRootObject> UnpackSelection(IEnumerable<AutocadRootObject> objects)
  {
    using var transaction = Application.DocumentManager.CurrentDocument.Database.TransactionManager.StartTransaction();

    foreach (var obj in objects)
    {
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

  public BakeResult BakeInstances(
    List<(string[] layerPath, IInstanceComponent obj)> instanceComponents,
    Dictionary<string, List<Entity>> applicationIdMap,
    string baseLayerName,
    Action<string, double?>? onOperationProgressed
  )
  {
    var sortedInstanceComponents = instanceComponents
      .OrderByDescending(x => x.obj.MaxDepth) // Sort by max depth, so we start baking from the deepest element first
      .ThenBy(x => x.obj is InstanceDefinitionProxy ? 0 : 1) // Ensure we bake the deepest definition first, then any instances that depend on it
      .ToList();

    var definitionIdAndApplicationIdMap = new Dictionary<string, ObjectId>();

    using var transaction = Application.DocumentManager.CurrentDocument.Database.TransactionManager.StartTransaction();
    var conversionResults = new List<ReceiveConversionResult>();
    var createdObjectIds = new List<string>();
    var consumedObjectIds = new List<string>();
    var count = 0;

    foreach (var (path, instanceOrDefinition) in sortedInstanceComponents)
    {
      try
      {
        onOperationProgressed?.Invoke("Converting blocks", (double)++count / sortedInstanceComponents.Count);
        if (instanceOrDefinition is InstanceDefinitionProxy definitionProxy && definitionProxy.applicationId != null)
        {
          // TODO: create definition (block table record)
          var constituentEntities = definitionProxy.Objects
            .Select(id => applicationIdMap.TryGetValue(id, out List<Entity> value) ? value : null)
            .Where(x => x is not null)
            .SelectMany(ent => ent)
            .ToList();

          var record = new BlockTableRecord();
          var objectIds = new ObjectIdCollection();
          record.Name = baseLayerName + definitionProxy.applicationId;
          foreach (var entity in constituentEntities)
          {
            // record.AppendEntity(entity);
            objectIds.Add(entity.ObjectId);
          }

          using var blockTable = (BlockTable)
            transaction.GetObject(Application.DocumentManager.CurrentDocument.Database.BlockTableId, OpenMode.ForWrite);
          var id = blockTable.Add(record);

          record.AssumeOwnershipOf(objectIds);

          definitionIdAndApplicationIdMap[definitionProxy.applicationId] = id;
          transaction.AddNewlyCreatedDBObject(record, true);
        }
        else if (
          instanceOrDefinition is InstanceProxy instanceProxy
          && definitionIdAndApplicationIdMap.TryGetValue(instanceProxy.DefinitionId, out ObjectId definitionId)
        )
        {
          var matrix3d = GetMatrix3d(instanceProxy.Transform, instanceProxy.Units);
          var insertionPoint = Point3d.Origin.TransformBy(matrix3d);

          var modelSpaceBlockTableRecord = Application.DocumentManager.CurrentDocument.Database.GetModelSpace(
            OpenMode.ForWrite
          );

          var blockRef = new BlockReference(insertionPoint, definitionId) { BlockTransform = matrix3d };
          modelSpaceBlockTableRecord.AppendEntity(blockRef);

          if (instanceProxy.applicationId != null)
          {
            applicationIdMap[instanceProxy.applicationId] = new List<Entity>() { blockRef };
          }

          transaction.AddNewlyCreatedDBObject(blockRef, true);
          conversionResults.Add(
            new(Status.SUCCESS, instanceProxy, blockRef.Handle.Value.ToString(), "Instance (Block)")
          );
        }
      }
      catch (Exception ex) when (!ex.IsFatal())
      {
        conversionResults.Add(new(Status.ERROR, instanceOrDefinition as Base ?? new Base(), null, null, ex));
      }
    }
    transaction.Commit();
    return new(new List<string>(), new List<string>(), conversionResults);
  }

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

  private Matrix3d GetMatrix3d(Matrix4x4 matrix, string units)
  {
    var sf = Units.GetConversionFactor(
      units,
      Application.DocumentManager.CurrentDocument.Database.Insunits.ToSpeckleString()
    );

    var scaledTransform = new[]
    {
      matrix.M11,
      matrix.M12,
      matrix.M13,
      matrix.M14 * sf,
      matrix.M21,
      matrix.M22,
      matrix.M23,
      matrix.M24 * sf,
      matrix.M31,
      matrix.M32,
      matrix.M33,
      matrix.M34 * sf,
      matrix.M41,
      matrix.M42,
      matrix.M43,
      matrix.M44
    };

    var m3d = new Matrix3d(scaledTransform);
    if (!m3d.IsScaledOrtho())
    {
      m3d = new Matrix3d(MakePerpendicular(m3d));
    }

    return m3d;
  }

  // https://forums.autodesk.com/t5/net/set-blocktransform-values/m-p/6452121#M49479
  private static double[] MakePerpendicular(Matrix3d matrix)
  {
    // Get the basis vectors of the matrix
    Vector3d right = new(matrix[0, 0], matrix[1, 0], matrix[2, 0]);
    Vector3d up = new(matrix[0, 1], matrix[1, 1], matrix[2, 1]);

    Vector3d newForward = right.CrossProduct(up).GetNormal();
    Vector3d newUp = newForward.CrossProduct(right).GetNormal();

    return new[]
    {
      right.X,
      newUp.X,
      newForward.X,
      matrix[0, 3],
      right.Y,
      newUp.Y,
      newForward.Y,
      matrix[1, 3],
      right.Z,
      newUp.Z,
      newForward.Z,
      matrix[2, 3],
      0.0,
      0.0,
      0.0,
      matrix[3, 3],
    };
  }
}
