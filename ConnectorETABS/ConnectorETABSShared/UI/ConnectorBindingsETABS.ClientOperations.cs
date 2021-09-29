
using System.Collections.Generic;
using Speckle.DesktopUI;
using Speckle.DesktopUI.Utils;
using Speckle.Core.Logging;
using ConnectorETABS.Storage;
using System.Linq;


namespace Speckle.ConnectorETABS.UI
{
    public partial class ConnectorBindingsETABS : ConnectorBindings

    {
        #region Local stream I/O with local file
        public override void AddNewStream(StreamState state)
        {
            Tracker.TrackPageview(Tracker.STREAM_CREATE);
            var index = DocumentStreams.FindIndex(b => b.Stream.id == state.Stream.id);
            if (index == -1)
            {
                DocumentStreams.Add(state);
                WriteStateToFile();
            }
        }
        private void WriteStateToFile()
        {
            StreamStateManager.WriteStreamStateList(Doc, DocumentStreams);
        }

        public override void RemoveStreamFromFile(string streamId)
        {
            var streamState = DocumentStreams.FirstOrDefault(s => s.Stream.id == streamId);
            if (streamState != null)
            {
                DocumentStreams.Remove(streamState);
                WriteStateToFile();
            }
        }

        public override List<StreamState> GetStreamsInFile()
        {
            if (Doc != null)
                DocumentStreams = StreamStateManager.ReadState(Doc);

            return DocumentStreams;
        }

        public override void PersistAndUpdateStreamInFile(StreamState state)
        {
            var index = DocumentStreams.FindIndex(b => b.Stream.id == state.Stream.id);
            if (index != -1)
            {
                DocumentStreams[index] = state;
                WriteStateToFile();
            }
        }

        #endregion
    }
}