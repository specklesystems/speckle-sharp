using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autofac;
using ConnectorRevit.Operations;
using ConnectorRevit.Revit;
using ConnectorRevit.Services;
using ConnectorRevit.Storage;
using ConverterRevitTestsShared.Services;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Objects.Converter.Revit;
using RevitSharedResources.Interfaces;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using xUnitRevitUtils;

namespace ConverterRevitTests
{
  public class SpeckleConversionTest
  {
    private IContainer container;
    internal SpeckleConversionFixture fixture;

    public SpeckleConversionTest(SpeckleConversionFixture fixture)
    {
      this.fixture = fixture;
      this.fixture.TestClassName = GetType().Name;
      DefineDefaultDependencies();
    }

    internal async Task NativeToSpeckle()
    {
      await NativeToSpeckle(fixture.SourceDoc).ConfigureAwait(false);
    }
    internal async Task<IConvertedObjectsCache<Element, Base>> NativeToSpeckle(UIDocument uiDocument)
    {
      var scope = CreateScope(uiDocument);

      var sendOp = scope.Resolve<SendOperation>();
      await sendOp.Send().ConfigureAwait(false);

      var selection = scope.Resolve<ISendSelection>();
      var convertedObjects = scope.Resolve<IConvertedObjectsCache<Element, Base>>();
      
      foreach (var el in selection.Elements)
      {
        var speckleElements = convertedObjects.GetCreatedObjectsFromConvertedId(el.UniqueId);
        foreach (var @base in speckleElements)
        {
          AssertUtils.ValidSpeckleElement(el, @base);
        }
      }
      return convertedObjects;
    }

    /// <summary>
    /// Gets elements from the fixture SourceDoc
    /// Converts them to Speckle
    /// Creates a new Doc (or uses the open one if open!)
    /// Converts the speckle objects to Native
    /// </summary>
    internal async Task SpeckleToNative()
    {
      var convertedObjs = await SpeckleToNative(fixture.SourceDoc).ConfigureAwait(false);
      SpeckleUtils.DeleteElement(convertedObjs.GetCreatedObjects());
    }

    internal async Task<IConvertedObjectsCache<Base, Element>> SpeckleToNative(
      UIDocument uiDocument,
      IConvertedObjectsCache<Base, Element> previouslyConvertedObjectsCache = null
    )
    {
      var objectsToReceive = await NativeToSpeckle(uiDocument).ConfigureAwait(false);

      var scope = CreateScope(fixture.NewDoc);
      var receiveOp = scope.Resolve<ReceiveOperation>();
      await receiveOp.Receive().ConfigureAwait(false);

      var receivedObjects = scope.Resolve<IConvertedObjectsCache<Base, Element>>();
      var converter = scope.Resolve<ISpeckleConverter>();

      foreach (var speckleObject in objectsToReceive.GetCreatedObjects())
      {
        if (!converter.CanConvertToNative(speckleObject)) continue;

        var sourceObject = uiDocument.Document.GetElement(speckleObject.applicationId);
        var destObject = receivedObjects.GetCreatedObjectsFromConvertedId(speckleObject.applicationId).First();

        if (previouslyConvertedObjectsCache != null)
        {
          // make sure the previousObject was updated instead of a new one being created
          var previousObject = previouslyConvertedObjectsCache.GetCreatedObjectsFromConvertedId(speckleObject.applicationId).First();

          // if previous object is not valid then it was deleted and re-created instead of updated
          if (previousObject.IsValidObject)
          {
            Assert.Equal(previousObject.UniqueId, destObject.UniqueId);
          }
        }
        await AssertUtils.Handle(sourceObject, destObject, speckleObject).ConfigureAwait(false);
      }
      return receivedObjects;
    }

    internal async Task SpeckleToNativeUpdates()
    {
      var initialObjs = await SpeckleToNative(fixture.SourceDoc).ConfigureAwait(false);
      _ = await SpeckleToNative(fixture.UpdatedDoc, initialObjs).ConfigureAwait(false);
      SpeckleUtils.DeleteElement(initialObjs.GetCreatedObjects());
    }

    public void DefineDefaultDependencies()
    {
      var builder = new ContainerBuilder();
      builder.RegisterType<ConverterRevit>().As<ISpeckleConverter>()
        .InstancePerLifetimeScope();

      // receive operation dependencies
      builder.RegisterType<SpeckleObjectLocalReceiver>().As<ISpeckleObjectReceiver>();

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
      builder.RegisterType<SpeckleObjectLocalSender>().As<ISpeckleObjectSender>();

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

      builder.RegisterInstance<UIApplication>(xru.Uiapp)
        .SingleInstance();

      builder.RegisterType<ReceiveOperation>();
      builder.RegisterType<SendOperation>();

      OverrideDependencies(builder);
      container = builder.Build();
    }

    public virtual void OverrideDependencies(ContainerBuilder builder)
    {
      // override default dependencies by overriding this method
    }

    public ILifetimeScope CreateScope(UIDocument uiDocument)
    {
      var scope = CreateScope();

      // set document to use for operation
      var docProvider = scope.Resolve<IEntityProvider<UIDocument>>();
      docProvider.Entity = uiDocument;

      return scope;
    }
    public ILifetimeScope CreateScope()
    {
      var scope = container.BeginLifetimeScope();

      // using objects such as the following DUI entity providers is a bad practice that we have to employ
      // to make up for not having proper DI configured in DUI
      var streamStateProvider = scope.Resolve<IEntityProvider<StreamState>>();
      streamStateProvider.Entity = fixture.StreamState;
      var progressProvider = scope.Resolve<IEntityProvider<ProgressViewModel>>();
      progressProvider.Entity = new ProgressViewModel();

      return scope;
    }
  }
}
