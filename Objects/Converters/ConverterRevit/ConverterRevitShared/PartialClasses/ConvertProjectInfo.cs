using System.Collections.Generic;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using ProjectInfo = Objects.BuiltElements.Revit.ProjectInfo;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  private ProjectInfo ProjectInfoToSpeckle(DB.ProjectInfo revitInfo)
  {
    var speckleInfo = new ProjectInfo
    {
      address = revitInfo.Address,
      author = revitInfo.Author,
      buildingName = revitInfo.BuildingName,
      clientName = revitInfo.ClientName,
      issueDate = revitInfo.IssueDate,
      name = revitInfo.Name,
      number = revitInfo.Number,
      organizationDescription = revitInfo.OrganizationDescription,
      organizationName = revitInfo.OrganizationName,
      status = revitInfo.Status
    };
    Report.Log($"Converted ProjectInfo");

    Base parameterParent = new();
    GetAllRevitParamsAndIds(
      parameterParent,
      revitInfo,
      new List<string>
      {
        // parameters included in the strongly typed properties
        "PROJECT_ADDRESS",
        "PROJECT_AUTHOR",
        "PROJECT_BUILDING_NAME",
        "CLIENT_NAME",
        "PROJECT_ISSUE_DATE",
        "PROJECT_NUMBER",
        "PROJECT_ORGANIZATION_DESCRIPTION",
        "PROJECT_ORGANIZATION_NAME",
        "PROJECT_NUMBER",
        "PROJECT_STATUS",
        // parameters to be excluded entirely
        "ELEM_CATEGORY_PARAM_MT",
        "ELEM_CATEGORY_PARAM",
        "DESIGN_OPTION_ID",
      }
    );

    if (parameterParent["parameters"] is Base parameters)
    {
      Dictionary<string, object> parameterDict = parameters.GetMembers(DynamicBaseMemberType.Dynamic);
      foreach (KeyValuePair<string, object> kvp in parameterDict)
      {
        if (kvp.Value is Parameter param && param.value is not null)
        {
          speckleInfo[GetCleanBasePropName(param.name)] = param.value;
        }
      }
    }

    return speckleInfo;
  }
}
