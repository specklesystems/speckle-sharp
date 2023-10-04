using System.CommandLine;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;
using Speckle.Newtonsoft.Json;

namespace SpeckleAutomate;

public static class AutomationRunner
{
  public static async Task<AutomationContext> RunFunction<T>(
      Func<AutomationContext, T, Task> automateFunction,
      AutomationRunData automationRunData,
      string speckleToken,
      T inputs
  ) where T : struct
  {
    var automationContext = await AutomationContext.Initialize(automationRunData, speckleToken).ConfigureAwait(false);

    try
    {
      await automateFunction(automationContext, inputs).ConfigureAwait(false);
      if (automationContext.RunStatus is not ("FAILED" or "SUCCEEDED"))
        automationContext.MarkRunSuccess(
          "WARNING: Automate assumed a success status, but it was not marked as so by the function."
        );

    }
    catch (Exception e)
    {
      Console.WriteLine(e.ToString());
      automationContext.MarkRunFailed(
          "Function error. Check the automation run logs for details."
          );
    }
    finally
    {
      await automationContext.ReportRunStatus().ConfigureAwait(false);
    }
    return automationContext;


  }

  public static async Task<AutomationContext> RunFunction(
      Func<AutomationContext, Task> automateFunction,
      AutomationRunData automationRunData,
      string speckleToken
      )
  {
    return await RunFunction(
      async (context, fake) => await automateFunction(context).ConfigureAwait(false),
      automationRunData, 
      speckleToken,
      new Fake()
      ).ConfigureAwait(false);
  }
  
  private struct Fake{}

  
  public static async Task Main(string[] args, Func<AutomationContext, Task> automateFunction)
  {
    await Main(
      args,
      async (AutomationContext context, Fake fake) => await automateFunction(context)
        .ConfigureAwait(false)).ConfigureAwait(false);
  }
  
  public static async Task Main<T>(string[] args, Func<AutomationContext, T, Task> automateFunction) where T : struct
  {

    var speckleProjectDataArg = new Argument<string>(
      name: "Speckle project data",
      description: "The values of the project / model / version that triggered this function"
    );
    var functionInputsArg = new Argument<string>(
      name: "Function inputs",
      description: "The values provided by the function user, matching the function input schema"
    );
    var speckleTokenArg = new Argument<string>(
      name: "Speckle token",
      description: "A token to talk to the Speckle server with"
    );
    var rootCommand = new RootCommand();
    rootCommand.AddArgument(speckleProjectDataArg);
    rootCommand.AddArgument(functionInputsArg);
    rootCommand.AddArgument(speckleTokenArg);
    rootCommand.SetHandler(
      async (speckleProjectData, functionInputs, speckleToken) =>
      {
        var automationRunData = JsonConvert.DeserializeObject<AutomationRunData>(speckleProjectData);
        var functionInputsParsed = JsonConvert.DeserializeObject<T>(functionInputs);


        await RunFunction(automateFunction, automationRunData, speckleToken, functionInputsParsed).ConfigureAwait(false);
      },
      speckleProjectDataArg,
      functionInputsArg,
      speckleTokenArg
    );

    var schemaFilePathArg = new Argument<string>(
      name: "Function inputs file path",
      description: "A token to talk to the Speckle server with"
    );

    var generateSchemaCommand = new Command(
      "generate-schema",
      "Generate JSON schema for the function inputs"
    );
    generateSchemaCommand.AddArgument(schemaFilePathArg);
    generateSchemaCommand.SetHandler(async (schemaFilePath) =>
    {
      var generator = new JSchemaGenerator
      {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
      };
      var schema = generator.Generate(typeof(T));
      schema.ToString(Newtonsoft.Json.Schema.SchemaVersion.Draft2019_09);
      await File.WriteAllTextAsync(schemaFilePath, schema.ToString()).ConfigureAwait(false);
    }, schemaFilePathArg);
    rootCommand.Add(generateSchemaCommand);

    await rootCommand.InvokeAsync(args).ConfigureAwait(false);
  }
}
