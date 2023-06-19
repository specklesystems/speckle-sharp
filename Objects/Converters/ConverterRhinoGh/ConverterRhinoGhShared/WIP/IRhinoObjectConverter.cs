using Rhino.DocObjects;
using Speckle.Core.Models;

public interface IRhinoObjectConverter
{
  Base Convert(string appIdKey, RhinoObject ro);
}
