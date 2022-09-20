using DesktopUI2.Models;
using Speckle.Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Tekla.Structures.Model;

namespace ConnectorTeklaStructures.Storage
{
  public static class StreamStateManager
  {
    private static string _speckleFilePath;
    public static List<StreamState> ReadState(Model model)
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
    /// Writes the stream states to the <TeklaStructuresModelName>.txt file in speckle folder
    /// that exists or is created in the folder where the TeklaStructures model exists.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="streamStates"></param>
    public static void WriteStreamStateList(Model model, List<StreamState> streamStates)
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

    public static void ClearStreamStateList(Model model)
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
            //string TeklaStructuresFileName = Path.GetFileNameWithoutExtension(TeklaStructuresModelfilePath);
            //string TeklaStructuresModelFolder = Path.GetDirectoryName(TeklaStructuresModelfilePath);
            //string speckleFolderPath = Path.Combine(TeklaStructuresModelFolder, "speckle");
            //string speckleFilePath = Path.Combine(TeklaStructuresModelFolder, "speckle", $"{TeklaStructuresFileName}.txt");
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
    /// Reads the "/speckle/<TeklaStructuresModelName>.txt" file and returns the string in it
    /// </summary>
    /// <param name="model"></param>
    private static string ReadSpeckleFile(Model model)
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