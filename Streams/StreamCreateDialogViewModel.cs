using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stylet;

namespace Speckle.DesktopUI.Streams
{
  public class StreamCreateDialogViewModel : Conductor<IScreen>.StackNavigation
  {
    public StreamCreateDialogViewModel()
    {
      DisplayName = "Create Stream";
    }

  }
}
