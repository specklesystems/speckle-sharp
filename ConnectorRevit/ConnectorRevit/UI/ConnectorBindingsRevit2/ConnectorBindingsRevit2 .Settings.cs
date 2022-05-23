using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using DesktopUI2.Models.Settings;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit2
  {
    // CAUTION: these strings need to have the same values as in the converter
    const string InternalOrigin = "Internal Origin (default)";
    const string ProjectBase = "Project Base";
    const string Survey = "Survey";

    public override List<ISetting> GetSettings()
    {
      List<string> referencePoints = new List<string>() { InternalOrigin };

      // find project base point and survey point. these don't always have name props, so store them under custom strings
      var basePoint = new FilteredElementCollector(CurrentDoc.Document).OfClass(typeof(BasePoint)).Cast<BasePoint>().Where(o => o.IsShared == false).FirstOrDefault();
      if (basePoint != null)
        referencePoints.Add(ProjectBase);
      var surveyPoint = new FilteredElementCollector(CurrentDoc.Document).OfClass(typeof(BasePoint)).Cast<BasePoint>().Where(o => o.IsShared == true).FirstOrDefault();
      if (surveyPoint != null)
        referencePoints.Add(Survey);

      return new List<ISetting>
      {
        new ListBoxSetting {Slug = "reference-point", Name = "Reference Point", Icon ="LocationSearching", Values = referencePoints, Description = "Sends or receives stream objects in relation to this document point"},
        new CheckBoxSetting {Slug = "linkedmodels-send", Name = "Send Linked Models", Icon ="Link", IsChecked= false, Description = "Include Linked Models in the selection filters when sending"},
        new CheckBoxSetting {Slug = "linkedmodels-receive", Name = "Receive Linked Models", Icon ="Link", IsChecked= false, Description = "Include Linked Models when receiving NOTE: elements from linked models will be received in the current document"}
      };
    }
  }
}
