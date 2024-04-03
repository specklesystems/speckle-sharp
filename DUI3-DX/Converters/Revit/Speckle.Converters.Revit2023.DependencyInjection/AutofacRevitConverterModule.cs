// POC: not sure we should have this here as it attaches us to autofac, maybe a bit prematurely...

using Autofac;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;
using Speckle.Converters.RevitShared.ToSpeckle;

namespace Speckle.Converters.Revit2023.DependencyInjection;

public class AutofacRevitConverterModule : Module
{
  protected override void Load(ContainerBuilder builder)
  {
    // most things should be InstancePerLifetimeScope so we get one per operation
    builder.RegisterType<RevitConverterToSpeckle>().As<ISpeckleConverterToSpeckle>();

    // factory for conversions
    builder
      .RegisterType<Factory<string, IHostObjectToSpeckleConversion>>()
      .As<IFactory<string, IHostObjectToSpeckleConversion>>();

    // POC: do we need ToSpeckleScalingService as is, do we need to interface it out?
    builder.RegisterType<ToSpeckleScalingService>().AsSelf().InstancePerLifetimeScope();
    builder.RegisterType<RevitConversionContextStack>().AsSelf().InstancePerLifetimeScope();

    // POC: check with CI speckler but this AsImplementedInterfaces() seems wrong or non-specific here
    builder.RegisterType<RevitToSpeckleUnitConverter>().AsImplementedInterfaces().SingleInstance();
    builder.RegisterType<ParameterValueExtractor>().AsSelf().InstancePerLifetimeScope();
    builder.RegisterType<DisplayValueExtractor>().AsSelf().InstancePerLifetimeScope();
    builder.RegisterType<CachingService>().AsSelf().InstancePerLifetimeScope();
    builder.RegisterType<MeshDataTriangulator>().AsSelf().InstancePerLifetimeScope();
    builder.RegisterType<HostedElementConversionToSpeckle>().AsSelf().InstancePerLifetimeScope();
    builder.RegisterType<ParameterObjectAssigner>().AsSelf().InstancePerLifetimeScope();
  }
}
