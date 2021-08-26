using Speckle.ConnectorGSA.Proxy.GwaParsers;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Speckle.ConnectorGSA.Proxy.Cache
{
  public class GsaCache : IGSACache
  {
    #region GsaRecord_cache
    public int NumRecords { get => validRecords.Where(r => r.Latest).Count(); }

    private List<GsaCacheRecord> records = new List<GsaCacheRecord>();
    private List<string> foundStreamIds = new List<string>();  // To avoid storing stream ID strings multiple times

    //Performance-enhancing index tables for fast lookup
    private readonly Dictionary<Type, HashSet<int>> collectionIndicesBySchemaType = new Dictionary<Type, HashSet<int>>();
    private readonly Dictionary<Type, Dictionary<int, HashSet<int>>> collectionIndicesBySchemaTypeGsaId = new Dictionary<Type, Dictionary<int, HashSet<int>>>();
    private readonly Dictionary<string, HashSet<int>> collectionIndicesByApplicationId = new Dictionary<string, HashSet<int>>();

    private readonly Dictionary<int, HashSet<int>> collectionIndicesByStreamIdIndex = new Dictionary<int, HashSet<int>>();

    // < keyword , { < index, app_id >, < index, app_id >, ... } >
    private readonly Dictionary<Type, IPairCollectionComparable<int, string>> provisionals = new Dictionary<Type, IPairCollectionComparable<int, string>>();
    #endregion

    #region SpeckleObject_cache
    private List<object> objects = new List<object>();
    private readonly Dictionary<Type, HashSet<int>> objectIndicesByType = new Dictionary<Type, HashSet<int>>();
    private readonly Dictionary<Type, Dictionary<int, HashSet<int>>> objectIndicesBySchemaTypesGsaId = new Dictionary<Type, Dictionary<int, HashSet<int>>>();
    #endregion 

    //Hardcoded for now to use current 10.1 keywords - to be reviewed
    private static readonly GwaKeyword analKeyword = GwaKeyword.ANAL;
    private static readonly GwaKeyword comboKeyword = GwaKeyword.COMBINATION;

    private List<GsaCacheRecord> validRecords { get => records.Where(r => r != null).ToList(); }

    private object cacheLock = new object();

    public GsaCache()
    {
    }

    #region upsert

    //These first two Upsert methods are only called when hydrating the cache from the GSA instance
    public bool Upsert(GsaRecord gsaRecord)
    {
      var t = gsaRecord.GetType();
      return Add(t, gsaRecord);
    }

    public bool Upsert(IEnumerable<GsaRecord> gsaRecords)
    {
      int failed = 0;
      foreach (var r in gsaRecords)
      {
        if (!Upsert(r))
        {
          failed++;
        }
      }
      return (failed == 0);
    }

    //Called by the kit
    //Not every record has stream IDs (like generated nodes)
    public bool Upsert(GsaRecord record, bool? latest = true)
    {
      var t = record.GetType();
      try
      {
        var matchingRecords = new List<GsaCacheRecord>();
        lock (cacheLock)
        {
          if (GetAllRecords(t, record.Index.Value, out var foundRecords) && foundRecords != null && foundRecords.Count > 0)
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
        Add(t, record, true);

        return true;
      }
      catch (Exception ex)
      {
        return false;
      }
    }

    public bool SetSpeckleObjects(GsaRecord gsaRecord, IEnumerable<object> speckleObjects)
    {
      var t = gsaRecord.GetType();
      if (!gsaRecord.Index.HasValue)
      {
        return false; 
      }
      var gsaIndex = gsaRecord.Index.Value;
      if (!ValidSchemaTypeGsaIndex(t, gsaIndex))
      {
        return false;
      }

      var colIndices = collectionIndicesBySchemaTypeGsaId[t][gsaIndex];

      //objects = new List<object>();
      //objectIndicesByType = new Dictionary<Type, HashSet<int>>();
      //objectIndicesBySchemaTypesGsaId

      var objectsByType = speckleObjects.GroupBy(so => so.GetType()).ToDictionary(g => g.Key, g => g.ToList());

      lock (cacheLock)
      {
        var newIndexStart = objects.Count();
        foreach (var speckleType in objectsByType.Keys)
        {
          if (!objectIndicesBySchemaTypesGsaId.ContainsKey(t))
          {
            objectIndicesBySchemaTypesGsaId.Add(t, new Dictionary<int, HashSet<int>>());
          }
          if (!objectIndicesBySchemaTypesGsaId[t].ContainsKey(gsaIndex))
          {
            objectIndicesBySchemaTypesGsaId[t].Add(gsaIndex, new HashSet<int>());
          }
          if (!objectIndicesByType.ContainsKey(speckleType))
          {
            objectIndicesByType.Add(speckleType, new HashSet<int>());
          }

          var numSpeckleObjects = objectsByType[speckleType].Count();

          objects.AddRange(objectsByType[speckleType]);
          var newSpeckleObjectIndices = Enumerable.Range(newIndexStart, numSpeckleObjects);

          foreach (var nsoi in newSpeckleObjectIndices)
          {
            if (!objectIndicesByType[speckleType].Contains(nsoi))
            {
              objectIndicesByType[speckleType].Add(nsoi);
            }
            if (!objectIndicesBySchemaTypesGsaId[t][gsaIndex].Contains(nsoi))
            {
              objectIndicesBySchemaTypesGsaId[t][gsaIndex].Add(nsoi);
            }
          }

          newIndexStart += numSpeckleObjects;
        }
      }
      return true;
    }

    private bool Add(Type t, GsaRecord record, bool? latest = true)
    {
      lock (cacheLock)
      {
        records.Add(new GsaCacheRecord(record));
        if (!UpdateIndexTables(t, record.Index.Value, record.StreamId, record.ApplicationId))
        {
          return false;
        }
        if (provisionals.ContainsKey(t))
        {
          if (!string.IsNullOrEmpty(record.ApplicationId) && provisionals[t].ContainsRight(record.ApplicationId))
          {
            provisionals[t].RemoveRight(record.ApplicationId);
          }
          //In most cases where there is an Application ID and the provisional index matches the one of the new record, the call above will have removed
          //its index from the provisionals table.  But in the odd case where a different index is specified than the one that was assigned to the application ID,
          //the existing one at that index needs to be moved
          if (provisionals[t].ContainsLeft(record.Index.Value))
          {
            provisionals[t].RemoveLeft(record.Index.Value);
            //Only move the reservation if there is an Application ID involved
            if (provisionals[t].FindRight(record.Index.Value, out string right) && !string.IsNullOrEmpty(right))
            {
              var newIndex = FindNextFreeIndexForProvisional(t);
              UpsertProvisional(t, newIndex, right);
            }
          }
          
          if (provisionals[t].Count() == 0)
          {
            provisionals.Remove(t);
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
    private bool UpdateIndexTables(Type t, int gsaIndex, string streamId, string applicationId)
    {
      //Minus one because the record has already been added
      var newColIndex = records.Count() - 1;
      var trimmedAppId = string.IsNullOrEmpty(applicationId) ? applicationId : applicationId.Replace(" ", "");

      if (!collectionIndicesBySchemaType.ContainsKey(t))
      {
        collectionIndicesBySchemaType.Add(t, new HashSet<int>());
      }
      collectionIndicesBySchemaType[t].Add(newColIndex);
      if (!collectionIndicesBySchemaTypeGsaId.ContainsKey(t))
      {
        collectionIndicesBySchemaTypeGsaId.Add(t, new Dictionary<int, HashSet<int>>());
      }
      if (!collectionIndicesBySchemaTypeGsaId[t].ContainsKey(gsaIndex))
      {
        collectionIndicesBySchemaTypeGsaId[t].Add(gsaIndex, new HashSet<int>());
      }
      collectionIndicesBySchemaTypeGsaId[t][gsaIndex].Add(newColIndex);
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
      return true;
    }

    private void UpsertProvisional(Type t, int index, string applicationId = null)
    {
      if (!provisionals.ContainsKey(t))
      {
        provisionals.Add(t, new PairCollectionComparable<int, string>());
      }
      provisionals[t].Add(index, applicationId);
    }
    #endregion

    #region lookup
    public bool GetNative<T>(int index, out GsaRecord gsaRecord) => GetNative(typeof(T), index, out gsaRecord);

    public bool GetNative(Type t, int index, out GsaRecord gsaRecord)
    {
      if (GetAllRecords(t, index, out var foundRecords))
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

    public GsaRecord GetNative<T>(int index) => GetNative(typeof(T), index, out var gsaRecord) ? gsaRecord : null;

    public bool GetNative<T>(out List<GsaRecord> gsaRecords) => GetNative(typeof(T), out gsaRecords);

    public bool GetNative(Type t, out List<GsaRecord> gsaRecords)
    {
      if (GetAllRecords(t, out var foundRecords))
      {
        var latestFound = foundRecords.Where(r => r.Latest);
        if (latestFound.Count() > 0)
        {
          gsaRecords = latestFound.Select(f => f.GsaRecord).ToList();
          return true;
        }
      }
      gsaRecords = null;
      return false;
    }

    public bool GetSpeckleObjects<T>(int gsaIndex, out List<object> foundObjects)
    {
      return GetSpeckleObjects<T, object>(gsaIndex, out foundObjects);
    }

    public bool GetSpeckleObjects<T, U>(int gsaIndex, out List<U> foundObjects)
    {
      var t = typeof(T);
      if (!ValidSchemaTypeGsaIndex(t, gsaIndex) || !ValidSchemaTypeGsaIndexForSpeckleObjects(t, gsaIndex))
      {
        foundObjects = null;
        return false;
      }
      var indices = objectIndicesBySchemaTypesGsaId[t][gsaIndex].ToList();
      foundObjects = indices.Select(i => objects[i]).Cast<U>().ToList();
      return true;
    }

    public bool GetSpeckleObjects(out List<object> foundObjects)
    {
      if (!GetAllLatestRecords(out var allLatestRecords))
      {
        foundObjects = null;
        return false;
      }

      var allLatestObjects = new List<object>();
      foreach (var r in allLatestRecords.Where(r => r.GsaRecord != null && r.GsaRecord.Index.HasValue))
      {
        var t = r.Type;
        var gsaIndex = r.GsaRecord.Index.Value;
        if (ValidSchemaTypeGsaIndexForSpeckleObjects(t, gsaIndex))
        {
          var indices = objectIndicesBySchemaTypesGsaId[t][gsaIndex];
          if (indices != null && indices.Count > 0)
          {
            allLatestObjects.AddRange(indices.Select(i => objects[i]));
          }
        }
      }
      if (allLatestObjects.Count == 0)
      {
        foundObjects = null;
        return false;
      }
      foundObjects = allLatestObjects;
      return true;
    }

    public int? LookupIndex<T>(string applicationId)
    {
      var t = typeof(T);
      return (ValidAppId(applicationId, out string appId) ? GetRecordIndex(t, appId) : null);
    }

    public List<int?> LookupIndices<T>(IEnumerable<string> applicationIds)
    {
      var t = typeof(T);
      return (ValidAppIds(applicationIds, out List<string> appIds) ? GetRecordIndices(t, appIds) : new List<int?>());
    }

    public List<int> LookupIndices<T>()
    {
      var t = typeof(T);
      return (GetRecordIndices(t).Select(k => k).ToList());
    }


    public string GetApplicationId<T>(int gsaIndex)
    {
      var t = typeof(T);
      if (!collectionIndicesBySchemaTypeGsaId.ContainsKey(t) || collectionIndicesBySchemaTypeGsaId[t] == null
       || !collectionIndicesBySchemaTypeGsaId[t].ContainsKey(gsaIndex) || collectionIndicesBySchemaTypeGsaId[t][gsaIndex] == null
       || collectionIndicesBySchemaTypeGsaId[t][gsaIndex].Count == 0)
      {
        return "";
      }
      return collectionIndicesBySchemaTypeGsaId[t][gsaIndex].OrderBy(i => i).Select(i => records[i].ApplicationId).FirstOrDefault();
    }

    //To be fed into the proxy- assume the proxy knows about which keywords are SET and which are SET_AT
    //[ keyword, index, GWA line(s) ]
    public List<GsaRecord> GetExpiredRecords()
    {
      var matchingRecords = validRecords.Where(r => r.IsAlterable && r.Previous == true && r.Latest == false).ToList();
      return matchingRecords.Select(r => r.GsaRecord).ToList();
    }

    public List<GsaRecord> GetDeletableRecords()
    {
      var matchingRecords = validRecords.Where(r => r.IsAlterable && r.Latest == true).ToList();
      return matchingRecords.Select(r => r.GsaRecord).ToList();
    }

    private int? GetRecordIndex(Type t, string applicationId)
    {
      if (string.IsNullOrEmpty(applicationId) || !collectionIndicesByApplicationId.ContainsKey(applicationId) 
        || !collectionIndicesBySchemaType.ContainsKey(t))
      {
        return null;
      }
      var colIndices = collectionIndicesByApplicationId[applicationId].Intersect(collectionIndicesBySchemaType[t]);
      return (colIndices.Count() == 0) ? null : (int?)colIndices.Select(i => records[i].GsaRecord.Index).OrderBy(i => i).Last();
    }


    private SortedSet<int> GetRecordIndices(Type t)
    {
      if (!collectionIndicesBySchemaType.ContainsKey(t))
      {
        return new SortedSet<int>();
      }
      //should return GSA indices, and be ordered!

      var gsaIndexHash = GetRecordIndexHashSet(t);
      var retSet = new SortedSet<int>();
      foreach (var i in gsaIndexHash)
      {
        retSet.Add(i);
      }
      return retSet;
    }

    private List<int?> GetRecordIndices(Type t, IEnumerable<string> applicationIds)
    {
      var appIds = applicationIds.Where(aid => !string.IsNullOrEmpty(aid) && collectionIndicesByApplicationId.ContainsKey(aid)).ToList();
      if (!collectionIndicesBySchemaType.ContainsKey(t) || appIds.Count() == 0)
      {
        return new List<int?>();
      }
      var colIndicesHash = new HashSet<int>();
      foreach (var colIndex in collectionIndicesBySchemaType[t])
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

    private HashSet<int> GetRecordIndexHashSet(Type t)
    {
      if (!collectionIndicesBySchemaType.ContainsKey(t))
      {
        return new HashSet<int>();
      }
      //should return GSA indices, be ordered!

      var gsaIndexHash = new HashSet<int>();
      foreach (var i in collectionIndicesBySchemaType[t])
      {
        if (records[i].GsaRecord.Index.HasValue && !gsaIndexHash.Contains(records[i].GsaRecord.Index.Value))
        {
          gsaIndexHash.Add(records[i].GsaRecord.Index.Value);
        }
      }

      return gsaIndexHash;
    }
    private bool GetAllLatestRecords(out List<GsaCacheRecord> foundRecords)
    {
      var allLatestRecords = new List<GsaCacheRecord>();
      foreach (var t in collectionIndicesBySchemaTypeGsaId.Keys)
      {
        foreach (var gsaIndex in collectionIndicesBySchemaTypeGsaId[t].Keys)
        {
          var latestIndices = collectionIndicesBySchemaTypeGsaId[t][gsaIndex].Where(i => records[i].Latest);
          if (latestIndices != null && latestIndices.Count() > 0)
          {
            allLatestRecords.AddRange(latestIndices.Select(i => records[i]));
          }
        }
      }
      if (allLatestRecords != null && allLatestRecords.Count() > 0)
      {
        foundRecords = allLatestRecords;
        return true;
      }
      foundRecords = null;
      return false;
    }

    private bool GetAllRecords(Type t, int gsaIndex, out List<GsaCacheRecord> foundRecords)
    {
      if (!ValidSchemaTypeGsaIndex(t, gsaIndex))
      {
        foundRecords = null;
        return false;
      }
      foundRecords = collectionIndicesBySchemaTypeGsaId[t][gsaIndex].Select(i => records[i]).ToList();
      return true;
    }

    private bool GetAllRecords(Type t, string applicationId, out List<GsaCacheRecord> foundRecords)
    {
      if (string.IsNullOrEmpty(applicationId) || !collectionIndicesByApplicationId.ContainsKey(applicationId) 
        || !collectionIndicesBySchemaType.ContainsKey(t))
      {
        foundRecords = null;
        return false;
      }
      var colIndices = collectionIndicesByApplicationId[applicationId].Intersect(collectionIndicesBySchemaType[t]).OrderBy(i => i);
      foundRecords = colIndices.Select(i => records[i]).ToList();
      return true;
    }

    private bool GetAllRecords(Type t, out List<GsaCacheRecord> foundRecords)
    {
      if (!collectionIndicesBySchemaType.ContainsKey(t))
      {
        foundRecords = null;
        return false;
      }
      var colIndices = collectionIndicesBySchemaType[t].OrderBy(i => i);
      foundRecords = colIndices.Select(i => records[i]).ToList();
      return true;
    }

    private bool FindProvisionalIndex(Type t, string applicationId, out int? provisionalIndex)
    {
      if (provisionals.ContainsKey(t) && provisionals[t].ContainsRight(applicationId) 
        && provisionals[t].FindLeft(applicationId, out int index))
      {
        provisionalIndex = index;
        return true;
      }
      provisionalIndex = null;
      return false;
    }

    private int? HighestProvisional(Type t)
    {
      if (!provisionals.ContainsKey(t) || provisionals[t] == null || provisionals[t].Count() == 0)
      {
        return null;
      }

      return provisionals[t].MaxLeft();
    }

    private bool ProvisionalContains(Type t, int index)
    {
      if (!provisionals.ContainsKey(t) || provisionals[t] == null || provisionals[t].Count() == 0)
      {
        return false;
      }
      return provisionals[t].ContainsLeft(index);
    }

    private bool ValidSchemaTypeGsaIndex(Type t, int gsaIndex)
    {
      var validForGsaRecordCacheLookup = (collectionIndicesBySchemaTypeGsaId.ContainsKey(t) && collectionIndicesBySchemaTypeGsaId[t] != null
        && collectionIndicesBySchemaTypeGsaId[t].ContainsKey(gsaIndex) && collectionIndicesBySchemaTypeGsaId[t][gsaIndex] != null
        && collectionIndicesBySchemaTypeGsaId[t][gsaIndex].Count > 0);

      return validForGsaRecordCacheLookup;
    }

    private bool ValidSchemaTypeGsaIndexForSpeckleObjects(Type t, int gsaIndex)
    {
      return (objectIndicesBySchemaTypesGsaId.ContainsKey(t) && objectIndicesBySchemaTypesGsaId[t] != null
          && objectIndicesBySchemaTypesGsaId[t].ContainsKey(gsaIndex) && objectIndicesBySchemaTypesGsaId[t][gsaIndex] != null);
    }
    #endregion

    #region reservation

    private int FindNextFreeIndexForProvisional(Type t)
    {
      var indices = GetRecordIndexHashSet(t);
      var highestProvisional = HighestProvisional(t);
      var highestIndex = Math.Max((indices.Count() == 0) ? 0 : indices.Max(), highestProvisional ?? 0);
      for (int i = 1; i <= highestIndex; i++)
      {
        if (!indices.Contains(i) && !ProvisionalContains(t, i))
        {
          return i;
        }
      }
      return highestIndex + 1;
    }

    public int ResolveIndex<T>(string applicationId = "")
    {
      var t = typeof(T);
      lock(cacheLock)
      {
        if (ValidAppId(applicationId, out string appId) && GetAllRecords(t, appId, out var matchingRecords))
        {
          if (matchingRecords.Count() == 0)
          {
            if (FindProvisionalIndex(t, appId, out int? provisionalIndex))
            {
              return provisionalIndex.Value;
            }
            //No matches in either previous or latest
            var newIndex = FindNextFreeIndexForProvisional(t);
            UpsertProvisional(t, newIndex, appId);
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
          //Application ID is empty, null or not found in the cache
          var indices = GetRecordIndexHashSet(t);
          var highestProvisional = HighestProvisional(t);
          var highestIndex = Math.Max((indices.Count() == 0) ? 0 : indices.Max(), highestProvisional ?? 0);
          for (int i = 1; i <= highestIndex; i++)
          {
            if (!indices.Contains(i) && !ProvisionalContains(t, i))
            {
              UpsertProvisional(t, i);
              return i;
            }
          }

          UpsertProvisional(t, highestIndex + 1);
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
    #endregion

    public void Clear()
    {
      records.Clear();
      collectionIndicesByApplicationId.Clear();
      collectionIndicesBySchemaType.Clear();
      collectionIndicesBySchemaTypeGsaId.Clear();
      collectionIndicesByStreamIdIndex.Clear();
      objects.Clear();
      objectIndicesByType.Clear();
      objectIndicesBySchemaTypesGsaId.Clear();
    }
  }
}
