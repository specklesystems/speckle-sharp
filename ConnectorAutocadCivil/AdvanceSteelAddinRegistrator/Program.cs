using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AdvanceSteelAddinRegistrator
{
  internal class Program
  {
    private static string _year = null;
    static string manifestBasePath => $"C:\\Program Files\\Autodesk\\AutoCAD {_year}\\ADVS\\Addons\\";
    static string addinPath => $"C:\\Program Files\\Autodesk\\Advance Steel Speckle Connector\\{_year}\\";
    private static string manifest => Path.Combine(manifestBasePath, "MyAddons.xml");
    const string addinDllName = "SpeckleConnectorAdvanceSteel.dll";
    const string addinName = "Speckle";

    enum ExitCode : int
    {
      Success = 0,
      ErrorParsingXml = 1,
      ErrorUpdatingXml = 2,
      ErrorWritingXml = 3,
      NoAdminRights = 4,
      Unknown = 5,
      NoYearArgument = 6
    }
    static int Main(string[] args)
    {
      if (args == null || !args.Any() || args[0].Length!=4)
      {
        Console.WriteLine("Please provide a Year argument (eg: '202X')");
        return (int)ExitCode.NoYearArgument;
      }

      _year = args[0];
      
      
      AddonsData addonsData = new AddonsData();

      try
      {

        if (File.Exists(manifest))
          addonsData = ParseManifestFile();

        if (!Directory.Exists(manifestBasePath))
          Directory.CreateDirectory(manifestBasePath);

        addonsData = UpdateManifestFile(addonsData);

        if (addonsData != null && addonsData.Addons.Any(x => x.Name == addinName))
          WriteManifestFile(addonsData);
      }
      catch (Exception ex)
      {
        if (ex.InnerException != null && ex.InnerException is System.UnauthorizedAccessException)
        {
          Console.WriteLine("Admin Rights required to continue.");
          return (int)ExitCode.NoAdminRights;
        }

        else if (ex.Message.Contains("updating"))
        {
          Console.WriteLine(ex.Message);
          return (int)ExitCode.ErrorUpdatingXml;
        }

        else if (ex.Message.Contains("parsing"))
        {
          Console.WriteLine(ex.Message);
          return (int)ExitCode.ErrorParsingXml;
        }

        else if (ex.Message.Contains("writing"))
        {
          Console.WriteLine(ex.Message);
          return (int)ExitCode.ErrorWritingXml;
        }

        else
        {
          Console.WriteLine(ex.Message);
          return (int)ExitCode.Unknown;
        }

      }
      return (int)ExitCode.Success;
    }

    private static AddonsData UpdateManifestFile(AddonsData addonsData)
    {
      try
      {
        //safety check
        if (addonsData.Addons == null || !addonsData.Addons.Any())
        {
          addonsData.Addons = new AddonsDataAddon[] { new AddonsDataAddon() };
        }

        if (addonsData.Addons.Any(x => x.Name == addinName))
        {
          Console.WriteLine($"{addinName} addin already registered");
          return addonsData;
        }

        addonsData.Addons = addonsData.Addons.Append(new AddonsDataAddon() { Name = addinName, FullPath = Path.Combine(addinPath, addinDllName) }).ToArray();
      }
      catch (Exception ex)
      {
        throw new Exception($"Error updating manifest: {ex.Message}", ex);
      }
      return addonsData;
    }


    private static AddonsData ParseManifestFile()
    {

      AddonsData addonsData = null;
      try
      {

        using (var stream = new FileStream(manifest, FileMode.Open))
        {
          var serializer = new XmlSerializer(typeof(AddonsData));
          addonsData = serializer.Deserialize(stream) as AddonsData;
        }
      }
      catch (Exception ex)
      {
        throw new Exception($"Error parsing manifest: {ex.Message}", ex);
      }

      return addonsData;

    }

    private static void WriteManifestFile(AddonsData addonsData)
    {
      try
      {
        using (var stream = new FileStream(manifest, FileMode.Create))
        {
          var serializer = new XmlSerializer(typeof(AddonsData));
          serializer.Serialize(stream, addonsData);
          stream.Close();
        }
      }
      catch (Exception ex)
      {
        throw new Exception($"Error writing manifest: {ex.Message}", ex);
      }
    }
  }
}
