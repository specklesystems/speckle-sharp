using System;
using System.Collections.Generic;
using Autodesk.Navisworks.Api.Data;
using DesktopUI2.Models;
using Speckle.ConnectorNavisworks;
using Speckle.ConnectorNavisworks.Storage;

namespace Speckle.ConnectorNavisworks.Bindings
{
    public partial class ConnectorBindingsNavisworks
    {
        public override void WriteStreamsToFile(List<StreamState> streams)
        {
            SpeckleStreamManager.WriteStreamStateList(Doc, streams);
        }

        public override List<StreamState> GetStreamsInFile()
        {
            var streams = new List<StreamState>();
            if (Doc != null) streams = SpeckleStreamManager.ReadState(Doc);
            return streams;
        }
    }
}