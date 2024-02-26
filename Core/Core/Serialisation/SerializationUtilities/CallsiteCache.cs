using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CSharp.RuntimeBinder;

namespace Speckle.Core.Serialisation.SerializationUtilities;

internal static class CallSiteCache
{
  // Adapted from the answer to
  // https://stackoverflow.com/questions/12057516/c-sharp-dynamicobject-dynamic-properties
  // by jbtule, https://stackoverflow.com/users/637783/jbtule
  // And also
  // https://github.com/mgravell/fast-member/blob/master/FastMember/CallSiteCache.cs
  // by Marc Gravell, https://github.com/mgravell
  private static readonly Dictionary<string, CallSite<Func<CallSite, object, object?, object>>> s_setters = new();

  public static void SetValue(string propertyName, object target, object? value)
  {
    lock (s_setters)
    {
      CallSite<Func<CallSite, object, object?, object>>? site;

      lock (s_setters)
      {
        if (!s_setters.TryGetValue(propertyName, out site))
        {
          var binder = Binder.SetMember(
            CSharpBinderFlags.None,
            propertyName,
            typeof(CallSiteCache),
            new List<CSharpArgumentInfo>
            {
              CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
              CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
            }
          );
          s_setters[propertyName] = site = CallSite<Func<CallSite, object, object?, object>>.Create(binder);
        }
      }

      site.Target.Invoke(site, target, value);
    }
  }
}
