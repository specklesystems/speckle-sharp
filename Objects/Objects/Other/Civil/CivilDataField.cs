namespace Objects.Other.Civil;

public class CivilDataField : DataField
{
  public CivilDataField() { }

  public CivilDataField(
    string name,
    string type,
    object? value,
    string? units = null,
    string? context = null,
    string? displayName = null
  )
  {
    this.name = name;
    this.type = type;
    this.value = value;
    this.units = units;
    this.context = context;
    this.displayName = displayName;
  }

  /// <summary>
  /// The context type of the Civil3D part
  /// </summary>
  public string? context { get; set; }

  public string? displayName { get; set; }
}
