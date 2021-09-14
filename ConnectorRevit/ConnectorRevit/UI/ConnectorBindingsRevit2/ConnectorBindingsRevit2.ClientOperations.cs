using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using DesktopUI2.Models;
using Revit.Async;
using Speckle.ConnectorRevit.Storage;


using RevitElement = Autodesk.Revit.DB.Element;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit2
  {


    public List<StreamState> DocumentStreams { get; set; } = new List<StreamState>();


    public List<Exception> ConversionErrors { get; set; } = new List<Exception>();

    /// <summary>
    /// Keeps track of errors in the operations of send/receive.
    /// </summary>
    public List<Exception> OperationErrors { get; set; } = new List<Exception>();

    public override List<StreamState> GetStreamsInFile()
    {
      if (CurrentDoc != null)
        DocumentStreams = StreamStateManager2.ReadState(CurrentDoc.Document);

      return DocumentStreams;
    }

    #region Local file i/o

    /// <summary>
    /// Adds a new stream to the file.
    /// </summary>
    /// <param name="state">StreamState passed by the UI</param>
    public override void AddNewStream(StreamState state)
    {
      var index = DocumentStreams.FindIndex(b => b.Id == state.Id);
      if (index == -1)
      {
        DocumentStreams.Add(state);
        WriteStateToFile();
      }
    }

    /// <summary>
    /// Removes a stream from the file.
    /// </summary>
    /// <param name="streamId"></param>
    public override void RemoveStreamFromFile(string id)
    {
      var streamState = DocumentStreams.FirstOrDefault(s => s.Id == id);
      if (streamState != null)
      {
        DocumentStreams.Remove(streamState);
        WriteStateToFile();
      }
    }

    /// <summary>
    /// Update the stream state and adds adds the filtered objects
    /// </summary>
    /// <param name="state"></param>
    public override void PersistAndUpdateStreamInFile(StreamState state)
    {
      var index = DocumentStreams.FindIndex(b => b.Id == state.Id);
      if (index != -1)
      {
        DocumentStreams[index] = state;
        WriteStateToFile();
      }
    }

    /// <summary>
    /// Transaction wrapper around writing the local streams to the file.
    /// </summary>
    private void WriteStateToFile()
    {
      RevitTask.RunAsync(
        app =>
        {
          using (Transaction t = new Transaction(CurrentDoc.Document, "Speckle Write State"))
          {
            t.Start();
            StreamStateManager2.WriteStreamStateList(CurrentDoc.Document, DocumentStreams);
            t.Commit();
          }
        });
    }

    #endregion


  }
}
