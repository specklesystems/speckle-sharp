# nullable enable
using System.ComponentModel.DataAnnotations;
using Speckle.Automate.Sdk;

// WARNING do not delete this call, this is the actual execution of your function
await AutomationRunner.Main<FunctionInputs>(args, AutomateFunction.Run).ConfigureAwait(false);


struct FunctionInputs
{
  [Required]
  public string ForbiddenSpeckleType { get; set; }
}

static class AutomateFunction
{
  public static async Task Run(AutomationContext automationContext, FunctionInputs functionInputs)
  {
    var versionRootObject =await automationContext.ReceiveVersion().ConfigureAwait(false);
    automationContext.AttachErrorToObjects("InvalidObjectType", new[] { versionRootObject.id }, $"This object has a forbidden speckle type {functionInputs.ForbiddenSpeckleType}");
    automationContext.MarkRunFailed("Invalid object type check failed.");
  }
}
