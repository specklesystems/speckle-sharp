using Speckle.Core.Models;

namespace Objects.Other;

/// <summary>
/// Generic class for a data field
/// </summary>
public class DataField : Base
{
  public DataField() { }

  public DataField(string name, string type, string units, object? value = null)
  {
    this.name = name;
    this.type = type;
    this.units = units;
    this.value = value;
  }

  public string name { get; set; }

  public string type { get; set; }

  public object? value { get; set; }

  public string units { get; set; }
}
