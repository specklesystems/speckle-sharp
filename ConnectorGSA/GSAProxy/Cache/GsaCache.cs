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

    //Used in ordering calls to ToSpeckle()
    private HashSet<GwaKeyword> SchemaKeywords;  //Just the keywords which are in the supported full dependency trees
    private Dictionary<GwaKeyword, Type> TxTypeParsers = new Dictionary<GwaKeyword, Type>();
    private bool initialised = false;
    private bool initialisedError = false;  //To ensure initialisation is only attempted once.

    private List<GsaCacheRecord> records = new List<GsaCacheRecord>();
    private List<string> foundStreamIds = new List<string>();  // To avoid storing stream ID strings multiple times

    //Performance-enhancing index tables for fast lookup
    private readonly Dictionary<GwaKeyword, HashSet<int>> collectionIndicesByKw = new Dictionary<GwaKeyword, HashSet<int>>();
    private readonly Dictionary<GwaKeyword, Dictionary<int, HashSet<int>>> collectionIndicesByKwGsaId = new Dictionary<GwaKeyword, Dictionary<int, HashSet<int>>>();
    private readonly Dictionary<string, HashSet<int>> collectionIndicesByApplicationId = new Dictionary<string, HashSet<int>>();
    private readonly Dictionary<int, HashSet<int>> collectionIndicesByStreamIdIndex = new Dictionary<int, HashSet<int>>();
    private readonly Dictionary<string, HashSet<int>> collectionIndicesBySpeckleTypeName = new Dictionary<string, HashSet<int>>();

    private List<GsaCacheRecord> validRecords { get => records.Where(r => r != null).ToList(); }

    public GsaCache()  { }

    public bool Upsert(ProxyGwaLine proxyGwaLine)
    {
      if (!InitialiseIfNecessary() || !SchemaKeywords.Contains(proxyGwaLine.Keyword) || !TxTypeParsers.ContainsKey(proxyGwaLine.Keyword))
      {
        return false;
      }

      var parser = (IGwaParser)Activator.CreateInstance(TxTypeParsers[proxyGwaLine.Keyword]);

      if (parser.FromGwa(proxyGwaLine.GwaWithoutSet) && parser.Record != null)
      {
        var gsaCacheRecord = new GsaCacheRecord(proxyGwaLine.Keyword, parser.Record);
        records.Add(gsaCacheRecord);
      }

      if (!UpdateIndexTables(proxyGwaLine))
      {
        return false;
      }
      

      return true;
    }

    private bool UpdateIndexTables(ProxyGwaLine proxyGwaLine)
    {
      var kw = proxyGwaLine.Keyword;
      var gsaIndex = proxyGwaLine.Index;
      var newColIndex = records.Count();
      var applicationId = proxyGwaLine.ApplicationId;
      var streamId = proxyGwaLine.StreamId;
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

    #region init
    private bool InitialiseIfNecessary()
    {
      if (!initialised && !initialisedError)
      {
        if (!GetAssemblyTypes(out var assemblyTypes)
          || !GetSchemaTypes(assemblyTypes, out var schemaTypes)
          || !GetTypeTrees(schemaTypes, out var typeTreeCollection) 
          || !PopulateTxTypeGenerations(typeTreeCollection)
          || !GetParserTypes(assemblyTypes, out var parserTypes) 
          || !PopulateSchemaKeywords(schemaTypes) 
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
      //TxTypeParsers = parserTypes.ToDictionary(pt => Helper.GetGwaKeyword(pt), pt => pt.BaseType.GenericTypeArguments.First());
      TxTypeParsers = parserTypes.ToDictionary(pt => Helper.GetGwaKeyword(pt), pt => pt);
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
      SchemaKeywords = new HashSet<GwaKeyword>(gens.SelectMany(g => g));
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
        var gsaBaseType = typeof(GwaParser<GsaRecord_>);
        var gsaAttributeType = typeof(GwaParsers.GsaType);

        types = assemblyTypes.Where(t => Helper.InheritsOrImplements(t, (typeof(IGwaParser))) 
          && t.CustomAttributes.Any(ca => ca.AttributeType == gsaAttributeType)
          && SchemaKeywords.Contains(Helper.GetGwaKeyword(t))).ToList();

        return true;
      }
      catch
      {
        types = null;
        return false;
      }
    }

    private bool GetSchemaTypes(List<Type> assemblyTypes, out List<Type> schemaTypes)
    {
      var gsaAttributeType = typeof(GwaParsers.GsaType);
      try
      {
        schemaTypes = assemblyTypes.Where(t => Helper.InheritsOrImplements(t, (typeof(IGwaParser)))
            && t.CustomAttributes.Any(ca => ca.AttributeType == gsaAttributeType)).ToList();
      } 
      catch
      {
        schemaTypes = null;
        return false;
      }
      return (schemaTypes != null && schemaTypes.Count() > 0);
    }

    private bool PopulateSchemaKeywords(List<Type> schemaTypes)
    {
      this.SchemaKeywords = new HashSet<GwaKeyword>(schemaTypes.Select(st => Helper.GetGwaKeyword(st)));
      return (this.SchemaKeywords != null & this.SchemaKeywords.Count > 0);
    }

    public bool GetTypeTrees(List<Type> schemaTypes, out TypeTreeCollection<GwaKeyword> retCol)
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

    public void Clear()
    {
      records.Clear();
      collectionIndicesByApplicationId.Clear();
      collectionIndicesByKw.Clear();
      collectionIndicesByKwGsaId.Clear();
      collectionIndicesBySpeckleTypeName.Clear();
      collectionIndicesByStreamIdIndex.Clear();
    }
  }
}
