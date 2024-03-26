namespace Speckle.Automate.Sdk.Schema;

/// <summary>
/// Required data to run a function.
/// </summary>
/// <typeparam name="T"> Type for <see cref="FunctionInputs"/>.</typeparam>
public class FunctionRunData<T>
{
  public string SpeckleToken { get; set; }
  public AutomationRunData AutomationRunData { get; set; }
  public T? FunctionInputs { get; set; }
}
