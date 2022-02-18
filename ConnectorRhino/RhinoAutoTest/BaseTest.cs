using System;
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

namespace RhinoAutoTest
{
    public class BaseTest: IDisposable
    {
        protected RhinoTestFixture _fixture;
        
        public BaseTest(RhinoTestFixture fixture)
        {
          Console.WriteLine("Speckle - BaseTest created!");
          _fixture = fixture;
        }

        public static async Task<string> SendAllElements(string filePath)
        {
          var config = RhinoConfig.LoadConfig(filePath);
          var doc = RhinoDoc.Open(config.WorkingFolder + "\\" + config.SourceFile, out var alreadyOpen);

          var kit = KitManager.GetDefaultKit();
          var converter = kit.LoadConverter(Applications.Rhino7);

          var enumerable = AccountManager.GetAccounts();
          var acc = enumerable.FirstOrDefault(a => a.id == config.SenderId);
          var client = new Client(acc);
          var transport = new ServerTransport(acc, config.TargetStream);
          converter.SetContextDocument(doc);
          
          

          var elements = doc.Objects.Select(o => o.Geometry);

          var speckleObjects = new List<Base>();
          foreach (var rhinoObject in elements)
          {
            if (!converter.CanConvertToSpeckle(rhinoObject))
              continue;
            var converted = converter.ConvertToSpeckle(rhinoObject);
            speckleObjects.Add(converted);
          }

          var obj = new Base();
          obj[ "convertedObjects" ] = speckleObjects;
          var objectId = Operations.Send(obj, new List<ITransport> { transport }, false, disposeTransports: true).Result;
          var commitId = client.CommitCreate(new CommitCreateInput
          {
            branchName = config.TargetBranch,
            objectId = objectId,
            sourceApplication = Applications.Rhino7,
            streamId = config.TargetStream
          }).Result;
          Console.WriteLine("Speckle - Sent!!!");
          doc.Dispose();
          return commitId;
        }

        public void Dispose()
        {
          Console.WriteLine("SPeckle - Disposing!");
          _fixture?.Dispose();
          Console.WriteLine("Speckle - disposed");
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
