using System;
using System.Collections.Generic;
using Speckle.Core.Models;
using Speckle.netDxf;
namespace Objects.Converters.DxfConverter
{
    public partial class SpeckleDxfConverter
    {
        public void SetContextDocument(object doc)
        {
            switch (doc)
            {
                case null: // Create a new in-memory document
                    Doc = new DxfDocument();
                    Doc.DrawingVariables.InsUnits = UnitsToDocUnits(Settings.DocUnits);
                    break;
                case string str: // Load up an existing document
                    Doc = DxfDocument.Load(str);
                    break;
                case DxfDocument d:
                    Doc = d;
                    break;
                default:
                    throw new Exception("Provided doc is not a string or a DXF doc");
            }
        }

        public void SaveContextDocument(string path)
        {
            Doc?.Save(path);
        }

        public void SetContextObjects(List<ApplicationObject> objects)
        {
            // No context tracking
        }

        public void SetPreviousContextObjects(List<ApplicationObject> objects)
        {
            // No context tracking
        }
    }
}