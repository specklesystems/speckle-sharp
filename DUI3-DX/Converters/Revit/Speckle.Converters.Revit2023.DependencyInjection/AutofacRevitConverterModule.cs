// POC: not sure we should have this here as it attaches us to autofac, maybe a bit prematurely...

using Autodesk.Revit.DB;
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
    builder.RegisterType<RevitConverterToSpeckle>().As<ISpeckleConverterToSpeckle>().InstancePerLifetimeScope();

    // factory for conversions
    builder
      .RegisterType<Factory<string, IHostObjectToSpeckleConversion>>()
      .As<IFactory<string, IHostObjectToSpeckleConversion>>()
      .SingleInstance();

    // POC: do we need ToSpeckleScalingService as is, do we need to interface it out?
    builder.RegisterType<ScalingServiceToSpeckle>().AsSelf().InstancePerLifetimeScope();

    // POC: the concrete type can come out if we remove all the reference to it
    builder
      .RegisterType<RevitConversionContextStack>()
      .As<IRevitConversionContextStack>()
      .AsSelf()
      .InstancePerLifetimeScope();

    builder
      .RegisterType<RevitToSpeckleUnitConverter>()
      .As<IHostToSpeckleUnitConverter<ForgeTypeId>>()
      .InstancePerLifetimeScope();

    builder.RegisterType<ReferencePointConverter>().As<IReferencePointConverter>().InstancePerLifetimeScope();
    builder.RegisterType<RevitConversionSettings>().AsSelf().InstancePerLifetimeScope();

    builder.RegisterType<ParameterValueExtractor>().AsSelf().InstancePerLifetimeScope();
    builder.RegisterType<DisplayValueExtractor>().AsSelf().InstancePerLifetimeScope();
    builder.RegisterType<MeshDataTriangulator>().AsSelf().InstancePerLifetimeScope();
    builder.RegisterType<HostedElementConversionToSpeckle>().AsSelf().InstancePerLifetimeScope();
    builder.RegisterType<ParameterObjectAssigner>().AsSelf().InstancePerLifetimeScope();
  }
}
