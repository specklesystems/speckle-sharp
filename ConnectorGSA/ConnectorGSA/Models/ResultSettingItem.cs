using Speckle.GSA.API;

namespace ConnectorGSA.Models
{
  public class ResultSettingItem
  {
    public string Name { get; set; }
    public ResultType ResultType { get; set; }
    public bool Selected { get; set; } = false;
    public bool DefaultSelected { get; set; } = false;
    //These are the common ones to be positioned at the top before the rest
    public bool Highlighted { get; set; } = false;
    //Add in link to GSA codes etc here

    public ResultSettingItem(string name, ResultType resultType, bool defaultSelected)
    {
      Name = name;
      ResultType = resultType;
      DefaultSelected = defaultSelected;
      Selected = defaultSelected;
      Highlighted = defaultSelected;
    }
  }
}