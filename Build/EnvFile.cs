using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Build;

public static class EnvFile
{
  public static Dictionary<string, string> Parse(string path)
  {
    Dictionary<string, string> data = new();

    if (!File.Exists(path))
    {
      throw new FileNotFoundException(path);
    }

    using var reader = File.OpenText(path);
    string? line;
    while ((line = reader.ReadLine()) != null)
    {
      var values = line.Split("=", StringSplitOptions.RemoveEmptyEntries);
      if (values.Length < 2)
      {
        continue;
      }
      data.Add(values[0], string.Join('=', values.Skip(1)));
    }

    return data;
  }
}
