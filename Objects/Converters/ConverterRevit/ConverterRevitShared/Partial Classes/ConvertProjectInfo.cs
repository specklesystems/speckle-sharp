
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using RevitElementType = Objects.BuiltElements.Revit.RevitElementType;
using ProjectInfo = Objects.BuiltElements.Revit.ProjectInfo;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    private RevitElementType ElementTypeToSpeckle(DB.ElementType revitType)
    {
      var speckleType = new RevitElementType
      {
        type = revitType.Name,
        family = revitType.FamilyName,
        category = revitType.Category.Name
      };

      GetAllRevitParamsAndIds(speckleType, revitType);


      return speckleType;
    }

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
      return speckleInfo;
    }
  }
}
