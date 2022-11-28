using Autodesk.Navisworks.Api;
using DesktopUI2.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.ConnectorNavisworks.Storage
{
    internal class SpeckleStreamManager
    {
        private static readonly string SpeckleExtensionDictionary = "Speckle";
        private static readonly string SpeckleStreamStates = "StreamStates";

        public static List<StreamState> ReadState(Document doc)
        {
            var streams = new List<StreamState>();
            if (doc == null)
                return streams;

            //TODO! get saved stream state data from the current document.

            return streams;
        }
    }
}
