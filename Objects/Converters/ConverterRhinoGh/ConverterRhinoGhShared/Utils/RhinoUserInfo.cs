using System.Collections.Generic;
using System.Collections.Specialized;
using System;
using Objects.Structural.Analysis;
using Rhino;
using Rhino.Collections;
using Rhino.DocObjects;
using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.Converter.RhinoGh.Utils;

internal sealed class RhinoUserInfo : IRhinoUserInfo
{
  private const string USER_STRINGS = "userStrings";
  private const string USER_DICTIONARY = "userStrings";

  private readonly IRhinoDictionaryParser rhinoDictionaryParser;

  public RhinoUserInfo(IRhinoDictionaryParser rhinoDictionaryParser)
  {
    this.rhinoDictionaryParser = rhinoDictionaryParser;
  }

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
                  string name = null)
  {
    notes = new List<string>();

    // user strings
    if (userStrings != null && userStrings.Count > 0)
    {
      var userStringsBase = new Base();
      foreach (var key in userStrings.AllKeys)
        try
        {
          userStringsBase[key] = userStrings[key];
        }
        catch (Exception e)
        {
          notes.Add($"Could not attach user string: {e.Message}");
        }

      obj[USER_STRINGS] = userStringsBase;
    }

    // user dictionary
    if (userDictionary != null && userDictionary.Count > 0)
    {
      var userDictionaryBase = new Base();

      rhinoDictionaryParser.ParseArchivableToDictionary(userDictionaryBase, userDictionary);

      obj[USER_DICTIONARY] = userDictionaryBase;
    }

    // obj name
    if (!string.IsNullOrEmpty(name))
      obj["name"] = name;
  }
}
