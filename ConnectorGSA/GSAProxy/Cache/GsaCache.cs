using Speckle.ConnectorGSA.Proxy.GwaParsers;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.Cache
{
  public class GsaCache
  {
    public List<List<GwaKeyword>> TxTypeDependencyGenerations { get; private set; } = new List<List<GwaKeyword>>();
    public int NumRecords { get => validRecords.Where(r => r.Latest).Count(); }

    //Lookup tables used when the kit calls methods which pass in GwaSchema types, and when the connector upserts based on keywords
    private IPairCollection<GwaKeyword, Type> SchemaTypeByKeyword = new PairCollection<GwaKeyword, Type>();
    //Used in ordering calls to ToSpeckle()
    // [ Schema type, Parser type ]
    private Dictionary<Type, Type> ParsersBySchemaType = new Dictionary<Type, Type>();
    private Dictionary<GwaKeyword, Type> ParsersByKeyword = new Dictionary<GwaKeyword, Type>();

    private List<GsaCacheRecord> records = new List<GsaCacheRecord>();
    private List<string> foundStreamIds = new List<string>();  // To avoid storing stream ID strings multiple times

    //Performance-enhancing index tables for fast lookup
    private readonly Dictionary<GwaKeyword, HashSet<int>> collectionIndicesByKw = new Dictionary<GwaKeyword, HashSet<int>>();
    private readonly Dictionary<GwaKeyword, Dictionary<int, HashSet<int>>> collectionIndicesByKwGsaId = new Dictionary<GwaKeyword, Dictionary<int, HashSet<int>>>();
    private readonly Dictionary<string, HashSet<int>> collectionIndicesByApplicationId = new Dictionary<string, HashSet<int>>();

    private readonly Dictionary<int, HashSet<int>> collectionIndicesByStreamIdIndex = new Dictionary<int, HashSet<int>>();
    //private readonly Dictionary<string, HashSet<int>> collectionIndicesBySpeckleTypeName = new Dictionary<string, HashSet<int>>();

    // < keyword , { < index, app_id >, < index, app_id >, ... } >
    private readonly Dictionary<GwaKeyword, IPairCollectionComparable<int, string>> provisionals = new Dictionary<GwaKeyword, IPairCollectionComparable<int, string>>();

    //Hardcoded for now to use current 10.1 keywords - to be reviewed
    private static readonly GwaKeyword analKeyword = GwaKeyword.ANAL;
    private static readonly GwaKeyword comboKeyword = GwaKeyword.COMBINATION;

    private List<GsaCacheRecord> validRecords { get => records.Where(r => r != null).ToList(); }

    private object cacheLock = new object();
    private bool initialised = false;
    private bool initialisedError = false;  //To ensure initialisation is only attempted once.

    public GsaCache()  { }

    #region init
    private bool InitialiseIfNecessary()
    {
      if (!initialised && !initialisedError)
      {
        if (!GetAssemblyTypes(out var assemblyTypes)
          || !GetParserTypes(assemblyTypes, out var parserTypes)
          || !GetTypeTrees(parserTypes, out var typeTreeCollection)
          || !PopulateTxTypeGenerations(typeTreeCollection)
          || !PopulateSchemaKeywords(parserTypes)
          || !PopulateTxTypeParsers(parserTypes))
        {
          initialisedError = true;
          return false;
        }
        initialised = true;
      }
      return (initialised && !initialisedError);
    }

    private bool PopulateTxTypeParsers(List<Type> parserTypes)
    {
      ParsersByKeyword = parserTypes.ToDictionary(pt => Helper.GetGwaKeyword(pt), pt => pt);
      ParsersBySchemaType = parserTypes.ToDictionary(pt => pt.BaseType.GenericTypeArguments.First(), pt => pt);
      return true;
    }

    private bool PopulateTxTypeGenerations(TypeTreeCollection<GwaKeyword> col)
    {
      if (col == null)
      {
        return false;
      }
      var gens = col.Generations();
      if (gens == null || gens.Count == 0)
      {
        return false;
      }
      TxTypeDependencyGenerations = gens;
      return true;
    }

    private bool GetAssemblyTypes(out List<Type> types)
    {
      try
      {
        var assembly = GetType().Assembly; //This assembly
        types = assembly.GetTypes().ToList();
        return true;
      }
      catch
      {
        types = null;
        return false;
      }
    }

    private bool GetParserTypes(List<Type> assemblyTypes, out List<Type> types)
    {
      try
      {
        var gsaBaseType = typeof(GwaParser<GsaRecord>);
        var gsaAttributeType = typeof(GwaParsers.GsaType);

        types = assemblyTypes.Where(t => Helper.InheritsOrImplements(t, (typeof(IGwaParser)))
          && t.CustomAttributes.Any(ca => ca.AttributeType == gsaAttributeType)
          && Helper.IsSelfContained(t)
          && !t.IsAbstract
          ).ToList();

        return true;
      }
      catch
      {
        types = null;
        return false;
      }
    }

    //Yes the schema keywords are stored with their *parsers*
    private bool PopulateSchemaKeywords(List<Type> parserTypes)
    {
      var gwaParserInterface = typeof(IGwaParser);
      if (parserTypes.Any(t => !t.InheritsOrImplements(gwaParserInterface)))
      {
        return false;
      }

      var completeDependencyTreeKeywords = new HashSet<GwaKeyword>(TxTypeDependencyGenerations.SelectMany(g => g));

      this.SchemaTypeByKeyword = new PairCollection<GwaKeyword, Type>();
      foreach (var pt in parserTypes)
      {
        var kw = Helper.GetGwaKeyword(pt);
        if (completeDependencyTreeKeywords.Contains(kw))
        {
          this.SchemaTypeByKeyword.Add(Helper.GetGwaKeyword(pt), pt.BaseType.GetGenericArguments().First());
        }
      }
      return (this.SchemaTypeByKeyword != null && this.SchemaTypeByKeyword.Lefts.Count > 0 && this.SchemaTypeByKeyword.Rights.Count > 0);
    }

    private bool GetTypeTrees(List<Type> schemaTypes, out TypeTreeCollection<GwaKeyword> retCol)
    {
      try
      {
        var kwDict = schemaTypes.ToDictionary(st => Helper.GetGwaKeyword(st), st => Helper.GetReferencedKeywords(st));

        retCol = new TypeTreeCollection<GwaKeyword>(kwDict.Keys);
        foreach (var kw in kwDict.Keys)
        {
          retCol.Integrate(kw, kwDict[kw]);
        }
        return true;
      }
      catch
      {
        retCol = null;
        return false;
      }
    }
    #endregion

    #region upsert

    //This Upsert is only called when hydrating the cache from the GSA instance
    public bool Upsert(ProxyGwaLine proxyGwaLine)
    {
      if (!InitialiseIfNecessary() || !this.SchemaTypeByKeyword.ContainsLeft(proxyGwaLine.Keyword) || !ParsersByKeyword.ContainsKey(proxyGwaLine.Keyword))
      {
        return false;
      }

      var parser = (IGwaParser)Activator.CreateInstance(ParsersByKeyword[proxyGwaLine.Keyword]);

      if (parser.FromGwa(proxyGwaLine.GwaWithoutSet) && parser.Record != null)
      {
        //For the SET_AT keyworsd
        if (!parser.Record.Index.HasValue && proxyGwaLine.Index > 0)
        {
          parser.Record.Index = proxyGwaLine.Index;
        }
        return Add(proxyGwaLine.Keyword, parser.Record, true);
      }

      return false;
    }
    

    //Called by the kit
    //Not every record has stream IDs (like generated nodes)
    public bool Upsert<T>(GsaRecord record, bool? latest = true)
    {
      if (!InitialiseIfNecessary() || !this.SchemaTypeByKeyword.ContainsRight(typeof(T)) || !SchemaTypeByKeyword.FindLeft(typeof(T), out var keyword)
        || !ParsersBySchemaType.ContainsKey(typeof(T)))
      {
        return false;
      }
      try
      {
        var matchingRecords = new List<GsaCacheRecord>();
        lock(cacheLock)
        {
          if (GetAllRecords(keyword, record.Index.Value, out var foundRecords) && foundRecords != null && foundRecords.Count > 0)
          {
            matchingRecords = foundRecords;
          }
        }

        if (matchingRecords.Count() > 0)
        {
          var equalRecords = matchingRecords.Where(r => Equals(r.GsaRecord, record)).ToList();
          if (equalRecords.Count() == 1)
          {
            //There should just be one equal record

            //There is no change to the record but it clearly means it's part of the latest
            if (latest.HasValue)
            {
              lock (cacheLock)
              {
                equalRecords.First().Latest = latest.Value;
              }
            }

            return true;
          }
          else if (equalRecords.Count() == 0)
          {
            lock (cacheLock)
            {
              //These will be return at the next call to GetToBeDeletedGwa() and removed at the next call to Snapshot()
              foreach (var r in matchingRecords)
              {
                r.Latest = false;
              }
            }
          }
          else if (equalRecords.Count() > 1)
          {
            throw new Exception("Unexpected multiple matches found in upsert of cache records");
          }
        }

        //if there is no matching or no equal records, then add it as a new one
        Add(keyword, record, true);

        return true;
      }
      catch (Exception ex)
      {
        return false;
      }
    }

    private bool Add(GwaKeyword keyword, GsaRecord record, bool? latest = true)
    {
      lock (cacheLock)
      {
        records.Add(new GsaCacheRecord(keyword, record));
        if (!UpdateIndexTables(keyword, record.Index.Value, record.StreamId, record.ApplicationId))
        {
          return false;
        }
        if (provisionals.ContainsKey(keyword))
        {
          if (!string.IsNullOrEmpty(record.ApplicationId) && provisionals[keyword].ContainsRight(record.ApplicationId))
          {
            provisionals[keyword].RemoveRight(record.ApplicationId);
          }
          //In most cases where there is an Application ID and the provisional index matches the one of the new record, the call above will have removed
          //its index from the provisionals table.  But in the odd case where a different index is specified than the one that was assigned to the application ID,
          //the existing one at that index needs to be moved
          if (provisionals[keyword].ContainsLeft(record.Index.Value))
          {
            provisionals[keyword].RemoveLeft(record.Index.Value);
            //Only move the reservation if there is an Application ID involved
            if (provisionals[keyword].FindRight(record.Index.Value, out string right) && !string.IsNullOrEmpty(right))
            {
              var newIndex = FindNextFreeIndexForProvisional(keyword);
              UpsertProvisional(keyword, newIndex, right);
            }
          }
          
          if (provisionals[keyword].Count() == 0)
          {
            provisionals.Remove(keyword);
          }
        }
      }
      return true;
    }

    private bool Equals(GsaRecord a, GsaRecord b)
    {
      //TO DO
      return false;
    }

    //Assumptions:
    //- this is called within a lock
    //- the record has already been added
    private bool UpdateIndexTables(GwaKeyword kw, int gsaIndex, string streamId, string applicationId)
    {
      //Minus one because the record has already been added
      var newColIndex = records.Count() - 1;
      var trimmedAppId = string.IsNullOrEmpty(applicationId) ? applicationId : applicationId.Replace(" ", "");

      if (!collectionIndicesByKw.ContainsKey(kw))
      {
        collectionIndicesByKw.Add(kw, new HashSet<int>());
      }
      collectionIndicesByKw[kw].Add(newColIndex);
      if (!collectionIndicesByKwGsaId.ContainsKey(kw))
      {
        collectionIndicesByKwGsaId.Add(kw, new Dictionary<int, HashSet<int>>());
      }
      if (!collectionIndicesByKwGsaId[kw].ContainsKey(gsaIndex))
      {
        collectionIndicesByKwGsaId[kw].Add(gsaIndex, new HashSet<int>());
      }
      collectionIndicesByKwGsaId[kw][gsaIndex].Add(newColIndex);
      if (!string.IsNullOrEmpty(trimmedAppId))
      {
        if (!collectionIndicesByApplicationId.ContainsKey(trimmedAppId))
        {
          collectionIndicesByApplicationId.Add(trimmedAppId, new HashSet<int>());
        }
        collectionIndicesByApplicationId[trimmedAppId].Add(newColIndex);
      }
      if (!string.IsNullOrEmpty(streamId))
      {
        if (!foundStreamIds.Contains(streamId))
        {
          foundStreamIds.Add(streamId);
        }
        var streamIdIndex = foundStreamIds.IndexOf(streamId);
        if (!collectionIndicesByStreamIdIndex.ContainsKey(streamIdIndex))
        {
          collectionIndicesByStreamIdIndex.Add(streamIdIndex, new HashSet<int>());
        }
        collectionIndicesByStreamIdIndex[streamIdIndex].Add(newColIndex);
      }
      /*
      if (so != null && !string.IsNullOrEmpty(so.Type))
      {
        var speckleTypeName = SpeckleTypeName(so);
        if (!collectionIndicesBySpeckleTypeName.ContainsKey(speckleTypeName))
        {
          collectionIndicesBySpeckleTypeName.Add(speckleTypeName, new HashSet<int>());
        }
        collectionIndicesBySpeckleTypeName[speckleTypeName].Add(newColIndex);
      }
      */
      return true;
    }

    private void UpsertProvisional(GwaKeyword keyword, int index, string applicationId = null)
    {
      if (!provisionals.ContainsKey(keyword))
      {
        provisionals.Add(keyword, new PairCollectionComparable<int, string>());
      }
      provisionals[keyword].Add(index, applicationId);
    }
    #endregion

    #region lookup
    public bool GetNative<T>(int index, out GsaRecord gsaRecord)
    {
      if (InitialiseIfNecessary() && this.SchemaTypeByKeyword.FindLeft(typeof(T), out var keyword) 
        && GetAllRecords(keyword, index, out var foundRecords))
      {
        var latestFound = foundRecords.Where(r => r.Latest);
        if (latestFound.Count() > 0)
        {
          gsaRecord = latestFound.First().GsaRecord;
          return true;
        }
      }
      gsaRecord = null;
      return false;
    }

    public int? LookupIndex<T>(string applicationId)
    {
      if (!InitialiseIfNecessary() || !SchemaTypeByKeyword.ContainsRight(typeof(T)) 
        || !SchemaTypeByKeyword.FindLeft(typeof(T), out var keyword))
      {
        return null;
      }
      lock (cacheLock)
      {
        return (ValidAppId(applicationId, out string appId) ? GetRecordIndex(keyword, appId) : null);
      }
    }

    public List<int?> LookupIndices<T>(IEnumerable<string> applicationIds)
    {
      if (!InitialiseIfNecessary() || !SchemaTypeByKeyword.ContainsRight(typeof(T))
        || !SchemaTypeByKeyword.FindLeft(typeof(T), out var keyword))
      {
        return null;
      }
      lock (cacheLock)
      {
        return (ValidAppIds(applicationIds, out List<string> appIds) ? GetRecordIndices(keyword, appIds) : new List<int?>());
      }
    }

    public List<int?> LookupIndices<T>()
    {
      if (!InitialiseIfNecessary() || !SchemaTypeByKeyword.ContainsRight(typeof(T))
        || !SchemaTypeByKeyword.FindLeft(typeof(T), out var keyword))
      {
        return null;
      }
      lock (cacheLock)
      {
        return (GetRecordIndices(keyword).Select(k => (int?)k).ToList());
      }
    }

    public string GetApplicationId<T>(int gsaIndex)
    {
      if (!InitialiseIfNecessary() || !this.SchemaTypeByKeyword.FindLeft(typeof(T), out GwaKeyword kw)
       || !collectionIndicesByKwGsaId.ContainsKey(kw) || collectionIndicesByKwGsaId[kw] == null
       || !collectionIndicesByKwGsaId[kw].ContainsKey(gsaIndex) || collectionIndicesByKwGsaId[kw][gsaIndex] == null
       || collectionIndicesByKwGsaId[kw][gsaIndex].Count == 0)
      {
        return "";
      }
      return collectionIndicesByKwGsaId[kw][gsaIndex].OrderBy(i => i).Select(i => records[i].ApplicationId).FirstOrDefault();
    }

    //To be fed into the proxy- assume the proxy knows about which keywords are SET and which are SET_AT
    //[ keyword, index, GWA line(s) ]
    public List<Tuple<GwaKeyword, int, List<string>>> GetExpiredData()
    {
      if (!initialised)
      {
        return null;
      }
      lock (cacheLock)
      {
        var matchingRecords = validRecords.Where(r => IsAlterable(r.Keyword, r.ApplicationId) && r.Previous == true && r.Latest == false).ToList();
        return CacheRecordsToGwaData(matchingRecords);
      }
    }

    public List<Tuple<GwaKeyword, int, List<string>>> GetDeletableData()
    {
      if (!initialised)
      {
        return null;
      }
      lock (cacheLock)
      {
        var matchingRecords = validRecords.Where(r => IsAlterable(r.Keyword, r.ApplicationId) && r.Latest == true).ToList();
        return CacheRecordsToGwaData(matchingRecords);
      }
    }

    private int? GetRecordIndex(GwaKeyword kw, string applicationId)
    {
      if (!InitialiseIfNecessary() || string.IsNullOrEmpty(applicationId) || !collectionIndicesByApplicationId.ContainsKey(applicationId) 
        || !collectionIndicesByKw.ContainsKey(kw))
      {
        return null;
      }
      var colIndices = collectionIndicesByApplicationId[applicationId].Intersect(collectionIndicesByKw[kw]);
      return (colIndices.Count() == 0) ? null : (int?)colIndices.Select(i => records[i].GsaRecord.Index).OrderBy(i => i).Last();
    }


    private SortedSet<int> GetRecordIndices(GwaKeyword kw)
    {
      if (!collectionIndicesByKw.ContainsKey(kw))
      {
        return new SortedSet<int>();
      }
      //should return GSA indices, and be ordered!

      var gsaIndexHash = GetRecordIndexHashSet(kw);
      var retSet = new SortedSet<int>();
      foreach (var i in gsaIndexHash)
      {
        retSet.Add(i);
      }
      return retSet;
    }

    private List<int?> GetRecordIndices(GwaKeyword kw, IEnumerable<string> applicationIds)
    {
      var appIds = applicationIds.Where(aid => !string.IsNullOrEmpty(aid) && collectionIndicesByApplicationId.ContainsKey(aid)).ToList();
      if (!collectionIndicesByKw.ContainsKey(kw) || appIds.Count() == 0)
      {
        return new List<int?>();
      }
      var colIndicesHash = new HashSet<int>();
      foreach (var colIndex in collectionIndicesByKw[kw])
      {
        //The appIds have already been checked and they are all present as keys in the recordIndicesByApplicationId dictionary
        foreach (var appId in appIds)
        {
          if (collectionIndicesByApplicationId[appId].Contains(colIndex) && !colIndicesHash.Contains(colIndex))
          {
            colIndicesHash.Add(colIndex);
          }
        }
      }
      var indicesToReturn = colIndicesHash.Select(i => records[i].GsaRecord.Index).Distinct().OrderBy(i => i).Select(i => (int?)i).ToList();
      return indicesToReturn;
    }

    private HashSet<int> GetRecordIndexHashSet(GwaKeyword kw)
    {
      if (!collectionIndicesByKw.ContainsKey(kw))
      {
        return new HashSet<int>();
      }
      //should return GSA indices, be ordered!

      var gsaIndexHash = new HashSet<int>();
      foreach (var i in collectionIndicesByKw[kw])
      {
        if (records[i].GsaRecord.Index.HasValue && !gsaIndexHash.Contains(records[i].GsaRecord.Index.Value))
        {
          gsaIndexHash.Add(records[i].GsaRecord.Index.Value);
        }
      }

      return gsaIndexHash;
    }

    //Assumed to be called within a lock
    private List<Tuple<GwaKeyword, int, List<string>>> CacheRecordsToGwaData(List<GsaCacheRecord> records)
    {
      var returnData = new List<Tuple<GwaKeyword, int, List<string>>>();

      //Order by index as for some keywords (like LOAD_2D_FACE.2) the records do actually move indices when one is deleted
      records = records.OrderByDescending(r => r.GsaRecord.Index.Value).ToList();

      for (int i = 0; i < records.Count(); i++)
      {
        if (ParsersByKeyword.ContainsKey(records[i].Keyword))
        {
          var parser = (IGwaParser)Activator.CreateInstance(ParsersByKeyword[records[i].Keyword]);
          if (parser.Gwa(out var gwa) && gwa != null && gwa.Count > 0)
          {
            returnData.Add(new Tuple<GwaKeyword, int, List<string>>
              (records[i].Keyword, records[i].GsaRecord.Index.Value, gwa));
          }
        }
      }
      return returnData;
    }

    private bool GetAllRecords(GwaKeyword kw, int gsaIndex, out List<GsaCacheRecord> foundRecords)
    {
      if (!collectionIndicesByKwGsaId.ContainsKey(kw) || collectionIndicesByKwGsaId[kw] == null
        || !collectionIndicesByKwGsaId[kw].ContainsKey(gsaIndex) || collectionIndicesByKwGsaId[kw][gsaIndex] == null
        || collectionIndicesByKwGsaId[kw][gsaIndex].Count == 0)
      {
        foundRecords = null;
        return false;
      }
      foundRecords = collectionIndicesByKwGsaId[kw][gsaIndex].Select(i => records[i]).ToList();
      return true;
    }

    private List<GsaCacheRecord> GetAllRecords(GwaKeyword kw, string applicationId)
    {
      if (string.IsNullOrEmpty(applicationId) || !collectionIndicesByApplicationId.ContainsKey(applicationId) || !collectionIndicesByKw.ContainsKey(kw))
      {
        return new List<GsaCacheRecord>();
      }
      var colIndices = collectionIndicesByApplicationId[applicationId].Intersect(collectionIndicesByKw[kw]).OrderBy(i => i);
      return colIndices.Select(i => records[i]).ToList();
    }

    private bool FindProvisionalIndex(GwaKeyword keyword, string applicationId, out int? provisionalIndex)
    {
      if (provisionals.ContainsKey(keyword) && provisionals[keyword].ContainsRight(applicationId) 
        && provisionals[keyword].FindLeft(applicationId, out int index))
      {
        provisionalIndex = index;
        return true;
      }
      provisionalIndex = null;
      return false;
    }

    private int? HighestProvisional(GwaKeyword keyword)
    {
      if (!provisionals.ContainsKey(keyword) || provisionals[keyword] == null || provisionals[keyword].Count() == 0)
      {
        return null;
      }

      return provisionals[keyword].MaxLeft();
    }

    private bool ProvisionalContains(GwaKeyword keyword, int index)
    {
      if (!provisionals.ContainsKey(keyword) || provisionals[keyword] == null || provisionals[keyword].Count() == 0)
      {
        return false;
      }
      return provisionals[keyword].ContainsLeft(index);
    }

    
    #endregion

    #region reservation

    private int FindNextFreeIndexForProvisional(GwaKeyword keyword)
    {
      var indices = GetRecordIndexHashSet(keyword);
      var highestProvisional = HighestProvisional(keyword);
      var highestIndex = Math.Max((indices.Count() == 0) ? 0 : indices.Max(), highestProvisional ?? 0);
      for (int i = 1; i <= highestIndex; i++)
      {
        if (!indices.Contains(i) && !ProvisionalContains(keyword, i))
        {
          return i;
        }
      }
      return highestIndex + 1;
    }

    public int ResolveIndex(GwaKeyword keyword, string applicationId = "")
    {
      lock(cacheLock)
      {
        if (ValidAppId(applicationId, out string appId))
        {
          var matchingRecords = GetAllRecords(keyword, appId);

          if (matchingRecords.Count() == 0)
          {
            if (FindProvisionalIndex(keyword, appId, out int? provisionalIndex))
            {
              return provisionalIndex.Value;
            }
            //No matches in either previous or latest
            var newIndex = FindNextFreeIndexForProvisional(keyword);
            UpsertProvisional(keyword, newIndex, appId);
            return newIndex;
          }
          else
          {
            //There should be only at most one previous and one latest for this type and applicationID
            var existingPrevious = matchingRecords.Where(r => r.Previous && !r.Latest);
            var existingLatest = matchingRecords.Where(r => r.Latest);

            return (existingLatest.Count() > 0) 
              ? existingLatest.First().GsaRecord.Index.Value 
              : existingPrevious.First().GsaRecord.Index.Value;
          }
        }
        else
        {
          //Application ID is empty or null
          var indices = GetRecordIndexHashSet(keyword);
          var highestProvisional = HighestProvisional(keyword);
          var highestIndex = Math.Max((indices.Count() == 0) ? 0 : indices.Max(), highestProvisional ?? 0);
          for (int i = 1; i <= highestIndex; i++)
          {
            if (!indices.Contains(i) && !ProvisionalContains(keyword, i))
            {
              UpsertProvisional(keyword, i);
              return i;
            }
          }

          UpsertProvisional(keyword, highestIndex + 1);
          return highestIndex + 1;
        }
      }
    }

    #region validation
    private bool ValidAppId(string appIdIn, out string appIdOut)
    {
      appIdOut = null;
      if (appIdIn == null)
      {
        return false;
      }
      var appIdTrimmed = appIdIn.Replace(" ", "");
      if (!string.IsNullOrEmpty(appIdTrimmed))
      {
        appIdOut = appIdTrimmed;
        return true;
      }
      return false;
    }

    #endregion

    
    private bool ValidAppIds(IEnumerable<string> appIdIns, out List<string> appIdOuts)
    {
      appIdOuts = new List<string>();
      if (appIdIns == null)
      {
        return false;
      }
      appIdOuts = new List<string>();
      foreach (var aid in appIdIns)
      {
        if (ValidAppId(aid, out string appId))
        {
          appIdOuts.Add(aid);
        }
      }
      return appIdOuts.Count() > 0;
    }

    private bool IsAlterable(GwaKeyword keyword, string applicationId)
    {
      if (keyword == GwaKeyword.NODE && (applicationId == "" || (applicationId != null && applicationId.StartsWith("gsa"))))
      {
        return false;
      }
      return true;
    }
    #endregion

    public void Clear()
    {
      records.Clear();
      collectionIndicesByApplicationId.Clear();
      collectionIndicesByKw.Clear();
      collectionIndicesByKwGsaId.Clear();
      //collectionIndicesBySpeckleTypeName.Clear();
      collectionIndicesByStreamIdIndex.Clear();
    }
  }
}
