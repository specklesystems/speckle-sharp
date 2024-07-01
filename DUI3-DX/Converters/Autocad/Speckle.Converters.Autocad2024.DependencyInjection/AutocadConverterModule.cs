using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Autocad;
using Speckle.Converters.Common;
using Speckle.Converters.Common.DependencyInjection;

namespace Speckle.Converters.Autocad2024.DependencyInjection;

public class AutocadConverterModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    builder.AddRootCommon<AutocadRootToHostConverter>();
    builder.AddApplicationConverters<AutocadToSpeckleUnitConverter, UnitsValue>();
    builder.AddScoped<IConversionContextStack<Document, UnitsValue>, AutocadConversionContextStack>();
  }
}
