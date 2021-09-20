using ConnectorGSA;
using Speckle.Core.Credentials;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ConnectorGSATestsHeadless
{
  public class HeadlessTests
  {
    private string saveAsAlternativeFilepath(string fn)
    {
      return Path.Combine(TestDataDirectory, fn.Split('.').First() + "_test.gwb");
    }

    protected string TestDataDirectory { get => AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new[] { '\\' }) + @"\..\..\..\TestModels\"; }

    protected string modelWithoutResultsFile = "Structural Demo.gwb";
    protected string modelWithResultsFile = "Structural Demo Results.gwb";

    protected string v2ServerUrl = "https://v2.speckle.arup.com";

    [Fact]
    public void HeadlessSendDesignLayer()
    {
      var headless = new Headless();
      var cliResult = headless.RunCLI("sender",
        "--server", v2ServerUrl,
        "--file", Path.Combine(TestDataDirectory, modelWithoutResultsFile),
        "--saveAs", saveAsAlternativeFilepath(modelWithoutResultsFile),
        "--designLayerOnly");

      Assert.True(cliResult);
    }

    [Fact]
    public void HeadlessSendBothLayers()
    {
      var headless = new Headless();
      var account = AccountManager.GetDefaultAccount();
      var cliResult = headless.RunCLI("sender",
        "--server", account.serverInfo.url,
        "--email", account.userInfo.email,
        "--token", account.token,
        "--file", Path.Combine(TestDataDirectory, modelWithoutResultsFile),
        "--saveAs", saveAsAlternativeFilepath(modelWithoutResultsFile));

      Assert.True(cliResult);
    }

    [Fact]
    public void HeadlessSendBothLayersWithResults()
    {
      var headless = new Headless();
      var account = AccountManager.GetDefaultAccount();
      var cliResult = headless.RunCLI("sender",
        "--server", account.serverInfo.url,
        "--email", account.userInfo.email,
        "--token", account.token,
        "--file", Path.Combine(TestDataDirectory, modelWithResultsFile),
        "--saveAs", saveAsAlternativeFilepath(modelWithResultsFile),
        "--result", "\"Nodal Displacements\",\"Element 1d Force\",\"Element 2d Displacement\",\"Assembly Forces And Moments\"",
        "--resultCases", "A1,C1",
        "--result1DNumPosition", "3");

      Assert.True(cliResult);
    }
  }
}
