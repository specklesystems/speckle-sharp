using System.Collections.Generic;
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
  }
}
