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
    // add single root converter
    builder.AddRootCommon<AutocadRootToHostConverter>();

    // add application converters and context stack
    builder.AddApplicationConverters<AutocadToSpeckleUnitConverter, UnitsValue>();
    builder.AddScoped<IConversionContextStack<Document, UnitsValue>, AutocadConversionContextStack>();
  }
}
