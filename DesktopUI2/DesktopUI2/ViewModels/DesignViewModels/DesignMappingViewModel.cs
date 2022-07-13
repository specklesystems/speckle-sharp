using Speckle.Core.Credentials;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using static DesktopUI2.ViewModels.MappingViewModel;

namespace DesktopUI2.ViewModels.DesignViewModels
{
  public class DesignMappingViewModel
  {
    public string Title => "for Revit";
    public string TitleFull => "Scheduler for Revit";
    public string SearchQuery => "";
    public string Test { get; set; }
    public List<string> SearchResults { get; set; }
    public ObservableCollection<MappingValue> Mapping { get; set; }
    public List<string> tabs { get; set; } 

    public ConnectorBindings Bindings { get; set; }

    public string CurrentTypeName { get; set; } = "Testing 123";

    public DesignMappingViewModel()
    {
      tabs = new List<string> 
      { 
        "Materials",
        "Floors",
        "Walls",
        "Profile Sections"
       };

      Mapping = new ObservableCollection<MappingValue>
      (new List<MappingValue>
        {
          new MappingValue("W12x19", "W12x19"),
          new MappingValue("Type1", "type123"),
          new MappingValue("anotherType", "anotherType"),
          new MappingValue("yetAnotherType", "differentType" ),
          new MappingValue("short", "short"),
          new MappingValue( "a very very very long type name. Oh no", "a very very very long type name. Oh no")
        }
      );

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
