using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Objects.Converter.Archicad;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Archicad
{
  public sealed partial class ElementConverterManager
  {
    public async Task<List<ApplicationObject>> ConvertOneTypeToNative(
      Type elementType,
      IEnumerable<TraversalContext> elements,
      ConversionOptions conversionOptions,
      CancellationToken token
    )
    {
      try
      {
        var elementConverter = GetConverterForElement(elementType, conversionOptions, true);
        return await elementConverter.ConvertToArchicad(elements, token);
      }
      catch (Exception e)
      {
        SpeckleLog.Logger.Warning("Failed to convert element type {elementType}", elementType.ToString());
        return null;
      }
    }

    private async Task<bool> ConvertReceivedObjects(
      List<TraversalContext> flattenObjects,
      ConverterArchicad converter,
      ProgressViewModel progress
    )
    {
      Dictionary<Type, IEnumerable<TraversalContext>> receivedObjects;

      receivedObjects = flattenObjects
        .GroupBy(tc => tc.current.GetType())
        .ToDictionary(group => group.Key, group => group.Cast<TraversalContext>());
      SpeckleLog.Logger.Debug("Conversion started (element types: {0})", receivedObjects.Count);

      foreach (var (elementType, tc) in receivedObjects)
      {
        SpeckleLog.Logger.Debug("{0}: {1}", elementType, tc.Count<TraversalContext>());

        List<Base> elements = tc.Select(tc => tc.current).ToList<Base>();
        var convertedElements = await ConvertOneTypeToNative(
          elementType,
          tc,
          converter.ConversionOptions,
          progress.CancellationTokenSource.Token
        );

        if (convertedElements != null)
        {
          var dict = convertedElements
            .Where(obj => (!string.IsNullOrEmpty(obj.OriginalId)))
            .ToDictionary(obj => obj.OriginalId, obj => obj);
          foreach (var contextObject in converter.ContextObjects)
          {
            if (dict.ContainsKey(contextObject.OriginalId))
            {
              ApplicationObject obj = dict[contextObject.OriginalId];

              contextObject.Update(status: obj.Status, createdIds: obj.CreatedIds, log: obj.Log);
              progress.Report.UpdateReportObject(contextObject);
            }
          }
        }
      }

      SpeckleLog.Logger.Debug("Conversion done.");

      return true;
    }

    public async Task<bool> ConvertToNative(StreamState state, Base commitObject, ProgressViewModel progress)
    {
      try
      {
        ConversionOptions conversionOptions = new ConversionOptions(state.Settings);

        Objects.Converter.Archicad.ConverterArchicad converter = new Objects.Converter.Archicad.ConverterArchicad(
          conversionOptions
        );
        List<TraversalContext> flattenObjects = FlattenCommitObject(commitObject, converter);

        converter.SetContextObjects(flattenObjects);

        foreach (var applicationObject in converter.ContextObjects)
          progress.Report.Log(applicationObject);

        converter.ReceiveMode = state.ReceiveMode;

        return await ConvertReceivedObjects(flattenObjects, converter, progress);
      }
      catch (Exception ex)
      {
        SpeckleLog.Logger.Error(ex, "Conversion to native failed.");
        return false;
      }
    }

    private List<TraversalContext> FlattenCommitObject(
      Base commitObject,
      Objects.Converter.Archicad.ConverterArchicad converter
    )
    {
      // to filter out already traversed objects (e.g. the same Mesh in displayValue and in topRail member of the Railing)
      HashSet<string> traversedObjects = new HashSet<string>();

      TraversalContext Store(TraversalContext context, Objects.Converter.Archicad.ConverterArchicad converter)
      {
        if (!converter.CanConvertToNativeImplemented(context.current))
          return null;

        if (traversedObjects.Contains(context.current.id))
          return null;

        traversedObjects.Add(context.current.id);

        return context;
      }

      var traverseFunction = DefaultTraversal.CreateBIMTraverseFunc(converter);

      var objectsToConvert = traverseFunction
        .Traverse(commitObject)
        .Select(tc => Store(tc, converter))
        .Where(tc => tc != null)
        .ToList();

      return objectsToConvert;
    }
  }
}
