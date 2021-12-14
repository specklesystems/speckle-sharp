using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using ConnectorRevit;
using DesktopUI2.Models.Settings;
using Speckle.ConnectorRevit.Storage;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using RevitElement = Autodesk.Revit.DB.Element;

namespace Speckle.ConnectorRevit.UI
{
  public partial class ConnectorBindingsRevit2
  {
    //TODO: store these string values in something more solid to avoid typos?
    public override List<ISetting> GetSettings()
    {
      var referencePoints = new List<string>();

      return new List<ISetting>
      {
        new ListBoxSetting() {Slug="all",  Name = "All", Icon = "CubeScan", Description = "Selects all document objects and project information."}
      };
    }

    private List<string> GetReferencePoints()
    {
      if (CurrentDoc == null)
      {
        return new List<string>();
      }

      var selectedObjects = CurrentDoc.Selection.GetElementIds().Select(id => CurrentDoc.Document.GetElement(id).UniqueId).ToList();
      return selectedObjects;
    }
  }
}
