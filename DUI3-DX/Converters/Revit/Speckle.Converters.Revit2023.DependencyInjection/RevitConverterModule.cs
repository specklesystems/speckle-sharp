using Autodesk.Revit.DB;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common;
using Speckle.Converters.Common.DependencyInjection;
using Speckle.Converters.RevitShared;
using Speckle.Converters.RevitShared.Helpers;
using Speckle.Converters.RevitShared.Services;
using Speckle.Converters.RevitShared.ToSpeckle;

namespace Speckle.Converters.Revit2023.DependencyInjection;

public class RevitConverterModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    builder.AddConverterCommon<RootToSpeckleConverter, RevitToSpeckleUnitConverter, ForgeTypeId>();
    builder.AddSingleton(new RevitContext());

    // POC: do we need ToSpeckleScalingService as is, do we need to interface it out?
    builder.AddScoped<ScalingServiceToSpeckle>();

    // POC: the concrete type can come out if we remove all the reference to it
    builder.AddScoped<IRevitConversionContextStack, RevitConversionContextStack>();

    builder.AddScoped<IReferencePointConverter, ReferencePointConverter>();
    builder.AddScoped<RevitConversionSettings>();

    builder.AddScoped<IRevitVersionConversionHelper, RevitVersionConversionHelper>();

    builder.AddScoped<ParameterValueExtractor>();
    builder.AddScoped<DisplayValueExtractor>();
    builder.AddScoped<HostedElementConversionToSpeckle>();
    builder.AddScoped<ParameterObjectAssigner>();
    builder.AddScoped<ISlopeArrowExtractor, SlopeArrowExtractor>();
    builder.AddScoped<SendSelection>();
    builder.AddScoped<ToSpeckleConvertedObjectsCache>();
  }
}
