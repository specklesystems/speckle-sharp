using System;
using System.Collections.Generic;
using System.Timers;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autofac;
using ConnectorRevit.Operations;
using ConnectorRevit.Storage;
using DesktopUI2;
using DesktopUI2.Models;
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

    //public delegate IReceivedObjectIdMap<Base, Element> ReceivedObjectIdMapFactory(StreamState streamState);
    public ConnectorBindingsRevit(UIApplication revitApp) : base()
    {
      var builder = new ContainerBuilder();
      //builder.Register<ISpeckleConverter>((c, p) =>
      //{
      //  return (ISpeckleConverter)Activator.CreateInstance(Converter.GetType());
      //}).As<ISpeckleConverter>();

      builder.RegisterType(Converter.GetType()).As<ISpeckleConverter>();

      builder.RegisterType<StreamStateCache>().InstancePerLifetimeScope();
      builder.Register(c => c.Resolve<StreamStateCache>()).As<IReceivedObjectIdMap<Base, Element>>();

      builder.RegisterType<ConvertedObjectsCache>()
        .As<IConvertedObjectsCache<Base, Element>>()
        .InstancePerLifetimeScope();
      builder.RegisterType<ReceiveOperation>();
      Container = builder.Build();
      //services.AddScoped<IConvertedObjectsCache<Base, Element>, ConvertedObjectsCache>();
      //services.AddSingleton<Func<ISpeckleConverter>>(sp => () => 
      //{ 
      //  return (ISpeckleConverter)Activator.CreateInstance(Converter.GetType());
      //});
      //services.AddTransient<ISpeckleConverter>(sp => sp.GetRequiredService<Func<ISpeckleConverter>>()());
      //services.AddScoped<IReceivedObjectIdMap<Base, Element>, StreamStateCache>();
      //services.AddTransient<ReceivedObjectIdMapFactory>(sp => streamState =>
      //{
      //  return sp.GetRequiredService<IReceivedObjectIdMap<Base, Element>>();
      //});
      //services.AddTransient<ReceiveOperation>();

      //services.AddTransient<ReceiveOperation.Factory>();
      //Container = services.BuildServiceProvider();
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
