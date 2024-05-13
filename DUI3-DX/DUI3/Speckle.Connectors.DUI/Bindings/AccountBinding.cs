using Speckle.Connectors.DUI.Bridge;
using Speckle.Core.Credentials;

namespace Speckle.Connectors.DUI.Bindings;

public class AccountBinding : IBinding
{
  public string Name { get; set; } = "accountsBinding";
  public IBridge Parent { get; private set; }

  public AccountBinding(IBridge bridge)
  {
    Parent = bridge;
  }

  public Account[] GetAccounts() => AccountManager.GetAccounts().ToArray();
}
