using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Autocad;
using Speckle.Converters.Common;
using Speckle.Converters.Common.DependencyInjection;

namespace Speckle.Converters.Autocad20243.DependencyInjection;

public class AutocadConverterModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    builder.AddConverterCommon<AutocadRootToHostConverter, AutocadToSpeckleUnitConverter, UnitsValue>();

    // single stack per conversion
    builder.AddScoped<IConversionContextStack<Document, UnitsValue>, AutocadConversionContextStack>();
  }
}
