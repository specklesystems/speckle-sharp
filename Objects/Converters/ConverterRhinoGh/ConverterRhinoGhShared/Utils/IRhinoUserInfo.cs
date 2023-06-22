using System.Collections.Generic;
using System.Collections.Specialized;
using Objects.Structural.Analysis;
using Rhino;
using Rhino.Collections;
using Rhino.DocObjects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Converter.RhinoGh.Utils;

public interface IRhinoUserInfo
{
  /// <summary>
  /// Attaches the provided user strings, user dictionaries, and and name to Base
  /// </summary>
  /// <param name="obj">The converted Base object to attach info to</param>
  /// <returns></returns>
  public void GetUserInfo(
                  Base obj,
                  out List<string> notes,
                  ArchivableDictionary userDictionary = null,
                  NameValueCollection userStrings = null,
                  string name = null);
}
