using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSAProxy
{
  public class GsaModel : IGSAModel
  {
    public GSALayer Layer => throw new NotImplementedException();

    public char GwaDelimiter => throw new NotImplementedException();

    public List<int> ConvertGSAList(string list, GSAEntity entityType)
    {
      throw new NotImplementedException();
    }

    public List<int> LookupIndices(GwaKeyword keyword)
    {
      throw new NotImplementedException();
    }
  }
}
