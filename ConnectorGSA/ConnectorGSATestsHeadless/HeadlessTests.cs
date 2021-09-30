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
    //private string rxStreamId = "44ac187897";
    private string rxStreamId = "789a7f35e6";
    private string rxServerUrl = "https://speckle.xyz";
      
    private string saveAsAlternativeFilepath(string fn)
    {
      return Path.Combine(TestDataDirectory, fn.Split('.').First() + "_test.gwb");
    }

    protected string TestDataDirectory { get => AppDomain.CurrentDomain.BaseDirectory.TrimEnd(new[] { '\\' }) + @"\..\..\..\TestModels\"; }

    protected string modelWithoutResultsFile = "Structural Demo.gwb";
    protected string modelWithResultsFile = "Structural Demo Results.gwb";

    protected string v2ServerUrl = "https://v2.speckle.arup.com";

    #region send
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

    [Fact]
    public void HeadlessSendLargeModelWithResults()
    {
      var resultTypes = new List<string>()
      {
        "Nodal Displacements",
        "Nodal Velocity",
        "Nodal Acceleration",
        "Nodal Reaction",
        "Constraint Forces",
        "Element 1d Displacement",
        "Element 1d Force",
        "Element 2d Displacement",
        "Element 2d ProjectedMoment",
        "Element 2d Projected Force",
        "Element 2d Projected StressBottom",
        "Element 2d Projected StressMiddle",
        "Element 2d Projected StressTop",
        "Assembly ForcesAndMoments"
      };
      var headless = new Headless();
      var account = AccountManager.GetDefaultAccount();
      var cliResult = headless.RunCLI("sender",
        "--server", account.serverInfo.url,
        "--email", account.userInfo.email,
        "--token", account.token,
        "--file", @"C:\Temp\200518 SJC v10.1.gwb",
        "--saveAs", saveAsAlternativeFilepath(@"C:\Temp\200518 SJC v10.1.modified.gwb"),
        "--result", string.Join(",", resultTypes.Select(rt => "\"" + rt + "\"")),
        "--resultCases", "A1,C1",
        "--result1DNumPosition", "3");

      Assert.True(cliResult);
    }
    #endregion

    #region receive
    [Fact]
    public void HeadlessReceiveBothModels()
    {
      var headless = new Headless();
      var account = AccountManager.GetDefaultAccount();
      var cliResult = headless.RunCLI("receiver",
        "--server", account.serverInfo.url,
        "--email", account.userInfo.email,
        "--token", account.token,
        "--file", Path.Combine(TestDataDirectory, "Received.gwb"),
        "--streamIDs", rxStreamId,
        "--nodeAllowance", "0.1");

      Assert.True(cliResult);
    }
    #endregion
  }
}
