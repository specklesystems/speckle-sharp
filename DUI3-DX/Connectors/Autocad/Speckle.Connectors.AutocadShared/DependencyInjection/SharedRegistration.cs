using Speckle.Autofac;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.Autocad.Bindings;
using Speckle.Connectors.Autocad.HostApp;
using Speckle.Connectors.Autocad.Interfaces;
using Speckle.Connectors.Autocad.Plugin;
using Speckle.Connectors.DUI;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.DUI.WebView;
using Speckle.Connectors.Utils;

namespace Speckle.Connectors.Autocad.DependencyInjection;

public static class SharedRegistration
{
  public static void Load(SpeckleContainerBuilder builder)
  {
    builder.AddAutofac();
    builder.AddConnectorUtils();
    builder.AddDUI();
    builder.AddDUIView();

    // Register other connector specific types
    builder.AddSingleton<IAutocadPlugin, AutocadPlugin>();
    builder.AddTransient<TransactionContext>();
    builder.AddSingleton(new AutocadDocumentManager()); // TODO: Dependent to TransactionContext, can be moved to AutocadContext
    builder.AddSingleton<DocumentModelStore, AutocadDocumentStore>();
    builder.AddSingleton<AutocadContext>();
    builder.AddSingleton<AutocadLayerManager>();
    builder.AddSingleton<AutocadIdleManager>();

    // Register bindings
    builder.AddSingleton<IBinding, TestBinding>();
    builder.AddSingleton<IBinding, AccountBinding>();
    builder.AddSingleton<IBinding, AutocadBasicConnectorBinding>();
    builder.AddSingleton<IBasicConnectorBinding, AutocadBasicConnectorBinding>();
    builder.AddSingleton<IBinding, AutocadSelectionBinding>();
  }
}
