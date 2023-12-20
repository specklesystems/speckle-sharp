using System;
using DesktopUI2.Models;
using Speckle.Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Speckle.Core.Logging;
using Tekla.Structures.Model;

namespace ConnectorTeklaStructures.Storage;

public static class StreamStateManager
{
  private static string _speckleFilePath;

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
      return new List<StreamState>();
    }
  }

  /// <summary>
  /// Writes the stream states to the <TeklaStructuresModelName>.txt file in speckle folder
  /// that exists or is created in the folder where the TeklaStructures model exists.
  /// </summary>
  /// <param name="model"></param>
  /// <param name="streamStates"></param>
  public static void WriteStreamStateList(Model model, List<StreamState> streamStates)
  {
    if (_speckleFilePath == null)
    {
      GetOrCreateSpeckleFilePath(model);
    }

    if (_speckleFilePath == null)
      return;
    FileStream fileStream = new(_speckleFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

    if (!fileStream.CanWrite)
    {
      return;
    }

    try
    {
      using var streamWriter = new StreamWriter(fileStream);
      string streamStateString = JsonConvert.SerializeObject(streamStates) as string;

      if (string.IsNullOrEmpty(streamStateString))
        return;
      streamWriter.Write(streamStateString);
      streamWriter.Flush();
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      SpeckleLog.Logger.Error("Failed to write stream state list to file");
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
    if (_speckleFilePath == null)
    {
      GetOrCreateSpeckleFilePath(model);
    }

    if (_speckleFilePath == null || !File.Exists(_speckleFilePath))
    {
      return;
    }

    FileStream fileStream = null;
    try
    {
      fileStream = new FileStream(_speckleFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
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
  /// called "<TeklaStructuresModelName>.txt". This function create this file and folder if
  /// they doesn't exists and returns it, otherwise just returns the file path
  /// </summary>
  /// <param name="model"></param>
  private static void GetOrCreateSpeckleFilePath(Model model)
  {
    string TeklaStructuresModelfilePath = model.GetInfo().ModelPath;
    if (TeklaStructuresModelfilePath == "")
    {
      // TeklaStructures model is probably not saved, so speckle shouldn't do much
      _speckleFilePath = null;
      return;
    }
    string TeklaStructuresFileName = Path.GetFileNameWithoutExtension(model.GetInfo().ModelName);
    string speckleFolderPath = Path.Combine(TeklaStructuresModelfilePath, "speckle");
    string speckleFilePath = Path.Combine(speckleFolderPath, $"{TeklaStructuresFileName}.txt");

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

      _speckleFilePath = speckleFilePath;
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      // Combine handling for non-fatal exceptions
      string errorMessage = ex switch
      {
        IOException _ => $"IO error occurred while accessing {speckleFilePath}: {ex.Message}",
        UnauthorizedAccessException _ => $"Access to {speckleFilePath} denied: {ex.Message}",
        _ => $"An unexpected error occurred while processing {speckleFilePath}: {ex.Message}"
      };

      SpeckleLog.Logger.Error($"Could not create or retrieve the Speckle Stream State File: {errorMessage}");
      _speckleFilePath = null;
    }
  }

  /// <summary>
  /// Reads the "/speckle/<TeklaStructuresModelName>.txt" file and returns the string in it
  /// </summary>
  /// <param name="model"></param>
  private static string ReadSpeckleFile(Model model)
  {
    if (_speckleFilePath == null)
    {
      GetOrCreateSpeckleFilePath(model);
    }

    if (_speckleFilePath == null)
    {
      return "";
    }

    FileStream fileStream = null;
    try
    {
      fileStream = new FileStream(_speckleFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      using var streamReader = new StreamReader(fileStream, Encoding.UTF8);
      return streamReader.ReadToEnd();
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      // Handle non-fatal exceptions
      string errorMessage = ex switch
      {
        IOException _ => $"IO error occurred while reading from {_speckleFilePath}: {ex.Message}",
        UnauthorizedAccessException _ => $"Access to {_speckleFilePath} denied: {ex.Message}",
        _ => $"An unexpected error occurred while reading from {_speckleFilePath}: {ex.Message}"
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
