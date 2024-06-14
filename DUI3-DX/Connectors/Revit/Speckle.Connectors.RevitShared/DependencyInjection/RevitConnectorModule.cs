using Autodesk.Revit.DB;
using CefSharp;
using Speckle.Autofac;
using Speckle.Autofac.DependencyInjection;
using Speckle.Connectors.DUI;
using Speckle.Connectors.DUI.Bindings;
using Speckle.Connectors.DUI.Bridge;
using Speckle.Connectors.DUI.Models;
using Speckle.Connectors.Revit.Bindings;
using Speckle.Connectors.Revit.HostApp;
using Speckle.Connectors.Revit.Operations.Send;
using Speckle.Connectors.Revit.Plugin;
using Speckle.Connectors.Revit2023.Converters;
using Speckle.Connectors.RevitShared.Helpers;
using Speckle.Connectors.Utils;
using Speckle.Connectors.Utils.Builders;
using Speckle.Connectors.Utils.Caching;
using Speckle.Connectors.Utils.Operations;
using Speckle.Converters.Common;
using Speckle.Revit.Api;
using Speckle.Revit.Interfaces;

namespace Speckle.Connectors.Revit.DependencyInjection;

// POC: should interface out things that are not
public class RevitConnectorModule : ISpeckleModule
{
  public void Load(SpeckleContainerBuilder builder)
  {
    builder.AddAutofac();
    builder.AddConnectorUtils();
    builder.AddDUI();
    builder.AddSingleton(new RevitContext());

    // POC: the concrete type can come out if we remove all the reference to it
    builder.AddScoped<IConversionContextStack<IRevitDocument, IRevitForgeTypeId>, RevitConversionContextStack>();
    builder.ScanAssemblyOfType<RevitUnitUtils>();

    builder.AddSingletonInstance<ISyncToThread, SyncToCurrentThread>();

    // POC: different versons for different versions of CEF
    builder.AddSingleton(BindingOptions.DefaultBinder);

    var panel = new CefSharpPanel();
    panel.Browser.JavascriptObjectRepository.NameConverter = null;

    builder.AddSingleton(panel);
    builder.AddSingleton<IRevitPlugin, RevitPlugin>();

    // register
    builder.AddSingleton<DocumentModelStore, RevitDocumentStore>();

    // Storage Schema
    builder.AddScoped<DocumentModelStorageSchema>();
    builder.AddScoped<IdStorageSchema>();

    // POC: we need to review the scopes and create a document on what the policy is
    // and where the UoW should be
    // register UI bindings
    builder.AddSingleton<IBinding, TestBinding>();
    builder.AddSingleton<IBinding, ConfigBinding>("connectorName", "ArcGIS"); // POC: Easier like this for now, should be cleaned up later
    builder.AddSingleton<IBinding, AccountBinding>();
    builder.AddSingleton<IBinding, BasicConnectorBindingRevit>();
    builder.AddSingleton<IBasicConnectorBinding, BasicConnectorBindingRevit>();
    builder.AddSingleton<IBinding, SelectionBinding>();
    builder.AddSingleton<IBinding, RevitSendBinding>();
    //no receive?
    builder.AddSingleton<IRevitIdleManager, RevitIdleManager>();

    // send operation and dependencies
    builder.AddScoped<SendOperation<ElementId>>();
    builder.AddScoped<IRootObjectBuilder<ElementId>, RevitRootObjectBuilder>();

    // register send conversion cache
    builder.AddSingleton<ISendConversionCache, SendConversionCache>();
    builder.AddSingleton<IProxyMap, ProxyMap>();
  }
}
