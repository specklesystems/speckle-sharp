using Speckle.ConnectorTeklaStructures.UI;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tekla.Structures.Model;

namespace Speckle.ConnectorTeklaStructures.Util
{
  class ConnectorTeklaStructuresUtils
  {
#if TeklaStructures2021
    public static string TeklaStructuresAppName = HostApplications.TeklaStructures.GetVersion(HostAppVersion.v2021);
#elif TeklaStructures2020
  public static string TeklaStructuresAppName = HostApplications.TeklaStructures.GetVersion(HostAppVersion.v2020);
#endif

    public static Dictionary<string, (string, string)> ObjectIDsTypesAndNames { get; set; }

    public List<SpeckleException> ConversionErrors { get; set; }

    private static Dictionary<string, ModelObject.ModelObjectEnum> _categories { get; set; }

    public static Dictionary<string, ModelObject.ModelObjectEnum> GetCategories(Model model)
    {
      if (_categories == null)
      {
        _categories = new Dictionary<string, ModelObject.ModelObjectEnum>();
        foreach (var bic in SupportedBuiltInCategories)
        {
          var category = model.GetModelObjectSelector().GetAllObjectsWithType(bic);
          if (category == null)
            continue;
          _categories.Add(bic.ToString(), bic);
        }
      }
      return _categories;
    }


    #region Get List Names

    public static List<string> GetCategoryNames(Model model)
    {
      return GetCategories(model).Keys.OrderBy(x => x).ToList();
    }
    #endregion

    private static List<ModelObject.ModelObjectEnum> SupportedBuiltInCategories = new List<ModelObject.ModelObjectEnum>
    {
      ModelObject.ModelObjectEnum.BEAM
    };
  }
}
