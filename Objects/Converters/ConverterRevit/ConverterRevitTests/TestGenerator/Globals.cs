using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TestGenerator
{
  public static class Globals
  {
    public static string TestModelsFolderPath(string basePath)
    {
      return Path.Combine(basePath, "..", "..", "TestModels");
    }

    public static string TestModelsFolderForRevitVersion(string basePath, string year)
    {
      return Path.Combine(TestModelsFolderPath(basePath), year);
    }
  }
}
