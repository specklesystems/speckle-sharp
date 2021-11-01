using Interop.Gsa_10_1;
using Speckle.ConnectorGSA.Proxy.Results;
using Speckle.ConnectorGSA.Results;
using Speckle.GSA.API;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Speckle.GSA.API.GwaSchema;
using Speckle.ConnectorGSA.Proxy.GwaParsers;
using Speckle.GSA.API.CsvSchema;

namespace Speckle.ConnectorGSA.Proxy
{
  public class GsaProxy : IGSAProxy
  {
    private Dictionary<GSALayer, List<List<Type>>> nativeTypeDependencyGenerations; //leaves first, all the way up to roots

    //This is to store data for a requirement specific to sending, and specific to nodes: being filtered out if not referenced by other objects
    private Dictionary<GSALayer, List<Type>> nodeDependentSchemaTypesByLayer = new Dictionary<GSALayer, List<Type>>();

    private Dictionary<GSALayer, List<KwTypeData>> typeInfo = new Dictionary<GSALayer, List<KwTypeData>>();
    private Dictionary<GSALayer, Dictionary<GwaKeyword, int>> typeInfoIndicesByKeyword = new Dictionary<GSALayer, Dictionary<GwaKeyword, int>>();
    private Dictionary<GSALayer, Dictionary<Type, int>> typeInfoIndicesBySchemaType = new Dictionary<GSALayer, Dictionary<Type, int>>();
    private bool initialised = false;
    private bool initialisedError = false;  //To ensure initialisation is only attempted once.

    private Dictionary<GSALayer, List<KwTypeData>> kwTypeData = new Dictionary<GSALayer, List<KwTypeData>>();

    #region static_data
    private static readonly string SID_APPID_TAG = "speckle_app_id";
    private static readonly string SID_STRID_TAG = "speckle_stream_id";
    private static readonly char _GwaDelimiter = '\t';

    public static Dictionary<ResultType, string> ResultTypeStrings = new Dictionary<ResultType, string>
    {
      { ResultType.NodalDisplacements, "Nodal Displacements" },
      { ResultType.NodalVelocity, "Nodal Velocity" },
      { ResultType.NodalAcceleration, "Nodal Acceleration" },
      { ResultType.NodalReaction, "Nodal Reaction" },
      { ResultType.ConstraintForces, "Constraint Forces" },
      { ResultType.Element1dDisplacement, "1D Element Displacement" },
      { ResultType.Element1dForce, "1D Element Force" },
      { ResultType.Element2dDisplacement, "2D Element Displacement" },
      { ResultType.Element2dProjectedMoment, "2D Element Projected Moment" },
      { ResultType.Element2dProjectedForce, "2D Element Projected Force" },
      { ResultType.Element2dProjectedStressBottom, "2D Element Projected Stress - Bottom" },
      { ResultType.Element2dProjectedStressMiddle, "2D Element Projected Stress - Middle" },
      { ResultType.Element2dProjectedStressTop, "2D Element Projected Stress - Top" },
      { ResultType.AssemblyForcesAndMoments, "Assembly Forces and Moments" }
    };

    //These are the exceptions to the rule that, in GSA, all records that relate to each table (i.e. the set with mutually-exclusive indices) have the same keyword
    public static Dictionary<GwaKeyword, GwaKeyword[]> IrregularKeywordGroups = new Dictionary<GwaKeyword, GwaKeyword[]> {
      { 
        GwaKeyword.LOAD_BEAM, 
        new GwaKeyword[] 
        { 
          GwaKeyword.LOAD_BEAM_POINT, GwaKeyword.LOAD_BEAM_UDL, GwaKeyword.LOAD_BEAM_LINE, GwaKeyword.LOAD_BEAM_PATCH, GwaKeyword.LOAD_BEAM_TRILIN 
        } 
      }
    };

    #region nodeAt_factors
    public static bool NodeAtCalibrated = false;
    //Set to defaults, which will be updated at calibration
    private static readonly Dictionary<string, float> UnitNodeAtFactors = new Dictionary<string, float>();

    private readonly object syncLock = new object();

    public GsaProxy()
    {
    }

    public void CalibrateNodeAt()
    {
      float coordValue = 1000;
      var unitCoincidentDict = new Dictionary<string, float>() { { "mm", 20 }, { "cm", 1 }, { "in", 1 }, { "m", 0.1f } };
      var units = new[] { "m", "cm", "mm", "in" };

      var proxy = new GsaProxy();
      proxy.NewFile(false);
      foreach (var u in units)
      {
        proxy.SetUnits(u);
        var nodeIndex = proxy.NodeAt(coordValue, coordValue, coordValue, unitCoincidentDict[u]);
        float factor = 1;
        var gwa = proxy.GetGwaForNode(nodeIndex);
        var pieces = gwa.Split(_GwaDelimiter);
        if (float.TryParse(pieces.Last(), out float z1))
        {
          if (z1 != coordValue)
          {
            var factorCandidate = coordValue / z1;

            nodeIndex = proxy.NodeAt(coordValue * factorCandidate, coordValue * factorCandidate, coordValue * factorCandidate, 1 * factorCandidate);

            gwa = proxy.GetGwaForNode(nodeIndex);
            pieces = gwa.Split(_GwaDelimiter);

            if (float.TryParse(pieces.Last(), out float z2) && z2 == 1000)
            {
              //it's confirmed
              factor = factorCandidate;
            }
          }
        }
        if (UnitNodeAtFactors.ContainsKey(u))
        {
          UnitNodeAtFactors[u] = factor;
        }
        else
        {
          UnitNodeAtFactors.Add(u, factor);
        }
      }

      proxy.Close();

      NodeAtCalibrated = true;
    }
    #endregion
    #endregion

    //These are accessed via a lock --
    private IComAuto GSAObject;
    private readonly List<string> batchSetGwa = new List<string>();
    private readonly List<string> batchBlankGwa = new List<string>();
    // --

    public string FilePath { get; set; }

    public char GwaDelimiter { get => _GwaDelimiter; }

    //Results-related
    private string resultDir = null;
    private Dictionary<ResultGroup, IResultsProcessor> resultProcessors = new Dictionary<ResultGroup, IResultsProcessor>();
    private List<ResultType> allResultTypes;

    private List<string> cases = null;

    //This is the factor relative to the SI units (N, m, etc) that the model is currently set to - this is relevant for results as they're always
    //exported to CSV in SI units
    private Dictionary<ResultUnitType, double> unitData = new Dictionary<ResultUnitType, double>();

    private string SpeckleGsaVersion;
    private string units = "m";

    //Used in ordering the calling of the conversion code
    public List<List<Type>> GetTxTypeDependencyGenerations(GSALayer layer)
    {
      InitialiseIfNecessary();
      return nativeTypeDependencyGenerations[layer];
    }

    #region File Operations
    /// <summary>
    /// Creates a new GSA file. Email address and server address is needed for logging purposes.
    /// </summary>
    /// <param name="emailAddress">User email address</param>
    /// <param name="serverAddress">Speckle server address</param>
    public bool NewFile(bool showWindow = true, object gsaInstance = null)
    {
      lock (syncLock)
      {
        if (GSAObject != null)
        {
          try
          {
            GSAObject.Close();
          }
          catch { }
          GSAObject = null;
        }

        GSAObject = (IComAuto)gsaInstance ?? new ComAuto();

        GSAObject.LogFeatureUsage("api::specklegsa::" +
            FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location)
                .ProductVersion + "::GSA " + GSAObject.VersionString()
                .Split(new char[] { '\n' })[0]
                .Split(new char[] { GwaDelimiter }, StringSplitOptions.RemoveEmptyEntries)[1]);

        var retCode = GSAObject.NewFile();
        if (retCode == 0)
        {
          // 0 = successful opening
          GSAObject.SetLocale(Locale.LOC_EN_GB);
          if (showWindow)
          {
            GSAObject.DisplayGsaWindow(true);
          }
          return true;
        }
        return false;
      }
    }

    /// <summary>
    /// Opens an existing GSA file. Email address and server address is needed for logging purposes.
    /// </summary>
    /// <param name="path">Absolute path to GSA file</param>
    /// <param name="emailAddress">User email address</param>
    /// <param name="serverAddress">Speckle server address</param>
    public bool OpenFile(string path, bool showWindow = true, object gsaInstance = null)
    {
      if (!File.Exists(path))
      {
        return false;
      }
      lock(syncLock)
      {
        if (GSAObject != null)
        {
          try
          {
            GSAObject.Close();
          }
          catch { }
          GSAObject = null;
        }

        GSAObject = (IComAuto)gsaInstance ?? new ComAuto();

        //Squash any exceptions from the telemetry
        try
        {
          GSAObject.LogFeatureUsage("api::specklegsa::" +
            FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location)
              .ProductVersion + "::GSA " + GSAObject.VersionString()
              .Split(new char[] { '\n' })[0]
              .Split(new char[] { GwaDelimiter }, StringSplitOptions.RemoveEmptyEntries)[1]);
        }
        catch 
        {
        }

        try
        {
          GSAObject.Open(path);
        }
        catch
        {
          return false;
        }
        FilePath = path;
        GSAObject.SetLocale(Locale.LOC_EN_GB);

        if (showWindow)
        {
          GSAObject.DisplayGsaWindow(true);
        }
      }
      return true;
    }

    public bool SaveAs(string filePath)
    {
      lock (syncLock)
      {
        if (GSAObject.SaveAs(filePath) == 0)
        {
          this.FilePath = filePath;
          return true;
        }
        return false;
      }
    }

    public bool Save()
    {
      lock (syncLock)
      {
        return (GSAObject.Save() == 0);
      }
    }

    /// <summary>
    /// Close GSA file.
    /// </summary>
    public void Close()
    {
      lock(syncLock)
      {
        try
        {
          GSAObject.Close();
        }
        catch { }
      }
    }
    #endregion

    #region node_resolution
    public int NodeAt(double x, double y, double z, double coincidenceTol)
    {
      float factor = (UnitNodeAtFactors != null && UnitNodeAtFactors.ContainsKey(units)) ? UnitNodeAtFactors[units] : 1;
      //Note: the outcome of this might need to be added to the caches!
      int? index = null;
      lock (syncLock)
      {
        index = GSAObject.Gen_NodeAt(x * factor, y * factor, z * factor, coincidenceTol * factor);
      }
      return index ?? 0;
    }

    public List<Type> GetNodeDependentTypes(GSALayer layer)
    {
      return nodeDependentSchemaTypesByLayer[layer];
    }
    #endregion

    #region gsa_list_resolution

    public List<int> ConvertGSAList(string list, GSAEntity type)
    {
      if (list == null)
      {
        return new List<int>();
      }

      string[] pieces = list.ListSplit(" ");
      pieces = pieces.Where(s => !string.IsNullOrEmpty(s)).ToArray();

      List<int> items = new List<int>();
      for (int i = 0; i < pieces.Length; i++)
      {
        if (pieces[i].IsDigits())
        {
          items.Add(Convert.ToInt32(pieces[i]));
        }
        else if (pieces[i].Contains('"'))
        {
          items.AddRange(ConvertNamedGSAList(pieces[i], type));
        }
        else if (pieces[i] == "to" && int.TryParse(pieces[i - 1], out int lowerRange) && int.TryParse(pieces[i + 1], out int upperRange))
        {
          for (int j = lowerRange + 1; j <= upperRange; j++)
          {
            items.Add(j);
          }
          i++;
        }
        else
        {
          try
          {
            int[] tempItems = null;

            lock (syncLock)
            {
              GSAObject.EntitiesInList(pieces[i], (GsaEntity)type, out tempItems);

              if (tempItems == null)
              {
                GSAObject.EntitiesInList("\"" + list + "\"", (GsaEntity)type, out tempItems);
              }
            }

            if (tempItems != null)
            {
              items.AddRange(tempItems);
            }
          }
          catch
          { }
        }
      }

      return items;
    }

    private List<int> ConvertNamedGSAList(string list, GSAEntity type)
    {
      list = list.Trim(new char[] { '"', ' ' });

      try
      {
        object result;
        lock(syncLock)
        {
          result = GSAObject.GwaCommand(string.Join(GwaDelimiter.ToString(), new[] { "GET", "LIST", list }));
        }

        string[] newPieces = ((string)result).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).Select((s, idx) => idx.ToString() + ":" + s).ToArray();

        string res = newPieces.FirstOrDefault();

        string[] pieces = res.Split(GwaDelimiter);

        return ConvertGSAList(pieces[pieces.Length - 1], type);
      }
      catch
      {
        try
        {
          lock(syncLock)
          {
            GSAObject.EntitiesInList("\"" + list + "\"", (GsaEntity)type, out int[] itemTemp);
            return (itemTemp == null) ? new List<int>() : itemTemp.ToList();
          }
        }
        catch { return new List<int>(); }
      }
    }

    //Created as part of functionality needed to convert a load case specification in the UI into an itemised list of load cases 
    //(including combinations)
    //Since EntitiesInList doesn't offer load cases/combinations as a GsaEntity type, a dummy GSA proxy (and therefore GSA instance) 
    //is created by the GSA cache and that calls the method below - even though it deals with nodes - as a roundabout way of
    //converting a list specification into valid load cases or combinations.   This method is called separately for load cases and combinations. 
    public List<int> GetNodeEntitiesInList(string spec)
    {
      var listType = GsaEntity.NODE;

      //Check that this indeed a list - the EntitiesInList call will function differently if given a single item
      var pieces = spec.Trim().Split(new[] { ' ' });
      if (pieces.Count() == 1)
      {
        spec = pieces[0] + " " + pieces[0];
      }

      var result = GSAObject.EntitiesInList(spec, ref listType, out int[] entities);
      return (entities != null && entities.Count() > 0)
        ? entities.ToList()
        : new List<int>();
    }
    #endregion

    #region type_dependency_tree
    private bool Initialise(GSALayer layer)
    {
      var assembly = GetType().Assembly; //This assembly
      var assemblyTypes = assembly.GetTypes().ToList();

      var gsaBaseType = typeof(GwaParser<GsaRecord>);
      var gsaAttributeType = typeof(GsaType);
      var gsaChildAttributeType = typeof(GsaChildType);
      var gwaParserInterfaceType = typeof(IGwaParser);

      var tableKeywordParserTypes = assemblyTypes.Where(t => Helper.InheritsOrImplements(t, gwaParserInterfaceType)
        && (t.CustomAttributes.Any(ca => ca.AttributeType == gsaAttributeType)
            && Helper.IsSelfContained(t)
            && (layer == GSALayer.Both
              || (layer == GSALayer.Design && Helper.IsDesignLayer(t))
              || (layer == GSALayer.Analysis && Helper.IsAnalysisLayer(t))))).ToList();

      var lineKeywordParserTypes = assemblyTypes.Where(t => t.CustomAttributes.Any(ca => ca.AttributeType == gsaChildAttributeType)).ToList();

      if (typeInfo.ContainsKey(layer))
      {
        typeInfo[layer].Clear();
        typeInfoIndicesByKeyword[layer].Clear();
        typeInfoIndicesBySchemaType[layer].Clear();
      }
      else
      {
        typeInfo.Add(layer, new List<KwTypeData>());
        typeInfoIndicesByKeyword.Add(layer, new Dictionary<GwaKeyword, int>());
        typeInfoIndicesBySchemaType.Add(layer, new Dictionary<Type, int>());
      }

      foreach (var t in tableKeywordParserTypes)
      {
        var newIndex = typeInfo[layer].Count();

        var ktd = new KwTypeData();
        if (t.IsAbstract)
        {
          var childParserTypes = lineKeywordParserTypes.Where(lpt => lpt.InheritsOrImplements(t)).ToList();
          foreach (var cpt in childParserTypes)
          {
            var childSchemaType = (Type)cpt.GetAttribute<GsaChildType>(GsaChildType.GsaSchemaTypeProperty);
            var childKw = (GwaKeyword)cpt.GetAttribute<GsaChildType>(GsaChildType.GwaKeywordProperty);
            ktd.LineKeywords.Add(childKw);
            ktd.LineSchemaTypes.Add(childSchemaType);
            ktd.LineParserTypes.Add(cpt);
            ktd.HasDifferentatedKeywords = true;

            typeInfoIndicesBySchemaType[layer].Add(childSchemaType, newIndex);
          }
        }
        else
        {
          ktd.TableSchemaType = t.BaseType.GenericTypeArguments.First();
          typeInfoIndicesBySchemaType[layer].Add(ktd.TableSchemaType, newIndex);
        }
        ktd.TableKeyword = Helper.GetGwaKeyword(t);
        ktd.TableParserType = t;

        typeInfoIndicesByKeyword[layer].Add(ktd.TableKeyword, newIndex);

        typeInfo[layer].Add(ktd);

      }

      var layerKeywords = typeInfo[layer].Select(d => d.TableKeyword).ToList();

      foreach (var ktd in typeInfo[layer])
      {
        ktd.RefTableKeywords = Helper.GetReferencedKeywords(ktd.TableParserType).Where(kw => layerKeywords.Any(k => k == kw)).ToList();

        //Handling the special case of nodes
        if (ktd.RefTableKeywords.Contains(GwaKeyword.NODE))
        {
          if (!nodeDependentSchemaTypesByLayer.ContainsKey(layer))
          {
            nodeDependentSchemaTypesByLayer.Add(layer, new List<Type>());
          }
          if (ktd.HasDifferentatedKeywords)
          {
            nodeDependentSchemaTypesByLayer[layer].UpsertList(ktd.LineSchemaTypes);
          }
          else
          {
            nodeDependentSchemaTypesByLayer[layer].UpsertList(ktd.TableSchemaType);
          }
        }
      }

      if (nativeTypeDependencyGenerations == null)
      {
        nativeTypeDependencyGenerations = new Dictionary<GSALayer, List<List<Type>>>();
      }
      if (!nativeTypeDependencyGenerations.ContainsKey(layer))
      {
        nativeTypeDependencyGenerations.Add(layer, new List<List<Type>>());
      }

      var retCol = new TypeTreeCollection<GwaKeyword>(typeInfo[layer].Select(d => d.TableKeyword));
      foreach (var d in typeInfo[layer])
      {
        retCol.Integrate(d.TableKeyword, d.RefTableKeywords.ToArray());
      }

      var gens = retCol.Generations();
      if (gens == null || gens.Count == 0)
      {
        return false;
      }

      foreach (var gen in gens)
      {
        var genSchemaTypes = new List<Type>();

        foreach (var keyword in gen.Where(kw => typeInfo[layer].Any(d => d.TableKeyword == kw)))
        {
          //var ktd = typeInfo[layer].FirstOrDefault(d => d.TableKeyword == keyword);
          var ktd = typeInfo[layer][typeInfoIndicesByKeyword[layer][keyword]];
          if (ktd.HasDifferentatedKeywords)
          {
            genSchemaTypes.AddRange(ktd.LineSchemaTypes);
          }
          else
          {
            genSchemaTypes.Add(ktd.TableSchemaType);
          }
        }
        nativeTypeDependencyGenerations[layer].Add(genSchemaTypes);
      }

      return (!initialisedError);
    }

    #endregion

    #region extract_gwa_fns

    public string GenerateApplicationId(Type schemaType, int gsaIndex)
    {
      foreach (var l in typeInfo.Keys)
      {
        foreach (var ktd in typeInfo[l])
        {
          if (ktd.ContainsSchemaType(schemaType))
          {
            return "gsa/" + ktd.TableKeyword + "-" + gsaIndex;
          }
        }
      }
      return "";
    }

    //Tuple: keyword | index | Application ID | GWA command | Set or Set At
    public bool GetGwaData(GSALayer layer, IProgress<string> loggingProgress, out List<GsaRecord> records, IProgress<int> incrementProgress = null)
    {
      if (!InitialiseIfNecessary())
      {
        records = null;
        return false;
      }

      var retRecords = new List<GsaRecord>();
      var dataLock = new object();
      var setKeywords = new List<GwaKeyword>();
      var setAtKeywords = new List<GwaKeyword>();
      var setNoIndexKeywords = new List<GwaKeyword>();
      var tempKeywordIndexCache = new Dictionary<GwaKeyword, List<int>>();

      var keywords = typeInfo[layer].Select(d => d.TableKeyword).ToList();

      foreach (var kw in keywords)
      {
        var tableParserType = typeInfo[layer][typeInfoIndicesByKeyword[layer][kw]].TableParserType;
        var sct = Helper.GetGwaSetCommandType(tableParserType);
        if (sct == GwaSetCommandType.Set)
        {
          setKeywords.Add(kw);
        }
        else if (sct == GwaSetCommandType.SetAt)
        {
          setAtKeywords.Add(kw);
        }
        else
        {
          setNoIndexKeywords.Add(kw);
        }
      }

      for (int i = 0; i < setNoIndexKeywords.Count(); i++)
      {
        var newCommand = "GET_ALL" + GwaDelimiter + setNoIndexKeywords[i];

        string[] gwaLines;

        try
        {
          lock (syncLock)
          {
            gwaLines = ((string)GSAObject.GwaCommand(newCommand)).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
          }
        }
        catch
        {
          gwaLines = new string[0];
        }

          foreach (var gwa in gwaLines)
          {
            var pieces = gwa.ListSplit(_GwaDelimiter).ToList();
            if (pieces[0].StartsWith("set", StringComparison.InvariantCultureIgnoreCase))
            {
              pieces = pieces.Skip(1).ToList();
            }
            var keywordAndVersion = pieces.First();
            var keywordPieces = keywordAndVersion.Split('.');
            if (keywordPieces.Count() == 2 && keywordPieces.Last().All(c => char.IsDigit(c))
              && int.TryParse(keywordPieces.Last(), out int ver)
              && keywordPieces.First().TryParseStringValue(out GwaKeyword kw))
            {
              var ktd = typeInfo[layer][typeInfoIndicesByKeyword[layer][kw]];
              if (ktd != null)
              {
                var schemaType = ktd.GetSchemaType(kw);
                var parser = (IGwaParser)Activator.CreateInstance(ktd.GetParserType(schemaType));
                if (parser.FromGwa(gwa))
                {
                  retRecords.Add(parser.Record);
                }
                else if (loggingProgress != null)
                {
                  loggingProgress.Report(FormulateParsingErrorContext(parser.Record, schemaType.Name));
                }
              }
            }
          }
      }


      #region read_set_keyword_records
      for (int i = 0; i < setKeywords.Count(); i++)
      {
        var newCommand = "GET_ALL" + GwaDelimiter + setKeywords[i];
        var isNode = (setKeywords[i] == GwaKeyword.NODE);
        var isElement = (setKeywords[i] == GwaKeyword.EL);

        string[] gwaLines;

        try
        {
          lock(syncLock)
          {
            gwaLines = ((string)GSAObject.GwaCommand(newCommand)).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
          }
        }
        catch
        {
          gwaLines = new string[0];
        }

        Parallel.ForEach(gwaLines, gwa =>
        {
          if (ParseGeneralGwa(gwa, out GwaKeyword? keyword, out int? version, out int? index, out string streamId, out string appId, out string gwaWithoutSet, out string keywordAndVersion)
            && keyword.HasValue)
          {
            var ktd = typeInfo[layer][typeInfoIndicesByKeyword[layer][keyword.Value]];
            if (ktd != null)
            {
              index = index ?? 0;
              var originalSid = "";
              var kwStr = keyword.Value.GetStringValue();

              //For some GET_ALL calls, records with other keywords are returned, too.  Example: GET_ALL TASK returns TASK, TASK_TAG and ANAL records
              if (keyword.Value == setKeywords[i])
              {
                if (string.IsNullOrEmpty(streamId))
                {
                  //Slight hardcoding for optimisation here: the biggest source of GetSidTagValue calls would be from nodes, and knowing
                  //(at least in GSA v10 build 63) that GET_ALL NODE does return SID tags, the call is avoided for NODE keyword
                  if (!isNode && !isElement)
                  {
                    try
                    {
                      lock (syncLock) {streamId = GSAObject.GetSidTagValue(kwStr, index.Value, SID_STRID_TAG); }
                    }
                    catch { }
                  }
                }
                else
                {
                  originalSid += FormatStreamIdSidTag(streamId);
                }

                if (string.IsNullOrEmpty(appId))
                {
                  //Again, the same optimisation as explained above
                  if (!isNode && !isElement)
                  {
                    try
                    {
                      lock (syncLock)
                      {
                        appId = GSAObject.GetSidTagValue(kwStr, index.Value, SID_APPID_TAG);
                      }
                    }
                    catch { }
                  }
                }
                else
                {
                  originalSid += FormatStreamIdSidTag(appId);
                }

                var newSid = FormatStreamIdSidTag(streamId) + FormatApplicationIdSidTag(appId);
                if (!string.IsNullOrEmpty(newSid))
                {
                  if (string.IsNullOrEmpty(originalSid))
                  {
                    gwaWithoutSet = gwaWithoutSet.Replace(keywordAndVersion, keywordAndVersion + ":" + newSid);
                  }
                  else
                  {
                    gwaWithoutSet = gwaWithoutSet.Replace(originalSid, newSid);
                  }
                }

                var schemaType = ktd.GetSchemaType(keyword.Value);
                var t = ktd.GetParserType(schemaType);
                {
                  var parser = (IGwaParser)Activator.CreateInstance(t);
                  if (parser.FromGwa(gwa))
                  {
                    if (!parser.Record.Index.HasValue && index.HasValue)
                    {
                      parser.Record.Index = index.Value;
                    }
                    parser.Record.StreamId = streamId;
                    parser.Record.ApplicationId = appId;

                    lock (dataLock)
                    {
                      if (!tempKeywordIndexCache.ContainsKey(keyword.Value))
                      {
                        tempKeywordIndexCache.Add(keyword.Value, new List<int>());
                      }
                      if (!tempKeywordIndexCache[keyword.Value].Contains(index.Value))
                      {
                        retRecords.Add(parser.Record);
                        tempKeywordIndexCache[keyword.Value].Add(index.Value);
                      }
                    }
                  }
                  else if (loggingProgress != null)
                  {
                    loggingProgress.Report(FormulateParsingErrorContext(parser.Record, schemaType.Name));
                  }
                }
              }
            }
          }
        });

        if (incrementProgress != null)
        {
          incrementProgress.Report(1);
        }
      }
      #endregion

      #region read_set_at_keyword_records
      for (int i = 0; i < setAtKeywords.Count(); i++)
      {
        int highestIndex = 0;
        lock(syncLock)
        {
          highestIndex = GSAObject.GwaCommand("HIGHEST" + GwaDelimiter + setAtKeywords[i]);
        }

        for (int j = 1; j <= highestIndex; j++)
        {
          var newCommand = string.Join(GwaDelimiter.ToString(), new[] { "GET", setAtKeywords[i].GetStringValue(), j.ToString() });

          string gwaLine = "";
          try
          {
            lock (syncLock)
            {
              gwaLine = GSAObject.GwaCommand(newCommand);
            }
          }
          catch { }

          if ((gwaLine != "") 
            && ParseGeneralGwa(gwaLine, out GwaKeyword? keyword, out int? version, out int? index, out string streamId, out string appId, out string gwaWithoutSet,
              out string keywordAndVersion) && keyword.HasValue)
          {
            var ktd = typeInfo[layer][typeInfoIndicesByKeyword[layer][keyword.Value]];
            if (ktd != null)
            {
              var kwStr = keyword.Value.GetStringValue();
              var originalSid = "";
              if (string.IsNullOrEmpty(streamId))
              {
                try
                {
                  lock (syncLock)
                  {
                    streamId = GSAObject.GetSidTagValue(kwStr, j, SID_STRID_TAG);
                  }
                }
                catch { }
              }
              else
              {
                originalSid += FormatStreamIdSidTag(streamId);
              }
              if (string.IsNullOrEmpty(appId))
              {
                lock (syncLock)
                {
                  appId = GSAObject.GetSidTagValue(kwStr, j, SID_APPID_TAG);
                }
              }
              else
              {
                originalSid += FormatStreamIdSidTag(appId);
              }

              var newSid = FormatStreamIdSidTag(streamId) + FormatApplicationIdSidTag(appId);
              if (!string.IsNullOrEmpty(originalSid) && !string.IsNullOrEmpty(newSid))
              {
                gwaWithoutSet.Replace(originalSid, newSid);
              }
              
              var kw = ktd.HasDifferentatedKeywords ? (GwaKeyword)Enum.Parse(typeof(GwaKeyword), keywordAndVersion.Split('.')[0]) : keyword.Value;

              var schemaType = ktd.GetSchemaType(kw);
              var t = ktd.GetParserType(schemaType);

              var parser = (IGwaParser)Activator.CreateInstance(t);
              if (parser.FromGwa(gwaLine))
              {
                if (!parser.Record.Index.HasValue)
                {
                  parser.Record.Index = j;
                }
                parser.Record.StreamId = streamId;
                parser.Record.ApplicationId = appId;

                lock (dataLock)
                {
                  if (!tempKeywordIndexCache.ContainsKey(setAtKeywords[i]))
                  {
                    tempKeywordIndexCache.Add(setAtKeywords[i], new List<int>());
                  }
                  if (!tempKeywordIndexCache[setAtKeywords[i]].Contains(j))
                  {
                    retRecords.Add(parser.Record);
                    tempKeywordIndexCache[setAtKeywords[i]].Add(j);
                  }
                }
              }
              else if (loggingProgress != null)
              {
                loggingProgress.Report(FormulateParsingErrorContext(parser.Record, schemaType.Name));
              }
            }
          }
        }
        if (incrementProgress != null)
        {
          incrementProgress.Report(1);
        }
      }
      #endregion

      records = retRecords;
      return true; 
    }

    private string FormulateParsingErrorContext(GsaRecord gsaRecord, string typeName)
    {
      var errorMessage = "Parsing the GWA data for: " + typeName;
      if (gsaRecord != null)
      {
        if (gsaRecord.Index.HasValue)
        {
          errorMessage += " at index: " + gsaRecord.Index.Value.ToString();
        }
        if (!string.IsNullOrEmpty(gsaRecord.ApplicationId) && gsaRecord.ApplicationId.Length > 0)
        {
          errorMessage += " with ApplicationId = " + gsaRecord.ApplicationId;
        }
      }
      return errorMessage;
    }

    private bool InitialiseIfNecessary()
    {
      if (!initialised && !initialisedError)
      {
        nodeDependentSchemaTypesByLayer.Clear();

        if (!Initialise(GSALayer.Design) || !Initialise(GSALayer.Analysis) || !Initialise(GSALayer.Both))
        {
          initialisedError = true;
          //Already tried this layer once and it was an error, so don't try again
          return false;
        }

        initialised = true;
      }
     
      return !initialisedError;
    }

    private string FormatApplicationId(string keyword, int index, string applicationId)
    {
      //It has been observed that sometimes GET commands don't include the SID despite there being one.  For some (but not all)
      //of these instances, the SID is available through an explicit call for the SID, so try that next
      if (string.IsNullOrEmpty(applicationId))
      {
        lock(syncLock)
        {
          return GSAObject.GetSidTagValue(keyword, index, SID_APPID_TAG);
        }
      }
      return applicationId;
    }

    private int ExtractGwaIndex(string gwaRecord)
    {
      var pieces = gwaRecord.Split(GwaDelimiter);
      return (int.TryParse(pieces[1], out int index)) ? index : 0;
    }

    private bool GetUnitDataGwa(out List<string> gwa)
    {
      var newCommand = "GET_ALL" + GwaDelimiter + GwaKeyword.UNIT_DATA.GetStringValue();

      string[] gwaLines;

      try
      {
        lock(syncLock)
        {
          gwaLines = ((string)GSAObject.GwaCommand(newCommand)).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
        }
      }
      catch
      {
        gwa = null;
        return false;
      }
      gwa = gwaLines.ToList();
      return true;
    }
    #endregion

    #region writing_gwa

    public void WriteModel(List<GsaRecord> gsaRecords, IProgress<string> loggingProgress, GSALayer layer = GSALayer.Both)
    {
      var parsersToUseBySchemaType = new Dictionary<Type, List<IGwaParser>>();
      foreach (var r in gsaRecords)
      {
        var t = r.GetType();
        if (!parsersToUseBySchemaType.ContainsKey(t))
        {
          parsersToUseBySchemaType.Add(t, new List<IGwaParser>());
        }
        try
        {
          var ktd = typeInfo[layer][typeInfoIndicesBySchemaType[layer][t]];
          var parser = (IGwaParser)Activator.CreateInstance(ktd.GetParserType(t), r);
          parsersToUseBySchemaType[t].Add(parser);
        }
        catch
        {

        }
      }

      var typeGens = GetTxTypeDependencyGenerations(layer);
      foreach (var gen in typeGens)
      {
        foreach (var t in gen)
        {
          if (parsersToUseBySchemaType.ContainsKey(t))
          {
            var orderedParsers = parsersToUseBySchemaType[t].OrderBy(p => p.Record.Index).ToList();

            foreach (var op in orderedParsers)
            {
              try
              {
                if (op.Gwa(out var gwas, true))
                {
                  gwas.ForEach(g => SetGwa(g));
                }
              }
              catch (Exception ex)
              {
                loggingProgress.Report("Unable to generate GWA for " + t.Name 
                  + (string.IsNullOrEmpty(op.Record.ApplicationId) ? "" : " with applicationID = " + op.Record.ApplicationId));
              }
            }
          }
        }
      }

      try
      {
        Sync();
      }
      catch (Exception ex)
      {
        if (loggingProgress != null)
        {
          loggingProgress.Report("Unable to write to the GSA model: " + ex.Message);
        }
      }
    }

    //Assumed to be the full SET or SET_AT command
    private void SetGwa(string gwaCommand)
    {
      lock (syncLock)
      {
        batchSetGwa.Add(gwaCommand);
      }
    }

    private void Sync()
    {
      if (batchBlankGwa.Count() > 0)
      {
        lock(syncLock)
        {
          var batchBlankCommand = string.Join("\r\n", batchBlankGwa);
          var blankCommandResult = GSAObject.GwaCommand(batchBlankCommand);
          batchBlankGwa.Clear();
        }
      }

      if (batchSetGwa.Count() > 0)
      {
        lock(syncLock)
        {
          var batchSetCommand = string.Join("\r\n", batchSetGwa);
          var setCommandResult = GSAObject.GwaCommand(batchSetCommand);
          batchSetGwa.Clear();
        }
      }
    }

    public string GetGwaForNode(int index)
    {
      var gwaCommand = string.Join(GwaDelimiter.ToString(), new[] { "GET", "NODE.3", index.ToString() });
      lock(syncLock)
      {
        return (string)GSAObject.GwaCommand(gwaCommand);
      }
    }

    /// <summary>
    /// Update GSA case and task links. This should be called at the end of changes.
    /// </summary>
    public bool UpdateCasesAndTasks()
    {
      try
      {
        lock(syncLock)
        {
          GSAObject.ReindexCasesAndTasks();
        }
        return true;
      }
      catch
      {
        return false;
      }
    }

    public void DeleteGWA(string keyword, int index, GwaSetCommandType gwaSetCommandType)
    {
      var command = string.Join(GwaDelimiter.ToString(), new[] { (gwaSetCommandType == GwaSetCommandType.Set) ? "BLANK" : "DELETE", keyword, index.ToString() });
      lock(syncLock)
      {
        if (gwaSetCommandType == GwaSetCommandType.Set)
        {
          //For synchronising later
          batchBlankGwa.Add(command);
        }
        else
        {
          GSAObject.GwaCommand(command);
        }
      };
    }

    #endregion

    #region Document Properties

    public string GetTopLevelSid()
    {
      string sid = "";
      try
      {
        lock(syncLock)
        {
          var gwa = (string)GSAObject.GwaCommand("GET" + GwaDelimiter + "SID");
          sid = gwa.Substring(gwa.IndexOf(GwaDelimiter) + 1).Replace("\"\"", "\"");
          if (sid[0] == '"')
          {
            sid = sid.Substring(1);
          }
          if (sid.Last() == '"')
          {
            sid = sid.Substring(0, (sid.Length - 1));
          }
        }
      }
      catch
      {
        //File doesn't have SID
      }
      return sid;
    }

    public bool SetTopLevelSid(string StreamState)
    {
      try
      {
        lock(syncLock)
        {
          GSAObject.GwaCommand(string.Join(GwaDelimiter.ToString(), new[] { "SET", "SID", StreamState }));
        }
        return true;
      }
      catch
      {
        return false;
      }
    }

    /// <summary>
    /// Extract the title of the GSA model.
    /// </summary>
    /// <returns>GSA model title</returns>
    public string GetTitle()
    {
      string res;
      lock(syncLock)
      {
        res = (string)GSAObject.GwaCommand("GET" + GwaDelimiter + "TITLE");
      }

      string[] pieces = res.ListSplit(GwaDelimiter);

      return pieces.Length > 1 ? pieces[1] : "My GSA Model";
    }

    public string[] GetTolerances()
    {
      lock(syncLock)
      {
        return ((string)GSAObject.GwaCommand("GET" + GwaDelimiter + "TOL")).ListSplit(GwaDelimiter);
      }
    }

    #region load_case_expansion

    public List<string> ExpandLoadCasesAndCombinations(string loadCaseString, List<int> analIndices, List<int> comboIndices)
    {
      var retList = new List<string>();

      if (string.IsNullOrEmpty(loadCaseString) || !ProcessLoadCaseCombinationSpec(loadCaseString, out List<string> aParts, out List<string> cParts))
      {
        return retList;
      }

      if (loadCaseString.Equals("all", StringComparison.InvariantCultureIgnoreCase))
      {
        retList.AddRange(analIndices.Select(ai => "A" + ai));
        retList.AddRange(comboIndices.Select(ai => "C" + ai));
        return retList;
      }

      var tasks = new List<Task>();
      var retListLock = new object();

      if (aParts.Count() > 0)
      {
#if !DEBUG
        tasks.Add(Task.Run(() =>
#endif
        {
          var aSpecs = ExpandLoadCasesAndCombinationSubset(aParts, "A", analIndices);
          if (aSpecs != null && aSpecs.Count() > 0)
          {
            lock (retListLock)
            {
              retList.AddRange(aSpecs);
            }
          }
        }
#if !DEBUG
        ));
#endif
      }

      if (cParts.Count() > 0)
      {
#if !DEBUG
        tasks.Add(Task.Run(() =>
#endif
        {
          var cSpecs = ExpandLoadCasesAndCombinationSubset(cParts, "C", comboIndices);
          if (cSpecs != null && cSpecs.Count() > 0)
          {
            lock (retListLock)
            {
              retList.AddRange(cSpecs);
            }
          }
        }
#if !DEBUG
        ));
#endif
      }

#if !DEBUG
      Task.WaitAll(tasks.ToArray());
#endif

      return retList;
    }

    private List<string> ExpandLoadCasesAndCombinationSubset(List<string> listParts, string marker, List<int> cachedIndices)
    {
      var specs = new List<string>();
      if (listParts.All(sp => IsMarkerPattern(sp)))
      {
        var aPartsDistinct = listParts.Distinct();
        foreach (var a in aPartsDistinct)
        {
          if (a.Length > 1 && int.TryParse(a.Substring(1), out int specIndex))
          {
            if (cachedIndices.Contains(specIndex))
            {
              specs.Add(a);
            }
          }
        }
      }
      else
      {
        specs = (listParts[0].ToLower() == "all")
          ? cachedIndices.Select(i => marker + i).ToList()
          : ExpandSubsetViaProxy(cachedIndices.ToList(), listParts, marker);
      }
      return specs;
    }

    private bool ProcessLoadCaseCombinationSpec(string spec, out List<string> aParts, out List<string> cParts)
    {
      aParts = new List<string>();
      cParts = new List<string>();
      var formattedSpec = spec.ToLower().Trim();

      if (formattedSpec.StartsWith("all"))
      {
        aParts.Add("All");
        cParts.Add("All");
        return true;
      }

      var stage1Parts = new List<string>();
      //break up the string by any A<number> and C<number> substrings
      var inCurrSpec = false;
      var currSpec = "";
      var bnSpec = "";  //Between spec, could be any string
      for (int i = 0; i < formattedSpec.Length; i++)
      {
        if (Char.IsDigit(formattedSpec[i]))
        {
          if (i == 0)
          {
            bnSpec += formattedSpec[i];
          }
          else
          {
            if (formattedSpec[i - 1] == 'a' || formattedSpec[i - 1] == 'c')
            {
              //Previous is A or C and current is a number
              inCurrSpec = true;
              currSpec = spec[i - 1].ToString() + spec[i].ToString();
              bnSpec = bnSpec.Substring(0, bnSpec.Length - 1);
              if (bnSpec.Length > 0)
              {
                stage1Parts.Add(bnSpec);
              }
              bnSpec = "";
            }
            else if (Char.IsNumber(formattedSpec[i - 1]))
            {
              //Previous is not A or C but current is a number - assume continuation of previous state
              if (inCurrSpec)
              {
                currSpec += spec[i].ToString();
              }
              else
              {
                bnSpec += spec[i].ToString();
              }
            }
          }
        }
        else if (Char.IsLetter(formattedSpec[i]))
        {
          //it's not a number, so close off new part if relevant
          if (inCurrSpec)
          {
            stage1Parts.Add(currSpec);
            currSpec = "";
          }

          inCurrSpec = false;
          bnSpec += spec[i].ToString();
        }
        else
        {
          if (inCurrSpec)
          {
            stage1Parts.Add(currSpec);
            currSpec = "";
            inCurrSpec = false;
          }
          else if (bnSpec.Length > 0)
          {
            stage1Parts.Add(bnSpec);
            bnSpec = "";
          }
        }
      }

      if (inCurrSpec)
      {
        stage1Parts.Add(currSpec);
      }
      else
      {
        stage1Parts.Add(bnSpec);
      }

      //Now break up these items into groups, delimited by a switch between an A_ and C_ mention, or an all-number item and an A_ or C_ mention
      var partsAorC = stage1Parts.Select(p => GetAorC(p)).ToList();

      if (partsAorC.All(p => p == 0))
      {
        return false;
      }
      int? firstViableIndex = null;
      for (int i = 0; i < partsAorC.Count(); i++)
      {
        if (partsAorC[i] > 0)
        {
          firstViableIndex = i;
          break;
        }
      }
      if (!firstViableIndex.HasValue)
      {
        return false;
      }

      int currAorC = GetAorC(stage1Parts[firstViableIndex.Value]); // A = 1, C = 2
      if (currAorC == 1)
      {
        aParts.Add(stage1Parts[firstViableIndex.Value]);
      }
      else
      {
        cParts.Add(stage1Parts[firstViableIndex.Value]);
      }

      for (int i = (firstViableIndex.Value + 1); i < stage1Parts.Count(); i++)
      {
        var itemAorC = GetAorC(stage1Parts[i]);

        if (itemAorC == 0 || itemAorC == currAorC)
        {
          //Continue on
          if (currAorC == 1)
          {
            aParts.Add(stage1Parts[i]);
          }
          else
          {
            cParts.Add(stage1Parts[i]);
          }
        }
        else if (itemAorC != currAorC)
        {
          if (currAorC == 1)
          {
            RemoveTrailingLettersOnlyItems(ref aParts);

            cParts.Add(stage1Parts[i]);
          }
          else if (currAorC == 2)
          {
            RemoveTrailingLettersOnlyItems(ref cParts);

            aParts.Add(stage1Parts[i]);
          }
          currAorC = itemAorC;
        }
      }

      return (aParts.Count > 0 || cParts.Count > 0);
    }

    private static void RemoveTrailingLettersOnlyItems(ref List<string> parts)
    {
      var found = true;

      //First remove any all-letter items from the last state
      var index = (parts.Count - 1);

      do
      {
        if (parts[index].All(p => (char.IsLetter(p))))
        {
          parts.RemoveAt(index);
          index--;
        }
        else
        {
          found = false;
        }
      } while (found);
    }

    private static int GetAorC(string part)
    {
      if (string.IsNullOrEmpty(part) || part.Length < 2 || !(Char.IsLetter(part[0]) && Char.IsDigit(part[1])))
      {
        return 0;
      }
      return (char.ToLowerInvariant(part[0]) == 'a') ? 1 : (char.ToLowerInvariant(part[0]) == 'c') ? 2 : 0;
    }

    private bool IsMarkerPattern(string item) => (item.Length >= 2 && char.IsLetter(item[0]) && item.Substring(1).All(c => char.IsDigit(c)));

    private string RemoveMarker(string item) => (IsMarkerPattern(item) ? item.Substring(1) : item);
    

    //Since EntitiesInList doesn't offer load cases/combinations as a GsaEntity type, a dummy GSA instance is 
    //created where a node is created for every load case/combination in the specification.  This is done separately for load cases and combinations.
    private List<string> ExpandSubsetViaProxy(List<int> existingIndices, List<string> specParts, string marker)
    {
      var items = new List<string>();
      var gsaProxyTemp = new GsaProxy();

      try
      {
        gsaProxyTemp.NewFile(false);

        for (int i = 0; i < existingIndices.Count(); i++)
        {
          var testNode = new GSA.API.GwaSchema.GsaNode() { Index = existingIndices[i], Name = existingIndices[i].ToString() };
          gsaProxyTemp.WriteModel(new List<GsaRecord> { testNode }, null, GSALayer.Both);
        }
        gsaProxyTemp.Sync();
        var tempSpec = string.Join(" ", specParts.Select(a => RemoveMarker(a)));
        items.AddRange(gsaProxyTemp.GetNodeEntitiesInList(tempSpec).Select(e => marker + e.ToString()));
      }
      catch { }
      finally
      {
        gsaProxyTemp.Close();
        gsaProxyTemp = null;
      }

      return items;
    }
    #endregion

    /// <summary>
    /// Updates the GSA unit stored in SpeckleGSA.
    /// </summary>
    public string GetUnits()
    {
      string retrievedUnits;
      lock(syncLock)
      {
        retrievedUnits = (GSAObject.GwaCommand(string.Join(GwaDelimiter.ToString(), new[] { "GET", "UNIT_DATA.1", "LENGTH" }))).ListSplit(GwaDelimiter)[2];
      }
      this.units = retrievedUnits;
      return retrievedUnits;
    }

    public bool SetUnits(string units)
    {
      this.units = units;
      int retCode;
      lock(syncLock)
      {
        var retCode1 = GSAObject.GwaCommand(string.Join(GwaDelimiter.ToString(), new[] { "SET", "UNIT_DATA", "LENGTH", units }));
        var retCode2 = GSAObject.GwaCommand(string.Join(GwaDelimiter.ToString(), new[] { "SET", "UNIT_DATA", "DISP", units }));
        var retCode3 = GSAObject.GwaCommand(string.Join(GwaDelimiter.ToString(), new[] { "SET", "UNIT_DATA", "SECTION", units }));
        //Apparently 1 seems to be the code for success, from observation
        retCode = (new[] { retCode1, retCode2, retCode3 }).Min();
      }
      return (retCode == 1);
    }
    #endregion

    #region Views
    /// <summary>
    /// Update GSA viewer. This should be called at the end of changes.
    /// </summary>
    public bool UpdateViews()
    {
      try
      {
        lock(syncLock)
        {
          GSAObject.UpdateViews();
        }
        return true;
      }
      catch
      {
        return false;
      }
    }

    #region results

    public bool PrepareResults(IEnumerable<ResultType> resultTypes, int numBeamPoints = 3)
    {
      this.resultDir = Path.Combine(Environment.CurrentDirectory, "GSAExport");
      this.allResultTypes = resultTypes.ToList();

      ProcessUnitGwaData();

      //First delete all existing csv files in the results directory to avoid confusion
      if (!ClearResultsDirectory())
      {
        return false;
      }
      var retCode = GSAObject.ExportToCsv(resultDir, numBeamPoints, true, true, ",");
      if (retCode == 0)
      {
        //Assume that
        return true;
      }
      return false;
    }

    public bool LoadResults(ResultGroup group, out int numErrorRows, List<string> cases = null, List<int> elemIds = null)
    {
      numErrorRows = 0;
      if (group == ResultGroup.Assembly)
      {
        resultProcessors.Add(group, new ResultsAssemblyProcessor(Path.Combine(resultDir, @"result_assembly\result_assembly.csv"), unitData, allResultTypes, cases, elemIds));
      }
      else if (group == ResultGroup.Element1d)
      {
        resultProcessors.Add(group, new Results1dProcessor(Path.Combine(resultDir, @"result_elem_1d\result_elem_1d.csv"), unitData, allResultTypes, cases, elemIds));
      }
      else if (group == ResultGroup.Element2d)
      {
        resultProcessors.Add(group, new Results2dProcessor(Path.Combine(resultDir, @"result_elem_2d\result_elem_2d.csv"), unitData, allResultTypes, cases, elemIds));
      }
      else if (group == ResultGroup.Node)
      {
        resultProcessors.Add(group, new ResultsNodeProcessor(Path.Combine(resultDir, @"result_node\result_node.csv"), unitData, allResultTypes, cases, elemIds));
      }
      else
      {
        return false;
      }
      
      return resultProcessors[group].LoadFromFile(out numErrorRows);
    }


    public bool GetResultRecords(ResultGroup group, int index, string loadCase, out List<CsvRecord> records)
    {
      if (resultProcessors.ContainsKey(group) && resultProcessors[group].GetResultRecords(index, loadCase, out records))
      {
        return true;
      }
      records = null;
      return false;
    }

    public bool GetResultRecords(ResultGroup group, int index, out List<CsvRecord> records)
    {
      if (resultProcessors.ContainsKey(group) && resultProcessors[group].GetResultRecords(index, out records))
      {
        return true;
      }
      records = null;
      return false;
    }

    public bool ClearResults(ResultGroup group)
    {
      if (resultProcessors.ContainsKey(group))
      {
        var removed = resultProcessors.Remove(group);
        if (removed)
        {
          GC.Collect();
          return true;
        }
      }
      return false;
    }

    public bool Clear()
    {
      resultProcessors.Clear();
      GC.Collect();
      initialised = false;
      initialisedError = false;
      return true;
    }

    private bool ClearResultsDirectory()
    {
      var di = new DirectoryInfo(resultDir);
      if (!di.Exists)
      {
        return true;
      }

      foreach (DirectoryInfo dir in di.GetDirectories())
      {
        foreach (FileInfo file in dir.GetFiles())
        {
          if (!file.Extension.Equals(".csv", StringComparison.InvariantCultureIgnoreCase))
          {
            return false;
          }
        }
      }

      foreach (FileInfo file in di.GetFiles())
      {
        file.Delete();
      }
      foreach (DirectoryInfo dir in di.GetDirectories())
      {
        dir.Delete(true);
      }
      return true;
    }

    private bool ProcessUnitGwaData()
    {
      if (!GetUnitDataGwa(out var unitGwaLines) || unitGwaLines == null || unitGwaLines.Count() == 0)
      {
        return false;
      }
      unitData.Clear();

      foreach (var gwa in unitGwaLines)
      {
        var firstDelimiterIndex = gwa.IndexOf(GwaDelimiter);
        var gwaLine = (gwa.StartsWith("set", StringComparison.InvariantCultureIgnoreCase)) ? gwa.Substring(firstDelimiterIndex) : gwa;

        var pieces = gwaLine.Split(GwaDelimiter);

        if (Enum.TryParse(pieces[1], true, out ResultUnitType rut) && float.TryParse(pieces.Last(), out float factor))
        {
          unitData.Add(rut, factor);
        }
      }
      return true;
    }
    #endregion

    private bool ContainsCaseInsensitive(string a, string b)
    {
      return (a.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    #endregion

    #region telemetry

    public void SetAppVersionForTelemetry(string speckleGsaAppVersion)
    {
      SpeckleGsaVersion = speckleGsaAppVersion;
    }

    public void SendTelemetry(params string[] messagePortions)
    {
      var finalMessagePortions = new List<string> { "SpeckleGSA", SpeckleGsaVersion, GSAObject.VersionString() };
      finalMessagePortions.AddRange(messagePortions);
      var message = string.Join("::", finalMessagePortions);
      GSAObject.LogFeatureUsage(message);
    }
    #endregion


    #region static_fns
    //This should only be called if the GWA is known to be either Set or SetAt, not SetNoIndex
    public static bool ParseGeneralGwa(string fullGwa, out GwaKeyword? keyword, out int? version, out int? index, out string streamId, out string applicationId,
      out string gwaWithoutSet, out string keywordAndVersion)
    {
      var pieces = fullGwa.ListSplit(_GwaDelimiter).ToList();
      keyword = null;
      version = null;
      keywordAndVersion = "";
      streamId = "";
      applicationId = "";
      index = null;
      gwaWithoutSet = fullGwa;

      if (pieces.Count() < 2)
      {
        return false;
      }

      //Remove the Set for the purpose of this method
      if (pieces[0].StartsWith("set", StringComparison.InvariantCultureIgnoreCase))
      {
        if (pieces[0].StartsWith("set_at", StringComparison.InvariantCultureIgnoreCase))
        {
          if (int.TryParse(pieces[1], out int foundIndex))
          {
            index = foundIndex;
          }

          //For SET_ATs the format is SET_AT <index> <keyword> .., so remove the first two
          pieces.Remove(pieces[1]);
          pieces.Remove(pieces[0]);
        }
        else
        {
          if (int.TryParse(pieces[2], out int foundIndex))
          {
            index = foundIndex;
          }

          pieces.Remove(pieces[0]);
        }
      }
      else
      {
        if (int.TryParse(pieces[1], out int foundIndex))
        {
          index = foundIndex;
        }
      }

      var delimIndex = pieces[0].IndexOf(':');
      var hasSid = (delimIndex > 0);
      keywordAndVersion = hasSid ? pieces[0].Substring(0, delimIndex) : pieces[0];

      if (!string.IsNullOrEmpty(keywordAndVersion))
      {
        var keywordPieces = keywordAndVersion.Split('.');
        if (keywordPieces.Count() == 2 && keywordPieces.Last().All(c => char.IsDigit(c))
          && int.TryParse(keywordPieces.Last(), out int ver)
          && keywordPieces.First().TryParseStringValue(out GwaKeyword kw))
        {
          version = ver;
          keyword = kw;

          if (hasSid)
          {
            //An SID has been found
            var sidTags = pieces[0].Substring(delimIndex);
            var match = Regex.Match(sidTags, "(?<={" + SID_STRID_TAG + ":).*?(?=})");
            streamId = (!string.IsNullOrEmpty(match.Value)) ? match.Value : "";
            match = Regex.Match(sidTags, "(?<={" + SID_APPID_TAG + ":).*?(?=})");
            applicationId = (!string.IsNullOrEmpty(match.Value)) ? match.Value : "";
          }

          foreach (var groupKeyword in IrregularKeywordGroups.Keys)
          {
            if (IrregularKeywordGroups[groupKeyword].Contains(keyword.Value))
            {
              keyword = groupKeyword;
              break;
            }
          }
          gwaWithoutSet = string.Join(_GwaDelimiter.ToString(), pieces);
          return true;
        }
      }

      return false;
    }

    public static string FormatApplicationIdSidTag(string value)
    {
      return (string.IsNullOrEmpty(value) ? "" : "{" + SID_APPID_TAG + ":" + value.Replace(" ", "") + "}");
    }

    public static string FormatStreamIdSidTag(string value)
    {
      return (string.IsNullOrEmpty(value) ? "" : "{" + SID_STRID_TAG + ":" + value.Replace(" ", "") + "}");
    }

    public static string FormatSidTags(string streamId = "", string applicationId = "")
    {
      return FormatStreamIdSidTag(streamId) + FormatApplicationIdSidTag(applicationId);
    }

    #endregion
  }

  internal class KwTypeData
  {
    public GwaKeyword TableKeyword;
    public Type TableParserType;
    public Type TableSchemaType;
    public List<GwaKeyword> RefTableKeywords;

    public bool HasDifferentatedKeywords = false;
    public List<GwaKeyword> LineKeywords = new List<GwaKeyword>();
    public List<Type> LineParserTypes = new List<Type>();
    public List<Type> LineSchemaTypes = new List<Type>();

    internal bool ContainsKeyword(GwaKeyword kw) => (TableKeyword == kw || LineKeywords.Any(lkw => lkw == kw));

    internal bool ContainsSchemaType(Type t) => (HasDifferentatedKeywords) ? LineSchemaTypes.Contains(t) : (TableSchemaType == t);

    internal Type GetParserType(Type schemaType)
    {
      if (ContainsSchemaType(schemaType))
      {
        if (HasDifferentatedKeywords)
        {
          return LineParserTypes[LineSchemaTypes.IndexOf(schemaType)];
        }
        else if (TableSchemaType == schemaType)
        {
          return TableParserType;
        }
      }
      return null;
    }

    internal Type GetSchemaType(GwaKeyword kw)
    {
      if (ContainsKeyword(kw))
      {
        if (HasDifferentatedKeywords)
        {
          return LineSchemaTypes[LineKeywords.IndexOf(kw)];
        }
        else if (TableKeyword == kw)
        {
          return TableSchemaType;
        }
      }
      return null;
    }
  }
}
