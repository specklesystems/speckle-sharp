using System.Linq;
using JetBrains.Annotations;
using Speckle.Core.Credentials;

namespace DUI3.Bindings;

public class AccountBinding : IBinding
{
  public string Name { get; set; } = "accountsBinding";
  public IBridge Parent { get; set; }

  [PublicAPI]
  public Account[] GetAccounts()
  {
    return AccountManager.GetAccounts().ToArray();
    // NOTE: removing the avatars is no longer needed as we've resolved the issue described below via the bridge implementation.
    // .Select(
    // a =>
    // {
    //   a.userInfo.avatar = null; // removing this as the get accounts call was a too large string to do "executeScriptAsync" with (this was not happening if this was a direct return from a binding call).
    //   return a;
    // }).ToArray();
  }
}
