using Speckle.GSA.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConnectorGSA
{

  public class GsaMessenger : IGSALocalMessenger
  {
    public event EventHandler<MessageEventArgs> MessageAdded;

    private readonly object syncLock = new object();
    private List<MessageEventArgs> MessageCache = new List<MessageEventArgs>();

    public int LoggedMessageCount { get; private set; } = 0;

    public void ResetLoggedMessageCount()
    {
      lock (syncLock)
      {
        LoggedMessageCount = 0;
      }
    }

    public bool Append(IEnumerable<string> messagePortionsToMatch, IEnumerable<string> additional)
    {
      lock (syncLock)
      {
        if (MessageCache.Count() > 0 && messagePortionsToMatch != null && messagePortionsToMatch.Count() > 0 && additional != null && additional.Count() > 0)
        {
          //This uses LINQ which isn't efficient but when there are messages, there shouldn't be too many - there isn't a great need to make this very efficient
          var matching = MessageCache.Where(mc => (messagePortionsToMatch.All(mptm => mc.MessagePortions.Any(mp => mp.Equals(mptm, StringComparison.InvariantCultureIgnoreCase))))).ToList();
          foreach (var m in matching)
          {
            m.MessagePortions = m.MessagePortions.Concat(additional).ToArray();
          }
        }
      }
      return true;
    }

    public bool CacheMessage(MessageIntent intent, MessageLevel level, Exception ex, params string[] messagePortions)
    {
      lock (syncLock)
      {
        MessageCache.Add(new MessageEventArgs(intent, level, ex, messagePortions));
      }
      return true;
    }

    public bool CacheMessage(MessageIntent intent, MessageLevel level, params string[] messagePortions)
    {
      lock (syncLock)
      {
        MessageCache.Add(new MessageEventArgs(intent, level, messagePortions));
      }
      return true;
    }

    public bool Message(MessageIntent intent, MessageLevel level, params string[] messagePortions)
    {
      return CacheMessage(intent, level, messagePortions);
    }

    public bool Message(MessageIntent intent, MessageLevel level, Exception ex, params string[] messagePortions)
    {
      return CacheMessage(intent, level, ex, messagePortions);
    }

    public void Trigger()
    {
      ConsolidateCache();
      lock (syncLock)
      {
        foreach (var m in MessageCache)
        {
          MessageAdded?.Invoke(null, m);
          if (m.Intent == MessageIntent.TechnicalLog)
          {
            this.LoggedMessageCount++;
          }
        }
        MessageCache.Clear();
      }
    }

    public void ConsolidateCache()
    {
      var newCache = new List<MessageEventArgs>();
      //Currently just recognises the first two levels of message portions
      lock (syncLock)
      {
        var excludedFromConsolidation = MessageCache.Where(m => m.Exception != null || m.Intent == MessageIntent.TechnicalLog);
        //Let log messages not be consolidated
        newCache.AddRange(excludedFromConsolidation);

        var msgGroups = MessageCache.Except(excludedFromConsolidation).GroupBy(m => new { m.Intent, m.Level }).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var gk in msgGroups.Keys)
        {
          var msgDict = new Dictionary<string, List<string>>();
          foreach (var m in msgGroups[gk].Where(m => m.MessagePortions.Count() > 0))
          {
            var validPortions = m.MessagePortions.Where(mp => !string.IsNullOrEmpty(mp)).ToList();
            if (validPortions.Count() == 0)
            {
              continue;
            }
            var l1msg = validPortions.First();
            if (!msgDict.ContainsKey(l1msg))
            {
              msgDict.Add(l1msg, new List<string>());
            }
            if (validPortions.Count() > 1)
            {
              msgDict[l1msg].Add(validPortions[1]);
            }
          }
          if (msgDict.Keys.Count() > 0)
          {
            foreach (var k in msgDict.Keys)
            {
              if (msgDict[k].Count() == 0)
              {
                newCache.Add(new MessageEventArgs(gk.Intent, gk.Level, k));
              }
              else
              {
                newCache.Add(new MessageEventArgs(gk.Intent, gk.Level, k, string.Join(",", msgDict[k])));
              }
            }
          }
        }
        MessageCache = newCache;
      }
    }

    /*
    public bool Message(SpeckleInterface.MessageIntent intent, SpeckleInterface.MessageLevel level, params string[] messagePortions)
    {
      return Message(Convert(intent), Convert(level), messagePortions);
    }

    public bool Message(SpeckleInterface.MessageIntent intent, SpeckleInterface.MessageLevel level, Exception ex, params string[] messagePortions)
    {
      return Message(Convert(intent), Convert(level), ex, messagePortions);
    }

    SpeckleGSAInterfaces.MessageIntent Convert(SpeckleInterface.MessageIntent mi)
      => (new Dictionary<SpeckleInterface.MessageIntent, SpeckleGSAInterfaces.MessageIntent>()
      {
        { SpeckleInterface.MessageIntent.Display, SpeckleGSAInterfaces.MessageIntent.Display },
        { SpeckleInterface.MessageIntent.TechnicalLog, SpeckleGSAInterfaces.MessageIntent.TechnicalLog },
        { SpeckleInterface.MessageIntent.Telemetry, SpeckleGSAInterfaces.MessageIntent.Telemetry }
      })[mi];

    SpeckleGSAInterfaces.MessageLevel Convert(SpeckleInterface.MessageLevel ml)
      => (new Dictionary<SpeckleInterface.MessageLevel, SpeckleGSAInterfaces.MessageLevel>()
      {
        { SpeckleInterface.MessageLevel.Debug, SpeckleGSAInterfaces.MessageLevel.Debug },
        { SpeckleInterface.MessageLevel.Information, SpeckleGSAInterfaces.MessageLevel.Information },
        { SpeckleInterface.MessageLevel.Error, SpeckleGSAInterfaces.MessageLevel.Error },
        { SpeckleInterface.MessageLevel.Fatal, SpeckleGSAInterfaces.MessageLevel.Fatal },
      })[ml];
    */
  }
}
