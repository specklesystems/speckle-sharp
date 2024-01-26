namespace Objects.BuiltElements.Civil;

public class CivilAlignment : Alignment
{
  public string type { get; set; }

  public string site { get; set; }

  public string style { get; set; }

  public double offset { get; set; }

  /// <summary>
  /// Name of parent alignment if this is an offset alignment
  /// </summary>
  public string parent { get; set; }
}
