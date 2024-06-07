namespace Objects.Other.Civil;

public class CivilDataField : DataField
{
  public CivilDataField() { }

  public CivilDataField(string name, string type, string units, string context, object? value = null)
  {
    this.name = name;
    this.type = type;
    this.units = units;
    this.context = context;
    this.value = value;
  }

  /// <summary>
  /// The context type of the Civil3D part
  /// </summary>
  public string context { get; set; }
}
