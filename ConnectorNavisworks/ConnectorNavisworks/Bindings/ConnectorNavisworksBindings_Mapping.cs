using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static DesktopUI2.ViewModels.MappingViewModel;

namespace Speckle.ConnectorNavisworks.Bindings
{
  public partial class ConnectorBindingsNavisworks
  {
    public override async Task<Dictionary<string, List<MappingValue>>> ImportFamilyCommand(
      Dictionary<string, List<MappingValue>> mapping)
    {
      // TODO!
      await Task.Delay(TimeSpan.FromMilliseconds(500));
      return new Dictionary<string, List<MappingValue>>();
    }
  }
}