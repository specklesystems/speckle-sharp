using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.Converters.DxfConverter
{
    public partial class SpeckleDxfConverter
    {
        public void SetContextDocument(object doc)
        {
            switch (doc)
            {
                case null: // Create a new in-memory document
                    Doc = new netDxf.DxfDocument();
                    break;
                case string str: // Load up an existing document
                    Doc = netDxf.DxfDocument.Load(str);
                    break;
            }
        }

        public void SaveContextDocument(string path)
        {
            Doc?.Save(path);
        }

        public void SetContextObjects(List<ApplicationPlaceholderObject> objects)
        {
            // No context tracking
        }

        public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects)
        {
            // No context tracking
        }
    }
}