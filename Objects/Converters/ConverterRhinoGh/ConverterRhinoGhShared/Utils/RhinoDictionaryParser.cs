using System.Collections.Generic;
using Objects.Structural.Analysis;
using Rhino;
using Rhino.Collections;
using Rhino.DocObjects;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;

namespace Objects.Converter.RhinoGh.Utils;

public sealed class RhinoDictionaryParser : IRhinoDictionaryParser
{
  /// <summary>
  /// Copies an ArchivableDictionary to a Base
  /// </summary>
  /// <param name="target"></param>
  /// <param name="dict"></param>
  public void ParseArchivableToDictionary(Base target, ArchivableDictionary dict)
  {
    foreach (var key in dict.Keys)
    {
      var obj = dict[key];
      switch (obj)
      {
        case ArchivableDictionary o:
          var nested = new Base();
          ParseArchivableToDictionary(nested, o);
          target[key] = nested;
          continue;

        case double _:
        case bool _:
        case int _:
        case string _:
        case IEnumerable<double> _:
        case IEnumerable<bool> _:
        case IEnumerable<int> _:
        case IEnumerable<string> _:
          target[key] = obj;
          continue;

        default:
          continue;
      }
    }
  }
}
