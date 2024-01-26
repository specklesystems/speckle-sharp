using System.Collections.Generic;
using System.IO;
using DesktopUI2.Models;
using Speckle.ConnectorNavisworks.Storage;

namespace Speckle.ConnectorNavisworks.Bindings;

public partial class ConnectorBindingsNavisworks
{
  /// <summary>
  /// Writes the list of stream states to the file.
  /// </summary>
  /// <param name="streams">The list of stream states to write.</param>
  public override void WriteStreamsToFile(List<StreamState> streams) =>
    SpeckleStreamManager.WriteStreamStateList(s_doc, streams);

  /// <summary>
  /// Retrieves the list of stream states from the file.
  /// </summary>
  /// <returns>The list of stream states.</returns>
  public override List<StreamState> GetStreamsInFile()
  {
    var streams = new List<StreamState>();
    if (s_doc != null)
    {
      streams = SpeckleStreamManager.ReadState(s_doc);
    }

    return streams;
  }

  /// <summary>
  /// Retrieves the name of the current file.
  /// </summary>
  /// <returns>The name of the current file.</returns>
  public override string GetFileName()
  {
    IsFileAndModelsPresent();

    return s_doc?.CurrentFileName ?? string.Empty;
  }

  private static void IsFileAndModelsPresent()
  {
    if (s_doc == null)
    {
      throw new FileNotFoundException("No active document found. Cannot Send.");
    }

    if (s_doc.Models.Count == 0)
    {
      throw new FileNotFoundException("No models are appended. Nothing to Send.");
    }
  }
}
