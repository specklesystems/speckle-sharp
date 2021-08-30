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

namespace Speckle.ConnectorGSA.Proxy
{
  public class GsaProxy : IGSAProxy
  {
    //Used by the app in ordering the calling of the conversion code
    public List<List<Type>> TxTypeDependencyGenerations { get; private set; } = new List<List<Type>>();
    public List<Type> SchemaTypes { get => ParsersBySchemaType.Keys.ToList(); }

    private Dictionary<Type, Type> ParsersBySchemaType = new Dictionary<Type, Type>();  //Used for writing to the GSA instance
    //private Dictionary<GwaKeyword, Type> ParsersByKeyword = new Dictionary<GwaKeyword, Type>(); //Used for reading from the GSA instance
    private IPairCollection<GwaKeyword, Type> ParsersSchemaType = new PairCollection<GwaKeyword, Type>();
    private bool initialised = false;
    private bool initialisedError = false;  //To ensure initialisation is only attempted once.
    private GSALayer prevLayer;

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

    //Note that When a GET_ALL is called for LOAD_BEAM, it returns LOAD_BEAM_UDL, LOAD_BEAM_LINE, LOAD_BEAM_PATCH and LOAD_BEAM_TRILIN
    public static GwaKeyword[] SetAtKeywords = new GwaKeyword[] 
    {
      GwaKeyword.LOAD_NODE, GwaKeyword.LOAD_BEAM, GwaKeyword.LOAD_GRID_POINT, GwaKeyword.LOAD_GRID_LINE, GwaKeyword.LOAD_2D_FACE, 
      GwaKeyword.LOAD_GRID_AREA, GwaKeyword.LOAD_2D_THERMAL, GwaKeyword.LOAD_GRAVITY, GwaKeyword.INF_BEAM, GwaKeyword.INF_NODE, 
      GwaKeyword.RIGID, GwaKeyword.GEN_REST 
    };

    #region nodeAt_factors
    public static bool NodeAtCalibrated = false;
    //Set to defaults, which will be updated at calibration
    private static readonly Dictionary<string, float> UnitNodeAtFactors = new Dictionary<string, float>();

    public static void CalibrateNodeAt()
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
    private Dictionary<ResultGroup, ResultsProcessorBase> resultProcessors = new Dictionary<ResultGroup, ResultsProcessorBase>();
    private List<ResultType> allResultTypes;

    private List<string> cases = null;

    //This is the factor relative to the SI units (N, m, etc) that the model is currently set to - this is relevant for results as they're always
    //exported to CSV in SI units
    private Dictionary<ResultUnitType, double> unitData = new Dictionary<ResultUnitType, double>();

    private string SpeckleGsaVersion;
    private string units = "m";

    #region File Operations
    /// <summary>
    /// Creates a new GSA file. Email address and server address is needed for logging purposes.
    /// </summary>
    /// <param name="emailAddress">User email address</param>
    /// <param name="serverAddress">Speckle server address</param>
    public bool NewFile(bool showWindow = true, object gsaInstance = null)
    {
      return ExecuteWithLock(() =>
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
      });
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
      ExecuteWithLock(() =>
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

        GSAObject.Open(path);
        FilePath = path;
        GSAObject.SetLocale(Locale.LOC_EN_GB);

        if (showWindow)
        {
          GSAObject.DisplayGsaWindow(true);
        }
      });
      return true;
    }

    public int SaveAs(string filePath) => ExecuteWithLock(() => GSAObject.SaveAs(filePath));

    /// <summary>
    /// Close GSA file.
    /// </summary>
    public void Close()
    {
      ExecuteWithLock(() =>
      {
        try
        {
          GSAObject.Close();
        }
        catch { }
      });
    }
    #endregion

    #region node_resolution
    public int NodeAt(double x, double y, double z, double coincidenceTol)
    {
      float factor = (UnitNodeAtFactors != null && UnitNodeAtFactors.ContainsKey(units)) ? UnitNodeAtFactors[units] : 1;
      //Note: the outcome of this might need to be added to the caches!
      var index = ExecuteWithLock(() => GSAObject.Gen_NodeAt(x * factor, y * factor, z * factor, coincidenceTol * factor));
      return index;
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
            var item = ExecuteWithLock(() =>
            {
              GSAObject.EntitiesInList(pieces[i], (GsaEntity)type, out int[] itemTemp);

              if (itemTemp == null)
              {
                GSAObject.EntitiesInList("\"" + list + "\"", (GsaEntity)type, out itemTemp);
              }
              return itemTemp;
            });

            if (item != null)
            {
              items.AddRange((int[])item);
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
        object result = ExecuteWithLock(() => GSAObject.GwaCommand(string.Join(GwaDelimiter.ToString(), new[] { "GET", "LIST", list })));
        string[] newPieces = ((string)result).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).Select((s, idx) => idx.ToString() + ":" + s).ToArray();

        string res = newPieces.FirstOrDefault();

        string[] pieces = res.Split(GwaDelimiter);

        return ConvertGSAList(pieces[pieces.Length - 1], type);
      }
      catch
      {
        try
        {
          return ExecuteWithLock(() =>
          {
            GSAObject.EntitiesInList("\"" + list + "\"", (GsaEntity)type, out int[] itemTemp);
            return (itemTemp == null) ? new List<int>() : itemTemp.ToList();
          });
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
      if (!initialised && !initialisedError)
      {
        var assembly = GetType().Assembly; //This assembly
        var assemblyTypes = assembly.GetTypes().ToList();

        var gsaBaseType = typeof(GwaParser<GsaRecord>);
        var gsaAttributeType = typeof(GsaType);
        var gwaParserInterfaceType = typeof(IGwaParser);

        var parserTypes = assemblyTypes.Where(t => Helper.InheritsOrImplements(t, gwaParserInterfaceType)
          && t.CustomAttributes.Any(ca => ca.AttributeType == gsaAttributeType)
          && Helper.IsSelfContained(t)
          && ((layer == GSALayer.Design) && Helper.IsDesignLayer(t) || (layer == GSALayer.Analysis) && Helper.IsDesignLayer(t))
          && !t.IsAbstract
          ).ToDictionary(pt => pt, pt => Helper.GetGwaKeyword(pt));

        var layerKeywords = parserTypes.Values.ToList();
        var kwDict = new Dictionary<GwaKeyword, GwaKeyword[]>();
        foreach (var pt in parserTypes.Keys)
        {
          var allRefKw = Helper.GetReferencedKeywords(pt).Where(kw => layerKeywords.Any(k => k == kw)).ToArray();
          kwDict.Add(parserTypes[pt], allRefKw);
        }

        var retCol = new TypeTreeCollection<GwaKeyword>(kwDict.Keys);
        foreach (var kw in kwDict.Keys)
        {
          retCol.Integrate(kw, kwDict[kw]);
        }

        var gens = retCol.Generations();
        if (gens == null || gens.Count == 0)
        {
          return false;
        }

        foreach (var kvp in parserTypes)
        {
          ParsersSchemaType.Add(kvp.Value, kvp.Key);
        }
        ParsersBySchemaType = parserTypes.Keys.ToDictionary(pt => pt.BaseType.GenericTypeArguments.First(), pt => pt);

        TxTypeDependencyGenerations.Clear();
        foreach (var gen in gens)
        {
          var genParserTypes = new List<Type>();

          foreach (var keyword in gen.Where(kw => ParsersSchemaType.ContainsLeft(kw)))
          {
            if (ParsersSchemaType.FindRight(keyword, out Type pt))
            {
              var schemaType = pt.BaseType.GenericTypeArguments.First();
              genParserTypes.Add(schemaType);
            }
          }
          TxTypeDependencyGenerations.Add(genParserTypes);
        }

        initialised = true;
      }
      return (initialised && !initialisedError);
    }
    #endregion

    #region extract_gwa_fns

    public string GenerateApplicationId(Type schemaType, int gsaIndex)
    {
      if (!ParsersSchemaType.ContainsRight(schemaType))
      {
        return "";
      }
      ParsersSchemaType.FindLeft(schemaType, out GwaKeyword kw);

      var appId = "gsa/" + kw + "-" + gsaIndex;
      return appId;
    }

    //Tuple: keyword | index | Application ID | GWA command | Set or Set At
    public bool GetGwaData(bool nodeApplicationIdFilter, out List<GsaRecord> records, IProgress<int> incrementProgress = null)
    {
      GSALayer layer = Instance.GsaModel.Layer;
      if (layer != prevLayer || !initialised)
      {
        if (!Initialise(layer))
        {
          records = null;
          initialisedError = true;
          //Already tried this layer once and it was an error, so don't try again
          return false;
        }
        else
        {
          prevLayer = layer;
        }
        initialised = true;
        initialisedError = false;
      }

      var retRecords = new List<GsaRecord>();
      var dataLock = new object();
      var setKeywords = new List<GwaKeyword>();
      var setAtKeywords = new List<GwaKeyword>();
      var tempKeywordIndexCache = new Dictionary<GwaKeyword, List<int>>();

      //var keywords = ParsersByKeyword.Keys.ToList();
      var keywords = ParsersSchemaType.Lefts;

      foreach (var kw in keywords)
      {
        if (SetAtKeywords.Any(b => kw == b))
        {
          setAtKeywords.Add(kw);
        }
        else
        {
          setKeywords.Add(kw);
        }
      }

      for (int i = 0; i < setKeywords.Count(); i++)
      {
        var newCommand = "GET_ALL" + GwaDelimiter + setKeywords[i];
        var isNode = (setKeywords[i] == GwaKeyword.NODE);
        var isElement = (setKeywords[i] == GwaKeyword.EL);

        string[] gwaLines;

        try
        {
          gwaLines = ExecuteWithLock(() => ((string)GSAObject.GwaCommand(newCommand)).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries));
        }
        catch
        {
          gwaLines = new string[0];
        }

        //TO DO: review if this line is even needed anymore
        if (setKeywords[i] == GwaKeyword.UNIT_DATA)
        {
          continue;
        }

        Parallel.ForEach(gwaLines, gwa =>
        {
          if (ParseGeneralGwa(gwa, out GwaKeyword? keyword, out int? version, out int? index, out string streamId, out string appId, out string gwaWithoutSet, out string keywordAndVersion)
            && keyword.HasValue && ParsersSchemaType.ContainsLeft(keyword.Value))
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
                    streamId = ExecuteWithLock(() => GSAObject.GetSidTagValue(kwStr, index.Value, SID_STRID_TAG));
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
                    appId = ExecuteWithLock(() => GSAObject.GetSidTagValue(kwStr, index.Value, SID_APPID_TAG));
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

              if (!(nodeApplicationIdFilter == true && isNode && string.IsNullOrEmpty(appId)) && ParsersSchemaType.FindRight(keyword.Value, out Type t))
              {
                var parser = (IGwaParser)Activator.CreateInstance(t);
                parser.FromGwa(gwa);
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
            }
          }
        });

        if (incrementProgress != null)
        {
          incrementProgress.Report(1);
        }
      }

      for (int i = 0; i < setAtKeywords.Count(); i++)
      {
        var highestIndex = ExecuteWithLock(() => GSAObject.GwaCommand("HIGHEST" + GwaDelimiter + setAtKeywords[i]));

        for (int j = 1; j <= highestIndex; j++)
        {
          var newCommand = string.Join(GwaDelimiter.ToString(), new[] { "GET", setAtKeywords[i].GetStringValue(), j.ToString() });

          var gwaLine = "";
          try
          {
            gwaLine = (string)ExecuteWithLock(() => GSAObject.GwaCommand(newCommand));
          }
          catch { }

          if (gwaLine != "")
          {
            ParseGeneralGwa(gwaLine, out GwaKeyword? keyword, out int? version, out int? index, out string streamId, out string appId, out string gwaWithoutSet, 
              out string keywordAndVersion);

            if (keyword == setAtKeywords[i] && ParsersSchemaType.FindRight(setAtKeywords[i], out Type t))
            {
              var kwStr = keyword.Value.GetStringValue();
              var originalSid = "";
              if (string.IsNullOrEmpty(streamId))
              {
                try
                {
                  streamId = ExecuteWithLock(() => GSAObject.GetSidTagValue(kwStr, j, SID_STRID_TAG));
                }
                catch { }
              }
              else
              {
                originalSid += FormatStreamIdSidTag(streamId);
              }
              if (string.IsNullOrEmpty(appId))
              {
                appId = ExecuteWithLock(() => GSAObject.GetSidTagValue(kwStr, j, SID_APPID_TAG));
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

              var parser = (IGwaParser)Activator.CreateInstance(t);
              parser.FromGwa(gwaLine);
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
          }
        }
        if (incrementProgress != null)
        {
          incrementProgress.Report(1);
        }
      }
      records = retRecords;
      return true; 
    }

    private string FormatApplicationId(string keyword, int index, string applicationId)
    {
      //It has been observed that sometimes GET commands don't include the SID despite there being one.  For some (but not all)
      //of these instances, the SID is available through an explicit call for the SID, so try that next
      return (string.IsNullOrEmpty(applicationId)) ? ExecuteWithLock(() => GSAObject.GetSidTagValue(keyword, index, SID_APPID_TAG)) : applicationId;
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
        gwaLines = ExecuteWithLock(() => ((string)GSAObject.GwaCommand(newCommand)).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries));
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

    //Assumed to be the full SET or SET_AT command
    public void SetGwa(string gwaCommand) => ExecuteWithLock(() => batchSetGwa.Add(gwaCommand));

    public void Sync()
    {
      if (batchBlankGwa.Count() > 0)
      {
        var batchBlankCommand = ExecuteWithLock(() => string.Join("\r\n", batchBlankGwa));
        var blankCommandResult = ExecuteWithLock(() => GSAObject.GwaCommand(batchBlankCommand));
        ExecuteWithLock(() => batchBlankGwa.Clear());
      }

      if (batchSetGwa.Count() > 0)
      {
        var batchSetCommand = ExecuteWithLock(() => string.Join("\r\n", batchSetGwa));
        var setCommandResult = ExecuteWithLock(() => GSAObject.GwaCommand(batchSetCommand));
        ExecuteWithLock(() => batchSetGwa.Clear());
      }
    }

    

    public string GetGwaForNode(int index)
    {
      var gwaCommand = string.Join(GwaDelimiter.ToString(), new[] { "GET", "NODE.3", index.ToString() });
      return (string)ExecuteWithLock(() => GSAObject.GwaCommand(gwaCommand));
    }

    public string SetSid(string gwa, string streamId, string applicationId)
    {
      ParseGeneralGwa(gwa, out GwaKeyword? keyword, out int? version, out int? index, out string foundStreamId, out string appId, 
        out string gwaWithoutSet, out string keywordAndVersion);

      var streamIdToWrite = (string.IsNullOrEmpty(streamId) ? foundStreamId : streamId);
      var applicationIdToWrite = (string.IsNullOrEmpty(applicationId) ? appId : applicationId);

      if (keyword.HasValue)
      {
        var kwStr = keyword.Value.GetStringValue();
        if (!string.IsNullOrEmpty(streamIdToWrite))
        {
          ExecuteWithLock(() => GSAObject.WriteSidTagValue(kwStr, index.Value, SID_STRID_TAG, streamIdToWrite));
        }
        if (!string.IsNullOrEmpty(applicationIdToWrite))
        {
          ExecuteWithLock(() => GSAObject.WriteSidTagValue(kwStr, index.Value, SID_APPID_TAG, applicationIdToWrite));
        }

        var newSid = FormatStreamIdSidTag(streamIdToWrite) + FormatApplicationIdSidTag(applicationIdToWrite);
        if (!string.IsNullOrEmpty(foundStreamId) || !string.IsNullOrEmpty(appId))
        {
          var originalSid = FormatStreamIdSidTag(foundStreamId) + FormatApplicationIdSidTag(appId);
          gwa = gwa.Replace(originalSid, newSid);
        }
        else
        {
          gwa = gwa.Replace(keywordAndVersion, keywordAndVersion + ":" + newSid);
        }

        return gwa;
      }
      return "";
    }

    /// <summary>
    /// Update GSA case and task links. This should be called at the end of changes.
    /// </summary>
    public bool UpdateCasesAndTasks()
    {
      try
      {
        ExecuteWithLock(() => GSAObject.ReindexCasesAndTasks());
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
      ExecuteWithLock(() =>
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
      });
    }

    #endregion

    #region Document Properties

    public string GetTopLevelSid()
    {
      string sid = "";
      try
      {
        sid = (string)ExecuteWithLock(() => GSAObject.GwaCommand("GET" + GwaDelimiter + "SID"));
      }
      catch
      {
        //File doesn't have SID
      }
      return sid;
    }

    public bool SetTopLevelSid(string sidRecord)
    {
      try
      {
        ExecuteWithLock(() => GSAObject.GwaCommand(string.Join(GwaDelimiter.ToString(), new[] { "SET", "SID", sidRecord })));
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
      string res = (string)ExecuteWithLock(() => GSAObject.GwaCommand("GET" + GwaDelimiter + "TITLE"));

      string[] pieces = res.ListSplit(GwaDelimiter);

      return pieces.Length > 1 ? pieces[1] : "My GSA Model";
    }

    public string[] GetTolerances()
    {
      return ((string)ExecuteWithLock(() => GSAObject.GwaCommand("GET" + GwaDelimiter + "TOL"))).ListSplit(GwaDelimiter);
    }

    /// <summary>
    /// Updates the GSA unit stored in SpeckleGSA.
    /// </summary>
    public string GetUnits()
    {
      var retrievedUnits = ((string)ExecuteWithLock(() => GSAObject.GwaCommand(string.Join(GwaDelimiter.ToString(), new[] { "GET", "UNIT_DATA.1", "LENGTH" })))).ListSplit(GwaDelimiter)[2];
      this.units = retrievedUnits;
      return retrievedUnits;
    }

    public bool SetUnits(string units)
    {
      this.units = units;
      var retCode = ExecuteWithLock(() => GSAObject.GwaCommand(string.Join(GwaDelimiter.ToString(), new[] { "SET", "UNIT_DATA", "LENGTH", units })));
      retCode = ExecuteWithLock(() => GSAObject.GwaCommand(string.Join(GwaDelimiter.ToString(), new[] { "SET", "UNIT_DATA", "DISP", units })));
      retCode = ExecuteWithLock(() => GSAObject.GwaCommand(string.Join(GwaDelimiter.ToString(), new[] { "SET", "UNIT_DATA", "SECTION", units })));
      //Apparently 1 seems to be the code for success, from observation
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
        ExecuteWithLock(() => GSAObject.UpdateViews());
        return true;
      }
      catch
      {
        return false;
      }
    }

    #region results

    public bool PrepareResults(List<ResultType> resultTypes, int numBeamPoints = 3)
    {
      this.resultDir = Path.Combine(Environment.CurrentDirectory, "GSAExport");
      this.allResultTypes = resultTypes;

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
      resultProcessors[group].LoadFromFile(out numErrorRows);
      return true;
    }

    public bool GetResultHierarchy(ResultGroup group, int index, out Dictionary<string, Dictionary<string, object>> valueHierarchy, int dimension = 1)
    {
      valueHierarchy = (resultProcessors.ContainsKey(group)) ? resultProcessors[group].GetResultHierarchy(index) : new Dictionary<string, Dictionary<string, object>>();
      return (valueHierarchy != null && valueHierarchy.Count > 0);
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

    #region lock-related
    private readonly object syncLock = new object();
    protected T ExecuteWithLock<T>(Func<T> f)
    {
      lock (syncLock)
      {
        return f();
      }
    }

    protected void ExecuteWithLock(Action a)
    {
      lock (syncLock)
      {
        a();
      }
    }
    #endregion

    #region static_fns
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
}
