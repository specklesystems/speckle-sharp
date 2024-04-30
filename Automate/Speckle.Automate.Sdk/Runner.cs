using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;
using Speckle.Automate.Sdk.DataAnnotations;
using Speckle.Automate.Sdk.Schema;
using Speckle.Core.Logging;

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
    AutomationContext automationContext = await AutomationContext
      .Initialize(automationRunData, speckleToken)
      .ConfigureAwait(false);

    try
    {
      await automateFunction(automationContext, inputs).ConfigureAwait(false);
      if (automationContext.RunStatus is not ("FAILED" or "SUCCEEDED"))
      {
        automationContext.MarkRunSuccess(
          "WARNING: Automate assumed a success status, but it was not marked as so by the function."
        );
      }
    }
    catch (Exception ex) when (!ex.IsFatal())
    {
      Console.WriteLine(ex.ToString());
      automationContext.MarkRunFailed("Function error. Check the automation run logs for details.");
    }
    finally
    {
      if (automationContext.ContextView is null)
      {
        automationContext.SetContextView();
      }

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
  public static async Task<int> Main(string[] args, Func<AutomationContext, Task> automateFunction)
  {
    return await Main(
        args,
        async (AutomationContext context, Fake _) => await automateFunction(context).ConfigureAwait(false)
      )
      .ConfigureAwait(false);
  }

  /// <summary>
  /// Main entrypoint to execute an Automate function with input data of type <typeparamref name="TInput"/>.
  /// </summary>
  /// <param name="args">The command line arguments passed into the function by automate</param>
  /// <param name="automateFunction">The automate function to execute</param>
  /// <typeparam name="TInput">The provided input data</typeparam>
  /// <remarks>This should always be called in your own functions, as it contains the logic to trigger the function automatically.</remarks>
  public static async Task<int> Main<TInput>(string[] args, Func<AutomationContext, TInput, Task> automateFunction)
    where TInput : struct
  {
    int returnCode = 0; // This is the CLI return code, defaults to 0 (Success), change to 1 to flag a failed run.

    Argument<string> pathArg = new(name: "Input Path", description: "A file path to retrieve function inputs");
    RootCommand rootCommand = new();

    rootCommand.AddArgument(pathArg);
    rootCommand.SetHandler(
      async (inputPath) =>
      {
        FunctionRunData<TInput>? data = FunctionRunDataParser.FromPath<TInput>(inputPath);

        AutomationContext context = await RunFunction(
            automateFunction,
            data.AutomationRunData,
            data.SpeckleToken,
            data.FunctionInputs
          )
          .ConfigureAwait(false);

        if (context.RunStatus != AutomationStatusMapping.Get(AutomationStatus.Succeeded))
        {
          returnCode = 1; // Flag run as failed.
        }
      },
      pathArg
    );

    Argument<string> schemaFilePathArg =
      new(name: "Function inputs file path", description: "A token to talk to the Speckle server with");

    Command generateSchemaCommand = new("generate-schema", "Generate JSON schema for the function inputs");
    generateSchemaCommand.AddArgument(schemaFilePathArg);
    generateSchemaCommand.SetHandler(
      (schemaFilePath) =>
      {
        JSchemaGenerator generator = new() { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        generator.GenerationProviders.Add(new SpeckleSecretProvider());
        JSchema schema = generator.Generate(typeof(TInput));
        schema.ToString(global::Newtonsoft.Json.Schema.SchemaVersion.Draft2019_09);
        File.WriteAllText(schemaFilePath, schema.ToString());
      },
      schemaFilePathArg
    );
    rootCommand.Add(generateSchemaCommand);

    await rootCommand.InvokeAsync(args).ConfigureAwait(false);

    return returnCode;
  }
}

public class SpeckleSecretProvider : JSchemaGenerationProvider
{
  public override JSchema GetSchema(JSchemaTypeGenerationContext context)
  {
    var attributes = context.MemberProperty?.AttributeProvider?.GetAttributes(false) ?? new List<Attribute>();
    var isSecretString = attributes.Any(att => att is SecretAttribute);

    if (isSecretString)
    {
      return CreateSchemaWithWriteOnly(context.ObjectType, context.Required);
    }

    return null;
  }

  private JSchema CreateSchemaWithWriteOnly(Type type, Required required)
  {
    JSchemaGenerator generator = new();
    JSchema schema = generator.Generate(type, required != Required.Always);

    schema.WriteOnly = true;

    return schema;
  }
}
