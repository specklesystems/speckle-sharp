using System;
using System.Collections.Generic;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.Core.Logging;
using ConnectorCSI.Storage;
using System.Linq;
using System.Threading.Tasks;
using DesktopUI2.Models.Settings;

namespace Speckle.ConnectorCSI.UI
{
  public partial class ConnectorBindingsCSI : ConnectorBindings

  {
    #region Local stream I/O with local file
    public override List<MenuItem> GetCustomStreamMenuItems()
    {
      return new List<MenuItem>();
    }

    public override void WriteStreamsToFile(List<StreamState> streams)
    {
      StreamStateManager.ClearStreamStateList(Model);

      foreach (var s in streams)
      {
        DocumentStreams.Add(s);
        WriteStateToFile();
      }
    }

    //public override void AddNewStream(StreamState state)
    //{
    //    Tracker.TrackPageview(Tracker.STREAM_CREATE);
    //    var index = DocumentStreams.FindIndex(b => b.Stream.id == state.Stream.id);
    //    if (index == -1)
    //    {
    //        DocumentStreams.Add(state);
    //        WriteStateToFile();
    //    }
    //}
    private void WriteStateToFile()
    {
      StreamStateManager.WriteStreamStateList(Model, DocumentStreams);
    }

    //public override void RemoveStreamFromFile(string streamId)
    //{
    //    var streamState = DocumentStreams.FirstOrDefault(s => s.Stream.id == streamId);
    //    if (streamState != null)
    //    {
    //        DocumentStreams.Remove(streamState);
    //        WriteStateToFile();
    //    }
    //}

    public override List<StreamState> GetStreamsInFile()
    {
      if (Model != null)
        DocumentStreams = StreamStateManager.ReadState(Model);

      return DocumentStreams;
    }

    public override List<ISetting> GetSettings()
    {
      return new List<ISetting> { };
    }

    //public override void PersistAndUpdateStreamInFile(StreamState state)
    //{
    //    var index = DocumentStreams.FindIndex(b => b.Stream.id == state.Stream.id);
    //    if (index != -1)
    //    {
    //        DocumentStreams[index] = state;
    //        WriteStateToFile();
    //    }
    //}

    #endregion
  }
}