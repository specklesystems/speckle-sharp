using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace ConnectorGrasshopper.Collections;

public class FlattenCollectionComponent : GH_SpeckleComponent
{
  public FlattenCollectionComponent()
    : base(
      "Flatten Collection",
      "SFC",
      "Flattens a collection to expose all it's inner collections and elements",
      ComponentCategories.PRIMARY_RIBBON,
      "Collections"
    ) { }

  public override Guid ComponentGuid => new Guid("9E1F4A6B-201C-4E91-AFC2-5F28031E97C1");
  protected override Bitmap Icon => Properties.Resources.FlattenSpeckleCollection;

  protected override void RegisterInputParams(GH_InputParamManager pManager)
  {
    pManager.AddParameter(new SpeckleCollectionParam());
  }

  protected override void RegisterOutputParams(GH_OutputParamManager pManager)
  {
    pManager.AddParameter(new SpeckleCollectionParam { Access = GH_ParamAccess.list });
  }

  public override void SolveInstanceWithLogContext(IGH_DataAccess DA)
  {
    Collection collection = null;
    if (!DA.GetData(0, ref collection))
      return;

    var excludeTypeFromFullName = new List<string> { "rhino model" };
    var flattened = FlattenCollection(collection, null, excludeTypeFromFullName);

    DA.SetDataList(0, flattened);
  }

  private static IEnumerable<Collection> FlattenCollection(
    Collection collection,
    string fullNamePrefix = null,
    IReadOnlyCollection<string> excludeTypeFromFullName = null
  )
  {
    string nextPrefix = NextPrefix(collection, fullNamePrefix, excludeTypeFromFullName);

    var elements = new List<Base>();
    var innerCollections = new List<Collection>();
    foreach (Base e in collection.elements)
    {
      if (e is Collection c)
        innerCollections.AddRange(FlattenCollection(c, nextPrefix, excludeTypeFromFullName));
      else
        elements.Add(e);
    }

    var newCollection = new Collection(collection.name, collection.collectionType)
    {
      elements = elements.ToList(),
      ["fullName"] = string.IsNullOrEmpty(nextPrefix) ? collection.name : nextPrefix
    };

    return new List<Collection> { newCollection }.Concat(innerCollections);
  }

  private static string NextPrefix(
    Collection collection,
    string fullNamePrefix,
    IReadOnlyCollection<string> excludeTypeFromFullName
  )
  {
    var nameParts = new List<string>();
    if (!string.IsNullOrEmpty(fullNamePrefix))
      nameParts.Add(fullNamePrefix);
    if (excludeTypeFromFullName == null || !excludeTypeFromFullName.Contains(collection.collectionType))
      nameParts.Add(collection.name);

    var nextPrefix = string.Join("::", nameParts);
    return nextPrefix;
  }
}
