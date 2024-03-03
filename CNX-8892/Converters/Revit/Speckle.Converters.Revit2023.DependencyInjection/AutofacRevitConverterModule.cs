// POC: not sure we should have this here as it attaches us to autofac, maybe a bit prematurely...
using Autofac;
using Speckle.Converters.Common;
using Speckle.Converters.RevitShared;

namespace Speckle.Converters.Revit2023;

public class AutofacRevitConverterModule : Module
{
  protected override void Load(ContainerBuilder builder)
  {
    // most things should be InstancePerLifetimeScope so we get one per operation
    builder.RegisterType<RevitConverterToSpeckle>().As<ISpeckleConverterToSpeckle>().InstancePerLifetimeScope();

    //
  }
}
