using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;
using Speckle.Automate.Sdk.Schema;
using Speckle.Newtonsoft.Json;

namespace Speckle.Automate.Sdk;

/// <summary>
/// Provides mechanisms to execute any function that conforms to the AutomateFunction "interface"
/// </summary>
public static class AutomationRunner
{
  [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
  public static async Task<AutomationContext> RunFunction<TInput>(
    Func<AutomationContext, TInput, Task> automateFunction,
    AutomationRunData automationRunData,
    string speckleToken,
    TInput inputs
  )
    where TInput : struct
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
    catch (Exception ex)
    {
      Console.WriteLine(ex.ToString());
      automationContext.MarkRunFailed("Function error. Check the automation run logs for details.");
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
        async (context, _) => await automateFunction(context).ConfigureAwait(false),
        automationRunData,
        speckleToken,
        new Fake()
      )
      .ConfigureAwait(false);
  }

  private struct Fake { }

  /// <summary>
  /// Main entrypoint to execute an Automate function with no input data
  /// </summary>
  /// <param name="args">The command line arguments passed into the function by automate</param>
  /// <param name="automateFunction">The automate function to execute</param>
  /// <remarks>This should always be called in your own functions, as it contains the logic to trigger the function automatically.</remarks>
  public static async Task Main(string[] args, Func<AutomationContext, Task> automateFunction)
  {
    await Main(args, async (AutomationContext context, Fake _) => await automateFunction(context).ConfigureAwait(false))
      .ConfigureAwait(false);
  }

  /// <summary>
  /// Main entrypoint to execute an Automate function with input data of type <see cref="TInput"/>
  /// </summary>
  /// <param name="args">The command line arguments passed into the function by automate</param>
  /// <param name="automateFunction">The automate function to execute</param>
  /// <typeparam name="TInput">The provided input data</typeparam>
  /// <remarks>This should always be called in your own functions, as it contains the logic to trigger the function automatically.</remarks>
  public static async Task Main<TInput>(string[] args, Func<AutomationContext, TInput, Task> automateFunction)
    where TInput : struct
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
        var functionInputsParsed = JsonConvert.DeserializeObject<TInput>(functionInputs);

        await RunFunction(automateFunction, automationRunData, speckleToken, functionInputsParsed)
          .ConfigureAwait(false);
      },
      speckleProjectDataArg,
      functionInputsArg,
      speckleTokenArg
    );

    var schemaFilePathArg = new Argument<string>(
      name: "Function inputs file path",
      description: "A token to talk to the Speckle server with"
    );

    var generateSchemaCommand = new Command("generate-schema", "Generate JSON schema for the function inputs");
    generateSchemaCommand.AddArgument(schemaFilePathArg);
    generateSchemaCommand.SetHandler(
      async (schemaFilePath) =>
      {
        var generator = new JSchemaGenerator { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        var schema = generator.Generate(typeof(TInput));
        schema.ToString(global::Newtonsoft.Json.Schema.SchemaVersion.Draft2019_09);
        File.WriteAllText(schemaFilePath, schema.ToString());
      },
      schemaFilePathArg
    );
    rootCommand.Add(generateSchemaCommand);

    await rootCommand.InvokeAsync(args).ConfigureAwait(false);
  }
}
