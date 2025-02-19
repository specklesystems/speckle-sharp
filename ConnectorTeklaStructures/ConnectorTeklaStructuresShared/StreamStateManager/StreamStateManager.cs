using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DesktopUI2.Models;
using Speckle.Core.Logging;
using Speckle.Newtonsoft.Json;
using Tekla.Structures.Model;

namespace ConnectorTeklaStructures.Storage;

public static class StreamStateManager
{
  private static string s_speckleFilePath;

  public static List<StreamState> ReadState(Model model)
  {
    var strings = ReadSpeckleFile(model);
    if (string.IsNullOrEmpty(strings))
    {
      return new List<StreamState>();
    }
    try
    {
      return JsonConvert.DeserializeObject<List<StreamState>>(strings);
    }
    catch (JsonException ex)
    {
      SpeckleLog.Logger.Warning(
        "Failed to deserialize saved stream state list: " + $"{ex.Message}. Resetting to an empty list."
      );
      return new List<StreamState>();
    }
  }

  /// <summary>
  /// Writes the stream states to the {TeklaStructuresModelName}.txt file in speckle folder
  /// that exists or is created in the folder where the TeklaStructures model exists.
  /// </summary>
  /// <param name="model"></param>
  /// <param name="streamStates"></param>
  public static void WriteStreamStateList(Model model, List<StreamState> streamStates)
  {
    if (s_speckleFilePath == null)
    {
      GetOrCreateSpeckleFilePath(model);
    }

    if (s_speckleFilePath == null)
    {
      return;
    }

    try
    {
      FileStream fileStream = new(s_speckleFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
      if (!fileStream.CanWrite)
      {
        return;
      }

      using var streamWriter = new StreamWriter(fileStream);
      string streamStateString = JsonConvert.SerializeObject(streamStates);
      if (string.IsNullOrEmpty(streamStateString))
      {
        return;
      }

      streamWriter.Write(streamStateString);
      streamWriter.Flush();
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      throw new SpeckleException($"Failed to write stream state list to file: {ex.Message}");
    }
  }

  /// <summary>
  /// Clears the contents of the stream state file associated with the specified model.
  /// </summary>
  /// <remarks>
  /// This method sets the length of the stream state file to zero, effectively clearing its contents.
  /// It is important to ensure that no other process is writing to the file when this method is called.
  /// If the file path is not set or the file does not exist, the method will return early without action.
  /// </remarks>
  /// <param name="model">The model associated with the stream state file. This is used to determine or create the file path if it is not already set.</param>
  public static void ClearStreamStateList(Model model)
  {
    if (s_speckleFilePath == null)
    {
      GetOrCreateSpeckleFilePath(model);
    }

    if (s_speckleFilePath == null || !File.Exists(s_speckleFilePath))
    {
      return;
    }

    FileStream fileStream = null;
    try
    {
      fileStream = new FileStream(s_speckleFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
      fileStream.SetLength(0);
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Error($"Failed to clear stream state list: {ex.Message}");
    }
    finally
    {
      fileStream?.Dispose();
    }
  }

  /// <summary>
  /// We need a folder in TeklaStructures model folder named "speckle" and a file in it
  /// called "{TeklaStructuresModelName}.txt". This function create this file and folder if
  /// they doesn't exists and returns it, otherwise just returns the file path
  /// </summary>
  /// <param name="model"></param>
  private static void GetOrCreateSpeckleFilePath(Model model)
  {
    string teklaStructuresModelfilePath = model.GetInfo().ModelPath;
    if (string.IsNullOrEmpty(teklaStructuresModelfilePath))
    {
      // TeklaStructures model is probably not saved, so speckle shouldn't do much
      s_speckleFilePath = null;
      return;
    }
    string teklaStructuresFileName = Path.GetFileNameWithoutExtension(model.GetInfo().ModelName);
    string speckleFolderPath = Path.Combine(teklaStructuresModelfilePath, "speckle");
    string speckleFilePath = Path.Combine(speckleFolderPath, $"{teklaStructuresFileName}.txt");

    try
    {
      if (!Directory.Exists(speckleFolderPath))
      {
        Directory.CreateDirectory(speckleFolderPath);
      }

      if (!File.Exists(speckleFilePath))
      {
        using (File.CreateText(speckleFilePath))
        {
          // No content initialization needed
        }
      }

      s_speckleFilePath = speckleFilePath;
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      // Combine handling for non-fatal exceptions
      string errorMessage = ex switch
      {
        IOException => $"IO error occurred while accessing {speckleFilePath}: {ex.Message}",
        UnauthorizedAccessException => $"Access to {speckleFilePath} denied: {ex.Message}",
        _ => $"An unexpected error occurred while processing {speckleFilePath}: {ex.Message}"
      };

      SpeckleLog.Logger.Error($"Could not create or retrieve the Speckle Stream State File: {errorMessage}");
      s_speckleFilePath = null;
    }
  }

  /// <summary>
  /// Reads the "/speckle/{TeklaStructuresModelName}.txt" file and returns the string in it
  /// </summary>
  /// <param name="model"></param>
  private static string ReadSpeckleFile(Model model)
  {
    if (s_speckleFilePath == null)
    {
      GetOrCreateSpeckleFilePath(model);
    }

    if (s_speckleFilePath == null)
    {
      return "";
    }

    FileStream fileStream = null;
    try
    {
      fileStream = new FileStream(s_speckleFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      using var streamReader = new StreamReader(fileStream, Encoding.UTF8);
      return streamReader.ReadToEnd();
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      // Handle non-fatal exceptions
      string errorMessage = ex switch
      {
        IOException => $"IO error occurred while reading from {s_speckleFilePath}: {ex.Message}",
        UnauthorizedAccessException => $"Access to {s_speckleFilePath} denied: {ex.Message}",
        _ => $"An unexpected error occurred while reading from {s_speckleFilePath}: {ex.Message}"
      };

      SpeckleLog.Logger.Error($"Could not read the Speckle Stream State File: {errorMessage}");
    }
    finally
    {
      fileStream?.Dispose();
    }

    return "";
  }
}
