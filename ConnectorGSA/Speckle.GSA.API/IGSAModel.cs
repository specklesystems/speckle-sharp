using Speckle.GSA.API.GwaSchema;
using System.Collections.Generic;

namespace Speckle.GSA.API
{
  public interface IGSAModel
  {
    GSALayer Layer { get; }
    List<int> LookupIndices(GwaKeyword keyword);
    List<int> ConvertGSAList(string list, GSAEntity entityType);
    char GwaDelimiter { get; }
  }

  public enum GSALayer
  {
    Design,
    Analysis
  }
}
