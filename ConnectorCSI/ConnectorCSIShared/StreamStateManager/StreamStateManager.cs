using CSiAPIv1;
using DesktopUI2.Models;
using Speckle.Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ConnectorCSI.Storage
{
  public static class StreamStateManager
  {
    private static string _speckleFilePath;
    public static List<StreamState> ReadState(cSapModel model)
    {
      var strings = ReadSpeckleFile(model);
      if (strings == "")
      {
        return new List<StreamState>();
      }
      try
      {
        return JsonConvert.DeserializeObject<List<StreamState>>(strings);
      }
      catch
      {
        return new List<StreamState>();
      }
    }

    /// <summary>
    /// Writes the stream states to the <CSIModelName>.txt file in speckle folder
    /// that exists or is created in the folder where the CSI model exists.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="streamStates"></param>
    public static void WriteStreamStateList(cSapModel model, List<StreamState> streamStates)
    {
      if (_speckleFilePath == null) GetOrCreateSpeckleFilePath(model);
      FileStream fileStream = new FileStream(_speckleFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
      try
      {
        using (var streamWriter = new StreamWriter(fileStream))
        {
          streamWriter.Write(JsonConvert.SerializeObject(streamStates) as string);
          streamWriter.Flush();
        }
      }
      catch { }
    }

    public static void ClearStreamStateList(cSapModel model)
    {
      if (_speckleFilePath == null) GetOrCreateSpeckleFilePath(model);
      FileStream fileStream = new FileStream(_speckleFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
      try
      {
        fileStream.SetLength(0);
      }
      catch { }
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
      if (CSIModelfilePath == "")
      {
        // CSI model is probably not saved, so speckle shouldn't do much
        _speckleFilePath = null;
        return;
      }
      string CSIFileName = Path.GetFileNameWithoutExtension(CSIModelfilePath);
      string CSIModelFolder = Path.GetDirectoryName(CSIModelfilePath);
      string speckleFolderPath = Path.Combine(CSIModelFolder, "speckle");
      string speckleFilePath = Path.Combine(CSIModelFolder, "speckle", $"{CSIFileName}.txt");
      try
      {
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
      catch
      {
        _speckleFilePath = null;
        return;
      }
    }

    /// <summary>
    /// Reads the "/speckle/<CSIModelName>.txt" file and returns the string in it
    /// </summary>
    /// <param name="model"></param>
    private static string ReadSpeckleFile(cSapModel model)
    {
      if (_speckleFilePath == null)
        GetOrCreateSpeckleFilePath(model);

      if (_speckleFilePath == null) return "";
      FileStream fileStream = new FileStream(_speckleFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
      try
      {
        using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
        {
          return streamReader.ReadToEnd();
        }
      }
      catch { return ""; }
    }

  }
}