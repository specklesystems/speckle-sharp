using System;
using System.Collections.Generic;
using System.Timers;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autofac;
using ConnectorRevit;
using ConnectorRevit.Operations;
using ConnectorRevit.Revit;
using ConnectorRevit.Services;
using ConnectorRevit.Storage;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using RevitSharedResources.Interfaces;
using Speckle.ConnectorRevit.Storage;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Timer = System.Timers.Timer;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit : ConnectorBindings
  {
    public static UIApplication RevitApp;

    public static UIDocument CurrentDoc => RevitApp.ActiveUIDocument;

    public Timer SelectionTimer;

    //Only use an instance of the converter as a local variable to avoid conflicts if multiple sending/receiving
    //operations are happening at the same time
    public ISpeckleConverter Converter { get; set; } = KitManager.GetDefaultKit().LoadConverter(ConnectorRevitUtils.RevitAppName);

    public List<Exception> ConversionErrors { get; set; } = new List<Exception>();

    /// <summary>
    /// Keeps track of errors in the operations of send/receive.
    /// </summary>
    public List<Exception> OperationErrors { get; set; } = new List<Exception>();
    public IContainer Container { get; }
    public ConnectorBindingsRevit(UIApplication revitApp) : base()
    {
      var builder = new ContainerBuilder();

      builder.RegisterType(Converter.GetType()).As<ISpeckleConverter>()
        .InstancePerLifetimeScope();

      // receive operation dependencies
      builder.RegisterType<SpeckleObjectServerReceiver>().As<ISpeckleObjectReceiver>();

      builder.RegisterType<StreamStateCache>().As<IReceivedObjectIdMap<Base, Element>>()
        .InstancePerLifetimeScope();

      builder.RegisterType<ConvertedObjectsCache>().As<IConvertedObjectsCache<Base, Element>>()
        .InstancePerLifetimeScope();

      builder.RegisterType<TransactionManager>().As<IRevitTransactionManager>()
        .InstancePerLifetimeScope();
      builder.RegisterType<ErrorEater>();

      builder.RegisterType<StreamStateConversionSettings>().As<IConversionSettings>()
        .InstancePerLifetimeScope();

      // send operation dependencies
      builder.RegisterType<SendSelection>().As<ISendSelection>()
        .InstancePerLifetimeScope();
      builder.RegisterType<ConvertedObjectsCacheSend>().As<IConvertedObjectsCache<Element, Base>>()
        .InstancePerLifetimeScope();
      builder.RegisterType<SpeckleObjectServerSender>().As<ISpeckleObjectSender>();

      // questionable DUI object registration
      builder.RegisterType<DUIEntityProvider<StreamState>>().As<IEntityProvider<StreamState>>()
        .InstancePerLifetimeScope();
      builder.RegisterType<DUIEntityProvider<ProgressViewModel>>().As<IEntityProvider<ProgressViewModel>>()
        .InstancePerLifetimeScope();
      builder.Register(c =>
      {
        var state = c.Resolve<IEntityProvider<StreamState>>();
        return state.Entity.ReceiveMode;
      });
      builder.Register(c =>
      {
        var state = c.Resolve<IEntityProvider<StreamState>>();
        return state.Entity.Filter;
      });
      builder.Register(c =>
      {
        var state = c.Resolve<IEntityProvider<StreamState>>();
        return state.Entity.Client;
      });
      
      builder.RegisterType<UIDocumentProvider>().As<IEntityProvider<UIDocument>>()
        .InstancePerLifetimeScope();

      builder.RegisterInstance<UIApplication>(revitApp)
        .SingleInstance();

      builder.RegisterType<ReceiveOperation>();
      builder.RegisterType<SendOperation>();
      Container = builder.Build();
      RevitApp = revitApp;
    }

    private void SelectionTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      var selectedObjects = GetSelectedObjects();

      //TODO

      //NotifyUi(new UpdateSelectionCountEvent() { SelectionCount = selectedObjects.Count });
      //NotifyUi(new UpdateSelectionEvent() { ObjectIds = selectedObjects });
    }

    public static string HostAppNameVersion => ConnectorRevitUtils.RevitAppName.Replace("Revit", "Revit "); //hack for ADSK store
    public static string HostAppName => HostApplications.Revit.Slug;

    public override string GetHostAppName() => HostAppName;
    public override string GetHostAppNameVersion() => HostAppNameVersion;
    
    public override string GetDocumentId() => CurrentDoc?.Document?.GetHashCode().ToString();

    public override string GetDocumentLocation() => CurrentDoc.Document.PathName;

    public override string GetActiveViewName() => CurrentDoc.Document.ActiveView.Title;

    public override string GetFileName() => CurrentDoc.Document.Title;

    public override List<StreamState> GetStreamsInFile()
    {
      var streams = new List<StreamState>();
      if (CurrentDoc != null)
        streams = StreamStateManager.ReadState(CurrentDoc.Document);

      return streams;
    }

    public override List<ReceiveMode> GetReceiveModes()
    {
      return new List<ReceiveMode> { ReceiveMode.Update, ReceiveMode.Create, ReceiveMode.Ignore };
    }

    //TODO
    public override List<MenuItem> GetCustomStreamMenuItems()
    {
      return new List<MenuItem>();
    }
  }
}
