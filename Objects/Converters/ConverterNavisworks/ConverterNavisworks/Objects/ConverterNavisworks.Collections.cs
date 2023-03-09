using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;

namespace Objects.Converter.Navisworks.Objects
{
  public class ElementCollection : Base
  {
    /// <summary>
    /// The human-readable name of the <see cref="ElementCollection"/>.
    /// </summary>
    /// <remarks>This name is not necessarily unique within a commit. Set the applicationId for a unique identifier.</remarks>
    public string name { get; set; }

    /// <summary>
    /// The type of this collection - this is derived in Navisworks from the Icon
    /// </summary>
    [JsonIgnore]
    public string collectionType { get; set; }

    /// <summary>
    /// The pluralization given to the type of this collection
    /// </summary>
    [JsonIgnore]
    public string _grouping { get; set; }

    public ElementCollection()
    {
    }
  }

  

  public class File : ElementCollection
  {
    public File()
    {
      collectionType = "File";
      _grouping = "Files";
    }
  }

  public class Layer : ElementCollection
  {
    public Layer()
    {
      collectionType = "Layer";
      _grouping = "Layers";
    }
  }

  public class Collection : ElementCollection
  {
    public Collection()
    {
      collectionType = "Collection";
      _grouping = "Collections";
    }
  }

  public class Group : ElementCollection
  {
    public Group()
    {
      collectionType = "Group";
      _grouping = "Groups";
    }
  }
  public class InsertGroup : ElementCollection
  {
    public InsertGroup()
    {
      collectionType = "Insert Group";
      _grouping = "Insert Groups";
    }
  }
  public class InsertGeometry : ElementCollection
  {
    public InsertGeometry()
    {
      collectionType = "Insert Geometry";
      _grouping = "Insert Geometries";
    }
  }

  public class CompositeObject : ElementCollection
  {
    public CompositeObject()
    {
      collectionType = "Composite Object";
      _grouping = "Composite Objects";
    }
  }
}
