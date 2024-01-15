using System;
using System.Drawing;
using System.Linq;
using ConnectorGrasshopper.Properties;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Speckle.Core.Credentials;
using Speckle.Core.Logging;

namespace ConnectorGrasshopper.Extras;

public class GH_SpeckleAccountGoo : GH_Goo<Account>
{
  public GH_SpeckleAccountGoo() { }

  public GH_SpeckleAccountGoo(Account account)
  {
    m_value = account;
  }

  [Obsolete("Implementation is faulty", true)]
  public GH_SpeckleAccountGoo(string userId)
  {
    m_value = AccountManager.GetAccounts().First(acc => acc.id == userId);
  }

  public override bool IsValid => m_value != null;
  public override string TypeName => "GH_SpeckleAccount";
  public override string TypeDescription => "An account belonging to a user in a Speckle server";

  public override IGH_Goo Duplicate()
  {
    return new GH_SpeckleAccountGoo(m_value);
  }

  public override string ToString()
  {
    return m_value.ToString();
  }

  public override bool CastFrom(object source)
  {
    var temp = string.Empty;
    if (source is GH_String ghString)
    {
      temp = ghString.Value;
    }
    else if (source is string text)
    {
      temp = text;
    }

    if (!string.IsNullOrEmpty(temp))
    {
      var searchResult = AccountManager
        .GetAccounts()
        .FirstOrDefault(acc => $"{acc.serverInfo.url}?u={acc.userInfo.id}" == temp);

      if (searchResult == null)
      {
        SpeckleLog.Logger.Warning("Failed to get local account with same server+id combination as the provided string");
        SpeckleLog.Logger.Information("Attempting match by ID only for backwards compatibility.");

        searchResult = AccountManager.GetAccounts().FirstOrDefault(acc => acc.userInfo.id == temp);
      }

      if (searchResult != null)
      {
        Value = searchResult;
        return true;
      }

      return false;
    }

    if (source is Account account)
    {
      Value = account;
      return true;
    }

    // Last ditch attempt, tries to use the casting logic of the parent.
    return base.CastFrom(source);
  }

  public override bool CastTo<Q>(ref Q target)
  {
    var type = typeof(Q);
    if (type == typeof(GH_SpeckleAccountGoo))
    {
      target = (Q)(object)new GH_SpeckleAccountGoo(m_value);
      return true;
    }

    if (type == typeof(Account))
    {
      target = (Q)(object)m_value;
      return true;
    }

    return base.CastTo(ref target);
  }
}

public class SpeckleAccountParam : GH_Param<GH_SpeckleAccountGoo>
{
  public SpeckleAccountParam()
    : base(
      "Speckle Account",
      "A",
      "A speckle account",
      ComponentCategories.PRIMARY_RIBBON,
      ComponentCategories.ACCOUNTS,
      GH_ParamAccess.item
    ) { }

  public override GH_Exposure Exposure => GH_Exposure.hidden;

  public override Guid ComponentGuid => new("2092EB37-5EA1-4779-BC4E-12074523228E");

  protected override Bitmap Icon => Resources.AccountParam;
}
