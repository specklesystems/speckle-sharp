using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Rhino;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Xunit;

namespace IndustrialConstructionUnitTests
{
    public class BaseTest
    {
        protected RhinoTestFixture _fixture;
        
        public BaseTest(RhinoTestFixture fixture)
        {
            _fixture = fixture;
        }

        public static async Task<string> SendAllElements(string filePath)
        {
          var config = RhinoConfig.LoadConfig(filePath);

          var kit = KitManager.GetDefaultKit();
          var converter = kit.LoadConverter(Applications.Rhino7);
          var doc = RhinoDoc.Open(config.WorkingFolder + "\\" + config.SourceFile, out var alreadyOpen);
          converter.SetContextDocument(doc);

          var elements = doc.Objects.ToList();

          return "yay!!";
        }
    }
    
    class RhinoConfig
    {
      public string SourceFile { get; set; }
      public string WorkingFolder { get; set; }
      public string TargetStream { get; set; }
      public string TargetBranch { get; set; }
      public string SenderId { get; set; }

      public static RhinoConfig LoadConfig(string filePath)
      {
        var workingFolder = Path.GetDirectoryName(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var configPath = Path.Combine(workingFolder, $"{fileName}.config.json");
        if (!File.Exists(configPath)) throw new FileNotFoundException(configPath);

        var revitConfig = JsonSerializer.Deserialize<RhinoConfig>(File.ReadAllText(configPath));
        return revitConfig;
      }
    }

}
