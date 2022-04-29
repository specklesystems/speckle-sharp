public class TestExecutor
{
    private TestContext context;

  public TestExecutor(TestContext context)
  {
    this.context = context;
  }

  public async Task<TestResult> Execute()
    {
        // test workflow
        // 1. get test configuration -> AutotestConfig: app support matrix
        // 2. initialize available apps based on config -> usable application handler registry
        // 3. build executable test pipelines from config and available apps
        // 4. execute test pipelines:
        //   a. send a context object through the test pipeline to trace the steps of the workflow
        //      ie: count the objects 
      return new TestResult(context.StreamLink);
    }

    public async Task Dispose()
    {
      context.Dispose();
    }

    public static TestExecutor Initialize(TestContext context) {
        return new TestExecutor(context);
    }
} 

public class TestResult
{
    public string StreamUrl;

  public TestResult(string streamUrl)
  {
    StreamUrl = streamUrl;
  }
}