
using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using ProjectInfo = Objects.BuiltElements.Revit.ProjectInfo;
using RevitElementType = Objects.BuiltElements.Revit.RevitElementType;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    private RevitElementType ElementTypeToSpeckle(DB.ElementType revitType)
    {
      var fs = revitType as FamilySymbol;

      var speckleType = new RevitElementType
      {
        type = revitType.Name,
        family = revitType.FamilyName,
        category = revitType.Category.Name
      };

      if (fs != null && fs.Family != null)
      {
        speckleType.placementType = fs.Family?.FamilyPlacementType.ToString();
        speckleType.hasFamilySymbol = true;
      }

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
