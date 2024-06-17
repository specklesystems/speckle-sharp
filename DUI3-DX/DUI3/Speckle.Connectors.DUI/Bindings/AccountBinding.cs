using Speckle.Connectors.DUI.Bridge;
using Speckle.Core.Credentials;

namespace Speckle.Connectors.DUI.Bindings;

public class AccountBinding : IBinding
{
  public string Name => "accountsBinding";
  public IBridge Parent { get; }

  public AccountBinding(IBridge bridge)
  {
    Parent = bridge;
  }

  public Account[] GetAccounts() => AccountManager.GetAccounts().ToArray();
}
