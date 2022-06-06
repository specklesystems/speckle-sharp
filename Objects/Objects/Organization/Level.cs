using Objects.Properties;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Organization
{
  /// <summary>
  /// The basic Speckle <see cref="Level"/> class.
  /// </summary>
  public class Level : Base, IHasSourceAppProps
  {
    /// <summary>
    /// The string identifier for this <see cref="Level"/>.
    /// This name will be used when creating the level at the given <see cref="elevation"/>.
    /// Depending on the receiving application, it may be used to retrieve an existing level with a matching name.
    /// However, this is not guaranteed behaviour.
    /// </summary>
    public string name { get; set; }

    /// <summary>
    /// A double specifying the elevation of this <see cref="Level"/>.
    /// You must specify <see cref="units"/> when setting <see cref="elevation"/> or you may see unexpected results
    /// when receiving. Unset units are usually assumed to be metres, but this is not guaranteed.
    /// </summary>
    public double elevation { get; set; }

    /// <summary>
    /// True if on receive this <see cref="Level"/> should be used to get an existing level by <see cref="name"/>
    /// (ie this <see cref="Level"/> is only for reference). If a match by <see cref="name"/> is found on receive,
    /// the <see cref="elevation"/> will be ignored. If no level with this <see cref="name"/> is found on receive,
    /// a new level will be created at the given <see cref="elevation"/> with the given <see cref="name"/>
    /// even if the model has an existing level at that elevation.
    /// </summary>
    public bool referenceOnly { get; set; }

    /// <summary>
    /// A string representing the abbreviated units (eg "m", "mm", "ft").
    /// Use the <see cref="Units"/> helper to ensure you're using the correct strings.
    /// </summary>
    public string units { get; set; }

    public ApplicationProperties sourceApp { get; set; }

    public Level()
    {
    }

    /// <summary>
    /// Create a Speckle Level.
    /// </summary>
    /// <param name="name">The name of the level as a string</param>
    /// <param name="elevation">The elevation of the level in the given <see cref="units"/></param>
    /// <param name="units">The units of the level.</param>
    /// <param name="sourceApp">[Optional] Source <see cref="ApplicationProperties"/> for higher fidelity data transfer to the target application</param>
    public Level(string name, double elevation, string units, ApplicationProperties sourceApp = null)
    {
      this.name = name;
      this.elevation = elevation;
      this.units = units;
      this.sourceApp = sourceApp;
    }


    /// <summary>
    /// Create a Speckle Level.
    /// </summary>
    /// <param name="name">The name of the level as a string</param>
    /// <param name="elevation">The elevation of the level in the given <see cref="units"/></param>
    /// <param name="sourceApp">[Optional] Source <see cref="ApplicationProperties"/> for higher fidelity data transfer to the target application</param>
    /// <remarks>In grasshopper, the <see cref="units"/> are automatically assigned as document units by the Schema Builder</remarks>
    [SchemaInfo("Level", "Creates a Speckle level", "BIM", "Organization")]
    public Level(string name, double elevation,
      [SchemaParamInfo("Source application properties for higher fidelity data transfer to the target application")]
      ApplicationProperties sourceApp = null)
    {
      this.name = name;
      this.elevation = elevation;
      this.sourceApp = sourceApp;
    }

    /// <summary>
    /// Creates a Speckle Level by <see cref="name"/> but without <see cref="elevation"/>.
    /// This is useful if your level is just a placeholder for name matching in a different host application.
    /// </summary>
    /// <param name="name">The name of the level as a string</param>
    /// <param name="sourceApp">[Optional] Source <see cref="ApplicationProperties"/> for higher fidelity data transfer to the target application</param>
    [SchemaInfo("Level Reference",
      "Creates a Speckle level by name without elevation. This is useful if your level is just a placeholder for name matching in a different host application.",
      "BIM", "Organization")]
    public Level(string name,
      [SchemaParamInfo("Source application properties for higher fidelity data transfer to the target application")]
      ApplicationProperties sourceApp = null)
    {
      this.name = name;
      this.referenceOnly = true;
      this.sourceApp = sourceApp;
    }
  }

  public class RevitLevelProperties : RevitProperties
  {
    /// <summary>
    /// If true, it creates an associated view in Revit. NOTE: only used when creating a level for the first time
    /// </summary>
    public bool createView { get; set; }
  }
}