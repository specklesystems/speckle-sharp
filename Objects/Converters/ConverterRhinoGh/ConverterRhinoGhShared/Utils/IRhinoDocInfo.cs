using Rhino;
using Rhino.DocObjects;
using Speckle.Core.Models;

namespace Objects.Converter.RhinoGh.Utils;

public interface IRhinoDocInfo
{
  string RemoveInvalidRhinoChars(string str);

  // RhinoDoc might need abstracting for unit tests

  string GetCommitInfo(RhinoDoc doc);

  int GetMaterialIndex(RhinoDoc doc, string name);

  Layer GetLayer(RhinoDoc doc, string path, out int index, bool MakeIfNull = false);

  Layer MakeLayer(RhinoDoc doc, string name, out int index, Layer parentLayer = null);
}
