using System.Runtime.Serialization;
using Speckle.Newtonsoft.Json;

namespace Speckle.Automate.Sdk.Schema;

public static class FunctionRunDataParser
{
  /// <summary>
  /// Function run data parser from json file path./>
  /// </summary>
  /// <param name="inputLocation"> Path to retrieve function run data.</param>
  /// <typeparam name="T"> Type for function inputs.</typeparam>
  /// <returns>The data to be able run function.</returns>
  /// <exception cref="SerializationException"> Throws if deserialized object is null.</exception>
  /// <exception cref="FileNotFoundException"> Throws unless file exists.</exception>
  public static FunctionRunData<T> FromPath<T>(string inputLocation)
  {
    string inputJsonString = ReadInputData(inputLocation);
    FunctionRunData<T>? functionRunData = JsonConvert.DeserializeObject<FunctionRunData<T>>(inputJsonString);

    if (functionRunData is null)
    {
      throw new SerializationException($"Function run data couldn't deserialized at {inputLocation}");
    }

    return functionRunData;
  }

  /// <summary>
  /// Read text from file.
  /// </summary>
  /// <param name="inputLocation"> Path to check file is exist.</param>
  /// <returns>Text in file.</returns>
  /// <exception cref="FileNotFoundException"> Throws unless file exists.</exception>
  private static string ReadInputData(string inputLocation)
  {
    if (!File.Exists(inputLocation))
    {
      throw new FileNotFoundException($"Cannot find the function inputs file at {inputLocation}");
    }

    return File.ReadAllText(inputLocation);
  }
}
