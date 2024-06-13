using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.DependencyInjection;
using Speckle.Converters.RevitShared;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;
using Speckle.Converters.RevitShared.ToSpeckle;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023.DependencyInjection;

public class RevitConverterModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    builder.AddConverterCommon<IRootToSpeckleConverter, RevitToSpeckleUnitConverter, IRevitForgeTypeId>();

    // POC: do we need ToSpeckleScalingService as is, do we need to interface it out?
    builder.AddScoped<IScalingServiceToSpeckle, ScalingServiceToSpeckle>();

    builder.AddScoped<IReferencePointConverter, ReferencePointConverter>();
    builder.AddScoped<IRevitConversionSettings, RevitConversionSettings>();

    builder.AddScoped<IRevitVersionConversionHelper, RevitVersionConversionHelper>();

    builder.AddScoped<IParameterValueExtractor, ParameterValueExtractor>();
    builder.AddScoped<IDisplayValueExtractor, DisplayValueExtractor>();
    builder.AddScoped<IHostedElementConversionToSpeckle, HostedElementConversionToSpeckle>();
    builder.AddScoped<IParameterObjectAssigner, ParameterObjectAssigner>();
    builder.AddScoped<ISlopeArrowExtractor, SlopeArrowExtractor>();
    builder.AddScoped<SendSelection>();
  }
}
