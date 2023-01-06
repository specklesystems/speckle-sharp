using System;
using System.Collections.Generic;
using DesktopUI2;
using DesktopUI2.Models;
using DesktopUI2.ViewModels;
using Speckle.Core.Logging;
using ConnectorTeklaStructures.Storage;
using System.Linq;
using System.Threading.Tasks;

namespace Speckle.ConnectorTeklaStructures.UI
{
  public partial class ConnectorBindingsTeklaStructures : ConnectorBindings
  {
    #region Local stream I/O with local file
    public override List<MenuItem> GetCustomStreamMenuItems()
    {
      return new List<MenuItem>();
    }

    public override void WriteStreamsToFile(List<StreamState> streams)
    {
      StreamStateManager.ClearStreamStateList(Model);
      StreamStateManager.WriteStreamStateList(Model, streams);
    }

    public override List<StreamState> GetStreamsInFile()
    {
      var streams = new List<StreamState>();
      if (Model != null)
        streams = StreamStateManager.ReadState(Model);

      return streams;
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
