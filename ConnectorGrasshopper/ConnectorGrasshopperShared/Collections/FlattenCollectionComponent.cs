using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Speckle.Core.Models;

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

    var flattened = FlattenCollection(collection);
    DA.SetDataList(0, flattened);
  }

  public IEnumerable<Collection> FlattenCollection(Collection collection, string fullNamePrefix = null)
  {
    var fullName = fullNamePrefix == null ? collection.name : fullNamePrefix + "::" + collection.name;
    var elements = collection.elements.Where(e => e is not Collection);
    var innerCollections = collection.elements
      .Where(e => e is Collection)
      .Cast<Collection>()
      .SelectMany(c => FlattenCollection(c, fullName));

    var newCollection = new Collection(collection.name, collection.collectionType) { elements = elements.ToList() };
    newCollection["fullName"] = fullName;
    return new List<Collection> { newCollection }.Concat(innerCollections);
  }
}
