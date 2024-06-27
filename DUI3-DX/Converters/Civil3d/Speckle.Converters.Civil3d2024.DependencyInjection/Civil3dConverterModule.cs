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
    builder.AddConverterCommon<Civil3dRootToHostConverter, Civil3dToSpeckleUnitConverter, Autodesk.Aec.BuiltInUnit>();
    builder.AddConverterCommon<AutocadRootToHostConverter, AutocadToSpeckleUnitConverter, UnitsValue>();

    // single stack per conversion
    builder.AddScoped<IConversionContextStack<Document, Autodesk.Aec.BuiltInUnit>, Civil3dConversionContextStack>();
    builder.AddScoped<IConversionContextStack<Document, UnitsValue>, AutocadConversionContextStack>();
  }
}
