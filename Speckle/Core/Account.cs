using System;
namespace Speckle.Core
{
  public class Account
  {
    public string Email { get; set; }
    public string ServerName { get; set; }
    public string ServerUrl { get; set; }
    public string ApiToken { get; set; }

    public Account() { }
  }
}
