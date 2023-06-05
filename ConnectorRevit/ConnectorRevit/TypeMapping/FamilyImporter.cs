using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using DesktopUI2.ViewModels;
using DesktopUI2.Views.Windows.Dialogs;
using Revit.Async;
using static DesktopUI2.ViewModels.ImportFamiliesDialogViewModel;
using System.Threading.Tasks;
using System.Xml;
using System.Windows.Forms;
using Speckle.Core.Models;

namespace ConnectorRevit.TypeMapping
{
  internal class FamilyImporter
  {
    /// <summary>
    /// Imports new family types into Revit
    /// </summary>
    /// <param name="hostTypesDict"></param>
    /// <returns>
    /// New host types dictionary with newly imported types added (if applicable)
    /// </returns>
    public async Task ImportFamilyTypes(HostTypeAsStringContainer hostTypesContainer, Document doc)
    {
      var windowsDialog = new OpenFileDialog();
      windowsDialog.Title = "Choose Revit Families";
      windowsDialog.Filter = "Revit Families (*.rfa)|*.rfa";
      windowsDialog.Multiselect = true;
      var result = windowsDialog.ShowDialog();

      if (result == DialogResult.Cancel)
      {
        return;
      }

      var allSymbols = new Dictionary<string, List<Symbol>>();
      var familyInfo = new Dictionary<string, FamilyInfo>();

      foreach (var path in windowsDialog.FileNames)
      {
        string pathClone = string.Copy(path);

        //open family file as xml to extract all family symbols without loading all of them into the project
        var symbols = new List<string>();
        doc.Application.ExtractPartAtomFromFamilyFile(path, path + ".xml");
        XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
        xmlDoc.Load(path + ".xml");

        XmlNamespaceManager nsman = new XmlNamespaceManager(xmlDoc.NameTable);
        nsman.AddNamespace("ab", "http://www.w3.org/2005/Atom");

        string familyName = pathClone.Split('\\').LastOrDefault().Split('.').FirstOrDefault();
        if (string.IsNullOrEmpty(familyName))
          continue;

        Family match = null;
        var catRoot = xmlDoc.GetElementsByTagName("category");
        var category = TypeCatMisc;

        foreach (var node in catRoot)
        {
          if (node is XmlElement xmlNode)
          {
            var term = xmlNode.SelectSingleNode("ab:term", nsman);
            if (term != null)
            {
              category = GetTypeCategory(term.InnerText);
              if (category == TypeCatMisc)
                continue;

              var filter = GetCustomTypeFilter(category);
              var families = new FilteredElementCollector(CurrentDoc.Document).OfClass(typeof(Family));
              var list = families.ToElements().Cast<Family>().ToList();

              match = list.FirstOrDefault(x => x.Name == familyName && filter.categories.Contains((BuiltInCategory)x.FamilyCategory?.Id.IntegerValue));
              if (match != null)
                break;
            }
          }
        }

        familyInfo.Add(familyName, new FamilyInfo(path, category));

        // see which types have already been loaded into the project from the selected family
        var loadedSymbols = new List<string>();
        if (match != null) //family exists in project
        {
          var symbolIds = match.GetFamilySymbolIds();
          foreach (var id in symbolIds)
          {
            var sym = CurrentDoc.Document.GetElement(id);
            loadedSymbols.Add(sym.Name);
          }
        }

        // get all types from XML document
        XmlNodeList familySymbols;
        try
        {
          var familyRoot = xmlDoc.GetElementsByTagName("A:family");
          if (familyRoot.Count == 1)
          {
            nsman.AddNamespace("A", familyRoot[0].NamespaceURI);
            nsman.AddNamespace("ab", "http://www.w3.org/2005/Atom");
            familySymbols = familyRoot[0].SelectNodes("A:part/ab:title", nsman);
            if (familySymbols.Count > 0)
              allSymbols[familyName] = new List<Symbol>();
            foreach (var symbol in familySymbols)
            {
              if (symbol is XmlElement el)
              {
                if (loadedSymbols.Contains(el.InnerText))
                  allSymbols[familyName].Add(new Symbol(el.InnerText, familyName, true));
                else
                  allSymbols[familyName].Add(new Symbol(el.InnerText, familyName));
              }
            }
          }
        }
        catch (Exception e)
        { }

        // delete the newly created xml file
        try
        {
          System.IO.File.Delete(path + ".xml");
        }
        catch (Exception ex)
        { }
      }

      //close current dialog body
      MainViewModel.CloseDialog();

      var vm = new ImportFamiliesDialogViewModel(allSymbols);
      var importFamilies = new ImportFamiliesDialog
      {
        DataContext = vm
      };

      await importFamilies.ShowDialog<object>();

      if (vm.selectedFamilySymbols.Count == 0)
      {
        //close current dialog body
        MainViewModel.CloseDialog();
        return hostTypesDict;
      }

      var newHostTypes = await RevitTask.RunAsync(app =>
      {
        using (var t = new Transaction(doc, $"Import family types"))
        {
          t.Start();
          bool symbolLoaded = false;

          foreach (var symbol in vm.selectedFamilySymbols)
          {
            bool successfullyImported = doc.LoadFamilySymbol(familyInfo[symbol.FamilyName].Path, symbol.Name);
            if (successfullyImported)
            {
              symbolLoaded = true;
              // add newly imported type to host types dict
              hostTypesContainer.a
              hostTypesDict[familyInfo[symbol.FamilyName].Category].Add(symbol.Name);
            }
          }

          if (symbolLoaded)
          {
            t.Commit();
            Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() {
              { "name", "Mappings Import Families" },
              { "count", vm.selectedFamilySymbols.Count }});
          }

          else
            t.RollBack();
          return hostTypesDict;
        }
      });




      //close current dialog body
      MainViewModel.CloseDialog();

      return newHostTypes;
    }

    public class FamilyInfo
    {
      public string Path { get; set; }
      public string Category { get; set; }
      public FamilyInfo(string path, string category)
      {
        Path = path;
        Category = category;
      }
    }

    /// <summary>
    /// Gets the category of a given base object
    /// </summary>
    /// <param name="obj"></param>
    /// <returns>name of category type as string</returns>
    private string GetTypeCategory(string speckleType)
    {
      switch (speckleType)
      {
        #region General
        case string a when a.Contains("beam"):
        case string b when b.Contains("brace"):
        case string c when c.Contains("framing"):
          return TypeCatFraming;

        case string a when a.Contains("column"):
          return TypeCatColumns;

        case string a when a.Contains("material"):
          return TypeCatMaterials;

        case string a when a.Contains("floor"):
          return TypeCatFloors;

        case string a when a.Contains("wall"):
          return TypeCatWalls;
          #endregion
      }
      return TypeCatMisc;
    }
    private const string TypeCatMaterials = "Materials";
    private const string TypeCatFloors = "Floors";
    private const string TypeCatWalls = "Walls";
    private const string TypeCatFraming = "Framing";
    private const string TypeCatColumns = "Columns";
    private const string TypeCatMisc = "Miscellaneous";
    private List<string> allTypeCategories = new List<string>
    {
      TypeCatColumns,
      TypeCatFloors,
      TypeCatFraming,
      TypeCatMaterials,
      TypeCatMisc,
      TypeCatWalls
    };
  }
}
