using Speckle.GSA.API.GwaSchema;
using System;


namespace Speckle.ConnectorGSA.Proxy.GwaParsers
{
  [AttributeUsage(AttributeTargets.Class)]
  public class GsaType : Attribute
  {
    //This is the keyword used in Gwa GET commands, which is not necessarily the keyword of the records themselves.
    //The keyword used for records is identical to that used for GET commands except for all except the LOAD_BEAM_x set.  
    //To get all LOAD_BEAM_POINT, LOAD_BEAM_UDL, LOAD_BEAM_LINE, LOAD_BEAM_PATCH and LOAD_BEAM_TRILIN records, which all share the same
    //table (i.e. index sequence): call GET LOAD_BEAM (this will get them all)
    public GwaKeyword Keyword { get; protected set; }
    public GwaSetCommandType SetCommandType { get; protected set; }
    //If the object is a child or contained within another object, then this would be false (e.g.: SECTION_COMP in SECTION)
    //Only self-contained keywords are the subject of a GET_ALL call to populate the cache.
    public bool SelfContained { get; protected set; }
    public bool AnalysisLayer { get; protected set; }
    public bool DesignLayer { get; protected set; }
    //These are keywords to use regardless of the layer - because keywords not of the layer in question will be filtered out by the app
    //This stays true to the actual schema, where references to entities of both layers is possible to be used here
    public GwaKeyword[] ReferencedKeywords { get; protected set; }

    public GsaType(GwaKeyword keyword, GwaSetCommandType setCommandType, bool selfContained, bool designLayer, bool analysisLayer, params GwaKeyword[] referencedKeywords)
    {
      this.Keyword = keyword;
      this.SetCommandType = setCommandType;
      this.AnalysisLayer = analysisLayer;
      this.DesignLayer = designLayer;
      this.ReferencedKeywords = referencedKeywords;
      this.SelfContained = selfContained;
    }

    public GsaType(GwaKeyword keyword, GwaSetCommandType setCommandType, bool selfContained, params GwaKeyword[] referencedKeywords)
    {
      this.Keyword = keyword;
      this.SetCommandType = setCommandType;
      this.AnalysisLayer = true;
      this.DesignLayer = true;
      this.ReferencedKeywords = referencedKeywords == null ? new GwaKeyword[0] : referencedKeywords;
      this.SelfContained = selfContained;
    }
  }

  //Only used where there are more than one keyword/class that share the same GSA table - i.e. the LOAD_BEAM ones
  [AttributeUsage(AttributeTargets.Class)]
  public class GsaChildType : Attribute
  {
    public GwaKeyword Keyword { get; protected set; }

    //Used in reflection
    public static string GsaSchemaTypeProperty = "GsaSchemaType"; 
    public static string GwaKeywordProperty = "Keyword";

    public Type GsaSchemaType { get; protected set; }

    public GsaChildType(GwaKeyword kw, Type t)
    {
      this.GsaSchemaType = t;
      this.Keyword = kw;
    }
  }
}
