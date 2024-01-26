using CSiAPIv1;
using DesktopUI2.Models;
using Speckle.Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ConnectorCSI.Storage;

public static class StreamStateManager
{
  private static string? _speckleFilePath;

  public static List<StreamState> ReadState(cSapModel model)
  {
    var strings = ReadSpeckleFile(model);
    if (string.IsNullOrEmpty(strings))
    {
      return new List<StreamState>();
    }

    return JsonConvert.DeserializeObject<List<StreamState>>(strings);
  }

  /// <summary>
  /// Writes the stream states to the <CSIModelName>.txt file in speckle folder
  /// that exists or is created in the folder where the CSI model exists.
  /// </summary>
  /// <param name="model"></param>
  /// <param name="streamStates"></param>
  public static void WriteStreamStateList(cSapModel model, List<StreamState> streamStates)
  {
    if (_speckleFilePath == null)
    {
      GetOrCreateSpeckleFilePath(model);
    }

    FileStream fileStream = new(_speckleFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

    using (var streamWriter = new StreamWriter(fileStream))
    {
      streamWriter.Write(JsonConvert.SerializeObject(streamStates) as string);
      streamWriter.Flush();
    }
  }

  public static void ClearStreamStateList(cSapModel model)
  {
    if (_speckleFilePath == null)
    {
      GetOrCreateSpeckleFilePath(model);
    }

    FileStream fileStream = new(_speckleFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

    fileStream.SetLength(0);
  }

  /// <summary>
  /// We need a folder in CSI model folder named "speckle" and a file in it
  /// called "<CSIModelName>.txt". This function create this file and folder if
  /// they doesn't exists and returns it, otherwise just returns the file path
  /// </summary>
  /// <param name="model"></param>
  private static void GetOrCreateSpeckleFilePath(cSapModel model)
  {
    string CSIModelfilePath = model.GetModelFilename(true);
    string CSIModelFolder = Path.GetDirectoryName(CSIModelfilePath);
    if (string.IsNullOrEmpty(CSIModelFolder))
    {
      // CSI model is probably not saved, so speckle shouldn't do much
      _speckleFilePath = null;
      return;
    }
    string CSIFileName = Path.GetFileNameWithoutExtension(CSIModelfilePath);
    string speckleFolderPath = Path.Combine(CSIModelFolder, "speckle");
    string speckleFilePath = Path.Combine(CSIModelFolder, "speckle", $"{CSIFileName}.txt");

    if (!Directory.Exists(speckleFolderPath))
    {
      Directory.CreateDirectory(speckleFolderPath);
    }
    if (!File.Exists(speckleFilePath))
    {
      File.CreateText(speckleFilePath);
    }
    _speckleFilePath = speckleFilePath;
  }

  /// <summary>
  /// Reads the "/speckle/<CSIModelName>.txt" file and returns the string in it
  /// </summary>
  /// <param name="model"></param>
  private static string ReadSpeckleFile(cSapModel model)
  {
    if (_speckleFilePath == null)
    {
      GetOrCreateSpeckleFilePath(model);
    }

    if (_speckleFilePath == null)
    {
      return "";
    }

    FileStream fileStream = new(_speckleFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

    using var streamReader = new StreamReader(fileStream, Encoding.UTF8);
    return streamReader.ReadToEnd();
  }

  /// <summary>
  /// Saves a backup file with the extension _speckleBackup1 (number ranges from 1-3)
  /// if all three backups already exist, it will override the oldest one
  /// </summary>
  /// <param name="model"></param>
  public static void SaveBackupFile(cSapModel model)
  {
    string CSIModelfilePath = model.GetModelFilename(true);
    string CSIModelFolder = Path.GetDirectoryName(CSIModelfilePath);
    if (string.IsNullOrEmpty(CSIModelFolder))
    {
      // CSI model is probably not saved, so speckle shouldn't do much
      return;
    }

    string fileExtension = CSIModelfilePath.Split('.').Last();
    string CSIFileName = Path.GetFileNameWithoutExtension(CSIModelfilePath);
    string speckleFolderPath = Path.Combine(CSIModelFolder, "speckle");

    var backups = new List<(DateTime, string)>();
    foreach (var fileName in Directory.GetFiles(speckleFolderPath))
    {
      if (
        fileName.Contains($"{CSIFileName}_speckleBackup")
        && fileName.Split('.').Last().ToLower() == fileExtension.ToLower()
      )
      {
        backups.Add((File.GetLastWriteTime(fileName), fileName));
      }
    }

    if (backups.Count < 3)
    {
      model.File.Save(
        Path.Combine(speckleFolderPath, $"{CSIFileName}_speckleBackup{backups.Count + 1}.{fileExtension}")
      );
    }
    else
    {
      var oldestBackup = backups.Min(o => o.Item1);
      var oldestFileName = backups.Where(o => o.Item1 == oldestBackup).First().Item2;
      model.File.Save(oldestFileName);
    }

    // save back as the original file
    model.File.Save(CSIModelfilePath);
  }
}
