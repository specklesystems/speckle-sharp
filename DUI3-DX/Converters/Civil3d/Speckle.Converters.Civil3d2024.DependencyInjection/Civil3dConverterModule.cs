using Autodesk.AutoCAD.ApplicationServices;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Civil3d;
using Speckle.Converters.Common;
using Speckle.Converters.Common.DependencyInjection;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Converters.Autocad;

namespace Speckle.Converters.Civil3d2024.DependencyInjection;

public class Civil3dConverterModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    // Register single root
    builder.AddRootCommon<Civil3dRootToHostConverter>();

    // register all application converters
    builder.AddApplicationConverters<Civil3dToSpeckleUnitConverter, Autodesk.Aec.BuiltInUnit>();
    builder.AddApplicationConverters<AutocadToSpeckleUnitConverter, UnitsValue>();
    builder.AddScoped<IConversionContextStack<Document, Autodesk.Aec.BuiltInUnit>, Civil3dConversionContextStack>();
    builder.AddScoped<IConversionContextStack<Document, UnitsValue>, AutocadConversionContextStack>();
  }
}
