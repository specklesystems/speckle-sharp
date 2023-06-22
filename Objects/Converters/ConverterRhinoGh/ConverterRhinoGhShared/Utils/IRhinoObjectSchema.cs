using Objects.Structural.Analysis;
using Rhino;
using Rhino.DocObjects;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Objects.Converter.RhinoGh.Utils;

public interface IRhinoObjectsSchema
{
  string GetSchema(RhinoObject obj, string key, out string[] args);
}

