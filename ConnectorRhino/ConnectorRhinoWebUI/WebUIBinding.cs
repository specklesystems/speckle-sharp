namespace ConnectorRhinoWebUI
{
  internal class WebUIBinding
  {
    public string GetAccounts()
    {
      var accountString = System.Text.Json.JsonSerializer.Serialize(Speckle.Core.Credentials.AccountManager.GetAccounts());
      return accountString;
    }

    public string SayHi(string name)
    {
      return $"Hi {name}! Hope you have a great day.";
    }

    public string GetSourceAppName() => "Rhino";

  }
}
