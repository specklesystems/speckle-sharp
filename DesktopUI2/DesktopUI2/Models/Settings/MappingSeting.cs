namespace DesktopUI2.Models.Settings;

public class MappingSeting : ListBoxSetting
{
  private string _mappingJson;

  public string MappingJson
  {
    get => _mappingJson;
    set => _mappingJson = value;
  }
}
