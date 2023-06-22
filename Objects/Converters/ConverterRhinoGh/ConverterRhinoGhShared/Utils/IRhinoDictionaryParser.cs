using Objects.Structural.Analysis;
using Rhino;
using Rhino.Collections;
using Rhino.DocObjects;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using static Objects.Converter.RhinoGh.Utils.IRhinoUnits;

namespace Objects.Converter.RhinoGh.Utils;

public interface IRhinoDictionaryParser
{
  /// <summary>
  /// Copies an ArchivableDictionary to a Base
  /// </summary>
  /// <param name="target"></param>
  /// <param name="dict"></param>
  void ParseArchivableToDictionary(Base target, ArchivableDictionary dict);
}
