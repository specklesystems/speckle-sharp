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
    // POC: Currently we can only register one IRootToHostConverter, and the below will overrid Autocad with Civil3d
    // This needs to be resolved to allow for multiple registrations of IRootToHost
    builder.AddConverterCommon<AutocadRootToHostConverter, AutocadToSpeckleUnitConverter, UnitsValue>();
    builder.AddConverterCommon<Civil3dRootToHostConverter, Civil3dToSpeckleUnitConverter, Autodesk.Aec.BuiltInUnit>();

    // single stack per conversion
    builder.AddScoped<IConversionContextStack<Document, UnitsValue>, AutocadConversionContextStack>();
    builder.AddScoped<IConversionContextStack<Document, Autodesk.Aec.BuiltInUnit>, Civil3dConversionContextStack>();
  }
}
