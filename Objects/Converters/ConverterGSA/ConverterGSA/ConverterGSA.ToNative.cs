using Objects.Structural.Geometry;
using Speckle.Core.Models;
using Speckle.GSA.API;
using Speckle.GSA.API.GwaSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConverterGSA
{
  //Container just for ToNative methods, and their helper methods
  public partial class ConverterGSA
  {
    private Dictionary<Type, Func<Base, List<GsaRecord>>> ToNativeFns;

    void SetupToNativeFns()
    {
      ToNativeFns = new Dictionary<Type, Func<Base, List<GsaRecord>>>()
      {
        {
          typeof(Axis), AxisToNative
        }
      };
    }

    #region ToNative
    //TO DO: implement conversion code for ToNative

    private List<GsaRecord> AxisToNative(Base @object)
    {
      var axis = (Axis)@object;

      var index = Instance.GsaModel.Cache.ResolveIndex<GsaAxis>(axis.applicationId);

      return new List<GsaRecord>
      {
        new GsaAxis()
        {
          ApplicationId = axis.applicationId,
          Name = axis.name,
          Index = index,
          OriginX = axis.definition.origin.x,
          OriginY = axis.definition.origin.y,
          OriginZ = axis.definition.origin.z
        }
      };
    }

    #endregion

    #region Helper

    #region ToNative
    #endregion

    #endregion
  }
}
