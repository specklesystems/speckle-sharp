using Speckle.Core.Credentials;
using System.Collections.Generic;

namespace DesktopUI2.ViewModels.DesignViewModels
{
  public class DesignMappingViewModel
  {
    public string Title => "for Revit";
    public string TitleFull => "Scheduler for Revit";
    public string SearchQuery => "";
    public string Test { get; set; }
    public List<string> SearchResults { get; set; }
    public Dictionary<string,string> mapping { get; set; }
    public List<string> tabs { get; set; } 

    public ConnectorBindings Bindings { get; set; }

    public List<AccountViewModel> SelectedUsers { get; set; } = new List<AccountViewModel>();

    public DesignMappingViewModel()
    {
      tabs = new List<string> 
      { 
        "Materials",
        "Floors",
        "Walls",
        "Profile Sections"
       };

      mapping = new Dictionary<string, string>
      {
        { "Brick Wall", "Other Wall" },
        { "W12x14", "W12x14" },
        { "Floor Type", "Floor Type 2" },
        { "Another Brick Wall", "Other Wall" },
      };

      SearchResults = new List<string>
      {
        "Type1",
        "Type2",
        "Type3",
        "A wiiiiiide flange",
        "some wall maybe?",
        "roof",
        "other",
        "types",
        "are",
        "welcome"
      };
      Test = "testing. Is this hooked up?";
    }
  }
}
