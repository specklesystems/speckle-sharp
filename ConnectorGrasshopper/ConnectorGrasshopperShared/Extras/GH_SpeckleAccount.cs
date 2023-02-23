﻿using System;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Speckle.Core.Credentials;

namespace ConnectorGrasshopper.Extras
{
  public class GH_SpeckleAccountGoo: GH_Goo<Account>
  {
    public GH_SpeckleAccountGoo()
    {
    }
    public GH_SpeckleAccountGoo(Account account)
    {
      m_value = account;
    }
    
    public GH_SpeckleAccountGoo(string userId)
    {
      m_value = AccountManager.GetAccounts().First(acc => acc.id == userId);
    }
    
    public override IGH_Goo Duplicate()
    {
      return new GH_SpeckleAccountGoo(m_value);
    }

    public override string ToString()
    {
      return m_value.ToString();
    }

    public override bool IsValid => m_value != null;
    public override string TypeName => "GH_SpeckleAccount";
    public override string TypeDescription => "An account belonging to a user in a Speckle server";

    public override bool CastFrom(object source)
    {
      if (source is GH_String ghString)
      {
        try
        {
          Value = AccountManager.GetAccounts().First(acc => acc.userInfo.id == ghString.Value);
          return true;
        }
        catch (Exception e) // TODO: Handle this exception instead of ignoring it
        {
        }
      }
      if(source is string userId)
      {
        try
        {
          Value = AccountManager.GetAccounts().First(acc => acc.id == userId);
          return true;
        }
        catch (Exception e) // TODO: Handle this exception instead of ignoring it
        {
        }
      }

      if (source is Account account)
      {
        Value = account;
        return true;
      }
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
    public SpeckleAccountParam() : base("Speckle Account", "A", "A speckle account", ComponentCategories.PRIMARY_RIBBON, ComponentCategories.ACCOUNTS, GH_ParamAccess.item)
    {
    }

    public override GH_Exposure Exposure => GH_Exposure.hidden;
    
    public override Guid ComponentGuid => new Guid("2092EB37-5EA1-4779-BC4E-12074523228E");

    protected override Bitmap Icon => Properties.Resources.AccountParam;
  }
}
