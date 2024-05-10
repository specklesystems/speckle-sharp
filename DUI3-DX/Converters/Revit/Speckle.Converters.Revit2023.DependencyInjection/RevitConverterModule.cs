// POC: not sure we should have this here as it attaches us to autofac, maybe a bit prematurely...

using Autodesk.Revit.DB;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;
using Speckle.Converters.RevitShared;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;
using Speckle.Converters.RevitShared.ToSpeckle;

namespace Speckle.Converters.Revit2023.DependencyInjection;

public class RevitConverterModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    // most things should be InstancePerLifetimeScope so we get one per operation
    builder.AddScoped<ISpeckleConverterToSpeckle, RevitConverterToSpeckle>();

    // factory for conversions
    builder.AddScoped<
      IFactory<string, IHostObjectToSpeckleConversion>,
      Factory<string, IHostObjectToSpeckleConversion>
    >();
    builder.AddScoped<
      IConverterResolver<IHostObjectToSpeckleConversion>,
      RecursiveConverterResolver<IHostObjectToSpeckleConversion>
    >();

    // POC: do we need ToSpeckleScalingService as is, do we need to interface it out?
    builder.AddScoped<ScalingServiceToSpeckle>();

    // POC: the concrete type can come out if we remove all the reference to it
    builder.AddScoped<IRevitConversionContextStack, RevitConversionContextStack>();

    builder.AddScoped<IHostToSpeckleUnitConverter<ForgeTypeId>, RevitToSpeckleUnitConverter>();

    builder.AddScoped<IReferencePointConverter, ReferencePointConverter>();
    builder.AddScoped<RevitConversionSettings>();

    builder.AddScoped<IRevitVersionConversionHelper, RevitVersionConversionHelper>();

    builder.AddScoped<ParameterValueExtractor>();
    builder.AddScoped<DisplayValueExtractor>();
    builder.AddScoped<HostedElementConversionToSpeckle>();
    builder.AddScoped<ParameterObjectAssigner>();
    builder.AddScoped<ISlopeArrowExtractor, SlopeArrowExtractor>();
  }
}
