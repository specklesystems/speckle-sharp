using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConverterRevitTests
{
  public static class Globals
  {
    public static string GetTestModel(string filename)
    {

      var path = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "TestModels", filename);
      return path;

    }
  }
}
