using Speckle.Autofac.DependencyInjection;
using Speckle.Converters.Common.DependencyInjection.ToHost;
using Speckle.Converters.Common.Objects;
using Speckle.Core.Models;
using Speckle.Core.Reflection;
using Speckle.Core.SchemaVersioning;
using Speckle.Core.Serialisation.TypeCache;
using Speckle.Objects;

namespace Speckle.Converters.Common.DependencyInjection;

public static class ContainerRegistration
{
  public static void AddConverterCommon<TRootToSpeckleConverter, THostToSpeckleUnitConverter, THostUnit>(
    this SpeckleContainerBuilder builder
  )
    where TRootToSpeckleConverter : class, IRootToSpeckleConverter
    where THostToSpeckleUnitConverter : class, IHostToSpeckleUnitConverter<THostUnit>
  {
    // some of these scopes might need consideration, probably could be scoped
    // but...  there's a bit of reflection on creation/initialisation for some of these...
    builder.AddSingleton<ITypeCache, ObjectsTypeCache>();
    builder.AddSingleton<ITypeFinder, TypeFinder>();
    builder.AddSingleton<ITypeInstanceResolver<ISchemaObjectUpgrader<Base, Base>>, SingletonTypeInstanceResolver<ISchemaObjectUpgrader<Base, Base>>>();
    builder.AddSingleton<ISchemaObjectUpgradeManager<Base, Base>, SchemaObjectUpgradeManager<Base, Base>>();
    
    builder.AddScoped<IRootToSpeckleConverter, TRootToSpeckleConverter>();
    builder.AddScoped<IHostToSpeckleUnitConverter<THostUnit>, THostToSpeckleUnitConverter>();
    /*
      POC: CNX-9267 Moved the Injection of converters into the converter module. Not sure if this is 100% right, as this doesn't just register the conversions within this converter, but any conversions found in any Speckle.*.dll file.
      This will require consolidating across other connectors.
    */
    builder.AddScoped<IFactory<IToSpeckleTopLevelConverter>, Factory<IToSpeckleTopLevelConverter>>();
    builder.AddScoped<
      IConverterResolver<IToSpeckleTopLevelConverter>,
      ConverterResolver<IToSpeckleTopLevelConverter>
    >();

    builder.AddScoped<IFactory<IToHostTopLevelConverter>, Factory<IToHostTopLevelConverter>>();
    builder.AddScoped<IConverterResolver<IToHostTopLevelConverter>, ConverterResolver<IToHostTopLevelConverter>>();

    builder.AddScoped<IRootToHostConverter, ConverterWithFallback>();

    builder.RegisterRawConversions();
    builder.InjectNamedTypes<IToSpeckleTopLevelConverter>();
    builder.InjectNamedTypes<IToHostTopLevelConverter>();
  }
}
