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
      automationContext.MarkRunException("Function error. Check the automation run logs for details.");
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
  ) =>
    await RunFunction(
        async (context, _) => await automateFunction(context).ConfigureAwait(false),
        automationRunData,
        speckleToken,
        new Fake()
      )
      .ConfigureAwait(false);

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
    Argument<string> pathArg = new(name: "Input Path", description: "A file path to retrieve function inputs");
    RootCommand rootCommand = new();

    // a stupid hack to be able to exit with a specific integer exit code
    // read more at https://github.com/dotnet/command-line-api/issues/1570
    var exitCode = 0;

    rootCommand.AddArgument(pathArg);
    rootCommand.SetHandler(
      async inputPath =>
      {
        FunctionRunData<TInput> data = FunctionRunDataParser.FromPath<TInput>(inputPath);

        var context = await RunFunction(
            automateFunction,
            data.AutomationRunData,
            data.SpeckleToken,
            data.FunctionInputs
          )
          .ConfigureAwait(false);

        exitCode = context.RunStatus == "EXCEPTION" ? 1 : 0;
      },
      pathArg
    );

    Argument<string> schemaFilePathArg =
      new(name: "Function inputs file path", description: "A token to talk to the Speckle server with");

    Command generateSchemaCommand = new("generate-schema", "Generate JSON schema for the function inputs");
    generateSchemaCommand.AddArgument(schemaFilePathArg);
    generateSchemaCommand.SetHandler(
      schemaFilePath =>
      {
        JSchemaGenerator generator = new() { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        generator.GenerationProviders.Add(new SpeckleSecretProvider());
        JSchema schema = generator.Generate(typeof(TInput));
        schema.ToString(SchemaVersion.Draft2019_09);
        File.WriteAllText(schemaFilePath, schema.ToString());
      },
      schemaFilePathArg
    );
    rootCommand.Add(generateSchemaCommand);

    await rootCommand.InvokeAsync(args).ConfigureAwait(false);

    // if we've gotten this far, the execution should technically be completed as expected
    // thus exiting with 0 is the semantically correct thing to do
    return exitCode;
  }
}

public class SpeckleSecretProvider : JSchemaGenerationProvider
{
  // `GetSchema` returning `null` indicates that the given type should not have a customised schema
  // Nullability of JSchemaTypeGenerationContext appears to be incorrect.
#pragma warning disable CS8764 // Nullability of return type doesn't match overridden member (possibly because of nullability attributes).
  public override JSchema? GetSchema(JSchemaTypeGenerationContext context)
  {
    var attributes = context.MemberProperty?.AttributeProvider?.GetAttributes(false) ?? new List<Attribute>();
    var isSecretString = attributes.Any(att => att is SecretAttribute);

    if (isSecretString)
    {
      return CreateSchemaWithWriteOnly(context.ObjectType, context.Required);
    }

    return null;
  }
#pragma warning restore CS8764 // Nullability of return type doesn't match overridden member (possibly because of nullability attributes).


  private JSchema CreateSchemaWithWriteOnly(Type type, Required required)
  {
    JSchemaGenerator generator = new();
    JSchema schema = generator.Generate(type, required != Required.Always);

    schema.WriteOnly = true;

    return schema;
  }
}
