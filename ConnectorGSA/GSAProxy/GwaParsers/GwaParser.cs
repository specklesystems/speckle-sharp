using Speckle.GSA.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Speckle.GSA.API.GwaSchema;

namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  public abstract class GwaParser<T> : IGwaParser where T: GsaRecord
  {
    public virtual Type GsaSchemaType { get => typeof(T); }
    public GsaRecord Record { get => record; }

    protected static readonly string SID_APPID_TAG = "speckle_app_id";
    protected static readonly string SID_STRID_TAG = "speckle_stream_id";

    public abstract bool FromGwa(string gwa);

    public abstract bool Gwa(out List<string> gwa, bool includeSet = false);

    protected T record;
    protected string keyword;
    protected GwaSetCommandType gwaSetCommandType;

    public GwaParser(T record)
    {
      this.record = record;

      keyword = this.GetType().GetAttribute<GsaType>("Keyword").ToString();
      Enum.TryParse(this.GetType().GetAttribute<GsaType>("SetCommandType").ToString(), out gwaSetCommandType);
    }

    #region basic_common_fns

    //The keywordOverride is mainly used for the LOAD_BEAM case
    protected bool InitialiseGwa(bool includeSet, out List<string> items, string keywordOverride = "")
    {
      items = new List<string>();
      if (includeSet)
      {
        if (gwaSetCommandType == GwaSetCommandType.SetAt)
        {
          items.AddRange(new[] { "SET_AT", record.Index.ToString() });
        }
        else
        {
          items.Add("SET");
        }
      }
      var sid = FormatSidTags(record.StreamId, record.ApplicationId);
      items.Add((string.IsNullOrEmpty(keywordOverride) ? keyword : keywordOverride) + "." + record.Version + ((string.IsNullOrEmpty(sid)) ? "" : ":" + sid));
      if (gwaSetCommandType == GwaSetCommandType.Set)
      {
        items.Add(record.Index.ToString());
      }
      return true;
    }

    //Designed to be called after ProcessGwa - and can handle the SET or SET_AT being included
    protected bool BasicFromGwa(string gwa, out List<string> remainingItems, string keywordOverride = "")
    {
      var items = Split(gwa);
      remainingItems = new List<string>();
      if (items.Count() == 0)
      {
        return false;
      }

      //Process and remove just the initial SET or SET_AT <index> items      
      if (items[0].StartsWith("set", StringComparison.InvariantCultureIgnoreCase))
      {
        if (items[0].StartsWith("set_at", StringComparison.InvariantCultureIgnoreCase))
        {
          gwaSetCommandType = GwaSetCommandType.SetAt;

          if (int.TryParse(items[1], out var foundIndex))
          {
            record.Index = foundIndex;
          }

          //For SET_ATs the format is SET_AT <index> <keyword> .., so remove the first two
          items.Remove(items[1]);
          items.Remove(items[0]);
        }
        else
        {
          gwaSetCommandType = GwaSetCommandType.Set;

          items.Remove(items[0]);
        }
      }

      if (!ParseKeywordVersionSid(items[0], keywordOverride))
      {
        return false;
      }

      //Remove keyword
      items.Remove(items[0]);

      if (gwaSetCommandType == GwaSetCommandType.Set)
      {
        if (!int.TryParse(items[0], out var index) || index == 0)
        {
          return false;
        }
        record.Index = index;
        items.Remove(items[0]);
      }

      remainingItems = items;

      return true;
    }

    protected bool AddItems(ref List<string> items, params object[] list)
    {
      try
      {
        foreach (var l in list)
        {
          if (l == null)
          {
            items.Add("");
          }
          else if (l is Func<string>)
          {
            items.Add(((Func<string>)l)());
          }
          else
          {
            items.Add(l.ToString());
          }
        }
        return true;
      }
      catch
      {
        return false;
      }
    }

    protected bool FromGwaByFuncs(List<string> items, out List<string> remainingItems, params Func<string, bool>[] fns)
    {
      if (fns.Count() > items.Count())
      {
        remainingItems = new List<string>();
        return false;
      }
      for (var i = 0; i < fns.Count(); i++)
      {
        if (fns[i] != null && !fns[i](items[i]))
        {
          remainingItems = (i == 0) ? new List<string>() : items.Skip(i).ToList();
          return false;
        }
      }
      remainingItems = items.Skip(fns.Count()).ToList();
      return true;
    }

    #endregion

    #region common_to_gwa_fns

    protected string AddEntities(List<int> memberIndices, List<int> elementIndices)
    {
      //For now assume that an empty list means "all"
      if ((memberIndices == null || memberIndices.Count() == 0) && (elementIndices == null || elementIndices.Count() == 0))
      {
        return "all";
      }

      var entityLists = new List<string>();
      if (memberIndices != null && memberIndices.Count > 0)
      {
        var listStr = AddEntities(memberIndices, GSALayer.Design);
        if (!string.IsNullOrEmpty(listStr))
        {
          entityLists.Add(listStr);
        }
      }
      if (elementIndices != null && elementIndices.Count > 0)
      {
        var listStr = AddEntities(elementIndices, GSALayer.Analysis);
        if (!string.IsNullOrEmpty(listStr))
        {
          entityLists.Add(listStr);
        }
      }
      return string.Join(" ", entityLists);
    }

    private string AddEntities(List<int> entities, GSALayer layer)
    {
      //Unlike other keywords which have entity type as a parameter, this keyword (at least for version 2) still has "element list" which means, 
      //for members, the group is used
      var allIndices = ((layer == GSALayer.Design) 
        ? Instance.GsaModel.Cache.LookupIndices<GsaMemb>()
        : Instance.GsaModel.Cache.LookupIndices<GsaEl>()).Distinct().OrderBy(i => i).ToList();

      if (entities.Distinct().OrderBy(i => i).SequenceEqual(allIndices))
      {
        return "all";
      }
      return (layer == GSALayer.Design)
        ? string.Join(" ", entities.Select(i => "G" + i))
        : string.Join(" ", entities);
    }

    protected string AddNodes(List<int> indices)
    {
      //For now assume that an empty list means "all"
      if (indices == null || indices.Count() == 0)
      {
        return "all";
      }

      //Unlike other keywords which have entity type as a parameter, this keyword (at least for version 2) still has "element list" which means, 
      //for members, the group is used

      var allIndices = Instance.GsaModel.Cache.LookupIndices<GsaNode>().Distinct().OrderBy(i => i).ToList();

      if (indices.Distinct().OrderBy(i => i).SequenceEqual(allIndices))
      {
        return "all";
      }
      return string.Join(" ", indices);
    }

    protected void AddEndReleaseItems(ref List<string> items, Dictionary<AxisDirection6, ReleaseCode> releases, List<double> stiffnesses, List<AxisDirection6> axisDirs)
    {
      var rls = "";
      var stiffnessIndex = 0;
      foreach (var d in axisDirs)
      {
        var releaseCode = (releases != null && releases.Count() > 0 && releases.ContainsKey(d)) ? releases[d] : ReleaseCode.Fixed;
        rls += releaseCode.GetStringValue();
        if (releaseCode == ReleaseCode.Stiff && releases.ContainsKey(d) && (++stiffnessIndex) < stiffnesses.Count())
        {
          stiffnesses.Add(stiffnesses[stiffnessIndex]);
        }
      }
      items.Add(rls);
      if (stiffnesses != null && stiffnesses.Count() > 0)
      {
        items.AddRange(stiffnesses.Select(s => s.ToString()));
      }
      return;
    }

    #endregion

    #region common_from_gwa_fns

    /*
    protected bool AddName(string v)
    {
      record.name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }
    */

    protected bool AddName(string v, out string name)
    {
      name = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    protected bool AddStringValue(string v, out string sv)
    {
      sv = (string.IsNullOrEmpty(v)) ? null : v;
      return true;
    }

    protected bool AddColour(string v, out Colour c)
    {
      if (!Enum.TryParse(v, true, out c))
      {
        c = Colour.NO_RGB;
      }
      return true;
    }

    protected bool ParseKeywordVersionSid(string v, string keywordOverride = "")
    {
      string keywordAndVersion;
      var delimIndex = v.IndexOf(':');
      if (delimIndex > 0)
      {
        //An SID has been found
        keywordAndVersion = v.Substring(0, delimIndex);
        var sidTags = v.Substring(delimIndex);
        var match = Regex.Match(sidTags, "(?<={" + SID_STRID_TAG + ":).*?(?=})");
        record.StreamId = (!string.IsNullOrEmpty(match.Value)) ? match.Value : null;
        match = Regex.Match(sidTags, "(?<={" + SID_APPID_TAG + ":).*?(?=})");
        record.ApplicationId = (!string.IsNullOrEmpty(match.Value)) ? match.Value : null;
      }
      else
      {
        keywordAndVersion = v;
      }

      var kwSplit = keywordAndVersion.Split('.');
      var foundKeyword = kwSplit[0];
      if (!foundKeyword.Equals(string.IsNullOrEmpty(keywordOverride) ? keyword : keywordOverride, StringComparison.InvariantCultureIgnoreCase))
      {
        return false;
      }
      if (kwSplit.Count() > 1)
      {
        if (!int.TryParse(kwSplit[1], out record.Version))
        {
          return false;
        }
      }
      else
      {
        record.Version = 1;
      }
      return true;
    }

    protected bool AddEntities(string v, out List<int> memberIndices, out List<int> elementIndices)
    {
      elementIndices = null;
      return (AddEntities(v, GSALayer.Design, out memberIndices) && AddEntities(v, GSALayer.Analysis, out elementIndices));
    }

    private bool AddEntities(string v, GSALayer layer, out List<int> indices)
    {
      var entityItems = v.Split(' ');
      if (layer == GSALayer.Design)
      {
        if (entityItems.Count() == 1 && entityItems.First().Equals("all", StringComparison.InvariantCultureIgnoreCase))
        {
          indices = Instance.GsaModel.Cache.LookupIndices<GsaMemb>().ToList();
        }
        else
        {
          //Only recognise the groups, as these represent the members
          //TO DO: for all elements, find if they have parents and include them
          var members = string.Join(" ", entityItems.Where(ei => ei.StartsWith("G")).Select(ei => ei.Substring(1)));
          indices = Instance.GsaModel.Proxy.ConvertGSAList(members, GSAEntity.MEMBER).ToList();
        }
      }
      else
      {
        indices = (entityItems.Count() == 1 && entityItems.First().Equals("all", StringComparison.InvariantCultureIgnoreCase))
          ? Instance.GsaModel.Cache.LookupIndices<GsaEl>().ToList()
          : Instance.GsaModel.Proxy.ConvertGSAList(v, GSAEntity.ELEMENT).ToList();
      }
      return true;
    }

    protected bool AddNodes(string v, out List<int> indices)
    {
      var entityItems = v.Split(' ');
      indices = (entityItems.Count() == 1 && entityItems.First().Equals("all", StringComparison.InvariantCultureIgnoreCase))
          ? Instance.GsaModel.Cache.LookupIndices<GsaNode>()
          : Instance.GsaModel.Proxy.ConvertGSAList(v, GSAEntity.NODE);
      return true;
    }

    //Useful helper function for MEMB and EL
    protected bool ProcessReleases(List<string> items, out List<string> remainingItems,
      ref Dictionary<AxisDirection6, ReleaseCode> Releases1, ref List<double> Stiffnesses1, ref Dictionary<AxisDirection6, ReleaseCode> Releases2, ref List<double> Stiffnesses2)
    {
      remainingItems = items; //default in case of early exit of this method
      var axisDirs = Enum.GetValues(typeof(AxisDirection6)).OfType<AxisDirection6>().Where(v => v != AxisDirection6.NotSet).ToList();

      var endReleases = new Dictionary<AxisDirection6, ReleaseCode>[2] { null, null };
      var endStiffnesses = new List<double>[2];

      var itemIndex = 0;
      for (var i = 0; i < 2; i++)
      {
        endReleases[i] = new Dictionary<AxisDirection6, ReleaseCode>();
        endStiffnesses[i] = new List<double>();

        var relCodes = items[itemIndex++];
        if (relCodes.Length < axisDirs.Count())
        {
          return false;
        }

        var numExpectedStiffnesses = 0;
        for (var j = 0; j < axisDirs.Count(); j++)
        {
          var upperCharCode = char.ToUpper(relCodes[j]);
          if (upperCharCode == 'K')
          {
            numExpectedStiffnesses++;
            endReleases[i].Add(axisDirs[j], ReleaseCode.Stiff);
          }
          else if (upperCharCode == 'R')
          {
            endReleases[i].Add(axisDirs[j], ReleaseCode.Released);
          }
          else
          {
            //For now, Fixed values aren't added as it's considered the default
            //endReleases[i].Add(axisDirs[j], ReleaseCode.Fixed);
          }
        }

        if (numExpectedStiffnesses > 0)
        {
          for (var k = 0; k < numExpectedStiffnesses; k++)
          {
            if (!double.TryParse(items[itemIndex++], out double stiffness))
            {
              return false;
            }
            endStiffnesses[i].Add(stiffness);
          }
        }
      }

      Releases1 = endReleases[0].Count() > 0 ? endReleases[0] : null;
      Releases2 = endReleases[1].Count() > 0 ? endReleases[1] : null;
      Stiffnesses1 = endStiffnesses[0].Count() > 0 ? endStiffnesses[0] : null;
      Stiffnesses2 = endStiffnesses[1].Count() > 0 ? endStiffnesses[1] : null;

      remainingItems = items.Skip(itemIndex).ToList();

      return true;
    }

    protected bool AddYesNoBoolean(string v, out bool dest)
    {
      dest = (v.Equals("YES", StringComparison.InvariantCultureIgnoreCase)) ? true : false;
      return true;
    }

    protected bool AddNullableIndex(string v, out int? dest)
    {
      dest = int.TryParse(v, out var n) && n > 0 ? (int?)n : null;
      return true;
    }

    //For when you need to read a value from GWA that is stored in a nullable double member
    protected bool AddNullableDoubleValue(string v, out double? dest)
    {
      dest = double.TryParse(v, out var n) ? (double?)n : null;
      return true;
    }

    //For when you need to read a value from GWA that is stored in a nullable integer member
    protected bool AddNullableIntValue(string v, out int? dest)
    {
      dest = int.TryParse(v, out var n) ? (int?)n : null;
      return true;
    }

    #endregion

    #region other_fns

    protected List<string> Split(string gwa)
    {
      try
      {
        return gwa.ListSplit(Instance.GsaModel.Proxy.GwaDelimiter).ToList();
      }
      catch
      {
        return new List<string>();
      }
    }

    protected bool Join(List<string> items, out string joined)
    {
      joined = string.Join(Instance.GsaModel.Proxy.GwaDelimiter.ToString(), items);
      return (joined.Length > 0);
    }

    protected string IndicesList(List<int> indices)
    {
      //Like entities, assume an empty list means "all"
      return (indices == null || indices.Count() == 0) ? "all" : string.Join(" ", indices);
    }

    protected List<int> StringToIntList(string s, char delim = ' ')
    {
      var retList = new List<int>();
      foreach (var i in s.Split(delim).Where(i => i.IsDigits()))
      {
        if (int.TryParse(i, out var result))
        {
          retList.Add(result);
        }
      }
      return retList;
    }

    protected List<double> StringToDoubleList(string s, char delim = ' ')
    {
      var retList = new List<double>();
      foreach (var i in s.Split(delim).Where(i => Regex.IsMatch(i, @"\d+\.?\d*")))
      {
        if (double.TryParse(i, out var result))
        {
          retList.Add(result);
        }
      }
      return retList;
    }

    protected bool EnumParse<T>(string s, out T v)
    {
      try
      {
        v = (T)Enum.Parse(typeof(T), s, true);
        return true;
      }
      catch
      {
        v = default(T);
        return false;
      }
    }

    #endregion

    #region static_methods

    private static string FormatApplicationIdSidTag(string value)
    {
      return (string.IsNullOrEmpty(value) ? null : "{" + SID_APPID_TAG + ":" + value.Replace(" ", "") + "}");
    }

    private static string FormatStreamIdSidTag(string value)
    {
      return (string.IsNullOrEmpty(value) ? null : "{" + SID_STRID_TAG + ":" + value.Replace(" ", "") + "}");
    }

    protected static string FormatSidTags(string streamId = null, string applicationId = null)
    {
      var streamIdSidTag = FormatStreamIdSidTag(streamId);
      var appIdSidTag = FormatApplicationIdSidTag(applicationId);
      var sidTags = "";
      if (!string.IsNullOrEmpty(streamIdSidTag))
      {
        sidTags += streamIdSidTag;
      }
      if (!string.IsNullOrEmpty(appIdSidTag))
      {
        sidTags += appIdSidTag;
      }
      return string.IsNullOrEmpty(sidTags) ? null : sidTags;
    }
    #endregion
  }
}
