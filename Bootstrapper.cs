using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Speckle.DesktopUI;
using Stylet;
using StyletIoC;

namespace Speckle.DesktopUI
{
  public class Bootstrapper : Bootstrapper<RootViewModel>
  {
    private ConnectorBindings bindings;

    // public Bootstrapper(ConnectorBindings appBindings = null)
    // {
    //   this.bindings = appBindings;
    // }
    protected override void ConfigureIoC(IStyletIoCBuilder builder)
    {
      base.ConfigureIoC(builder);

      // if (bindings != null)
      //   builder.Bind<ConnectorBindings>().ToInstance(bindings);
    }
  }
}