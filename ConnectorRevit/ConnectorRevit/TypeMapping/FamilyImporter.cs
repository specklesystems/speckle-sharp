using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using DesktopUI2.ViewModels;
using DesktopUI2.Views.Windows.Dialogs;
using Revit.Async;
using static DesktopUI2.ViewModels.ImportFamiliesDialogViewModel;
using Speckle.Core.Logging;
using System.Threading.Tasks;
using System.Xml;
using System.Windows.Forms;
using Speckle.Core.Models;
using RevitSharedResources.Interfaces;
using DynamicData;
using DesktopUI2.Models.TypeMappingOnReceive;
using Avalonia.Threading;

namespace ConnectorRevit.TypeMapping
{
  internal class FamilyImporter
  {
    private readonly Document document;

    public FamilyImporter(Document document)
    {
      this.document = document;
    }

    /// <summary>
    /// Imports new family types into Revit
    /// </summary>
    /// <param name="hostTypesDict"></param>
    /// <returns>
    /// New host types dictionary with newly imported types added (if applicable)
    /// </returns>
    public async Task ImportFamilyTypes(HostTypeAsStringContainer hostTypesContainer, IRevitElementTypeRetriever<ElementType, BuiltInCategory> typeRetriever)
    {
      var familyPaths = await Dispatcher.UIThread.InvokeAsync<string[]>(() => {
        using var windowsDialog = new OpenFileDialog
        {
          Title = "Choose Revit Families",
          Filter = "Revit Families (*.rfa)|*.rfa",
          Multiselect = true
        };
        var _ = windowsDialog.ShowDialog();
        return windowsDialog.FileNames;
      }).ConfigureAwait(false);
      //var windowsDialog = new OpenFileDialog
      //{
      //  Title = "Choose Revit Families",
      //  Filter = "Revit Families (*.rfa)|*.rfa",
      //  Multiselect = true
      //};
      //var result = windowsDialog.ShowDialog();

      //if (result == DialogResult.Cancel)
      //{
      //  return;
      //}

      var allSymbols = new Dictionary<string, List<Symbol>>();
      var familyInfo = new Dictionary<string, FamilyInfo>();

      foreach (var path in familyPaths)
      {
        var xmlPath = path.Replace(".rfa", ".xml");
        string pathClone = string.Copy(path);

        //open family file as xml to extract all family symbols without loading all of them into the project
        await RevitTask.RunAsync(() => document.Application.ExtractPartAtomFromFamilyFile(path, xmlPath))
          .ConfigureAwait(false);
        var xmlDoc = new XmlDocument(); // Create an XML document object
        xmlDoc.Load(xmlPath);

        var nsman = new XmlNamespaceManager(xmlDoc.NameTable);
        nsman.AddNamespace("ab", "http://www.w3.org/2005/Atom");

        string familyName = pathClone.Split('\\').LastOrDefault().Split('.').FirstOrDefault();
        if (string.IsNullOrEmpty(familyName))
          continue;

        var typeInfo = GetTypeInfo(xmlDoc, nsman, typeRetriever);
        familyInfo.Add(familyName, new FamilyInfo(path, typeInfo.CategoryName));

        var elementTypes = typeRetriever.GetAndCacheAvailibleTypes(typeInfo);
        AddSymbolToAllSymbols(allSymbols, xmlDoc, nsman, familyName, elementTypes);

        // delete the newly created xml file
        try
        {
          System.IO.File.Delete(xmlPath);
        }
        catch (Exception ex)
        { }
      }

      //close current dialog body
      MainViewModel.CloseDialog();

      var vm = new ImportFamiliesDialogViewModel(allSymbols);
      //var importFamilies = new ImportFamiliesDialog
      //{
      //  DataContext = vm
      //};

      //await importFamilies.ShowDialog<object>();

      await Dispatcher.UIThread.InvokeAsync(async () => {
        var importFamilies = new ImportFamiliesDialog
        {
          DataContext = vm
        };
        await importFamilies.ShowDialog().ConfigureAwait(true);
      }).ConfigureAwait(false);

      if (vm.selectedFamilySymbols.Count == 0)
      {
        //close current dialog body
        MainViewModel.CloseDialog();
        return;
      }

      await RevitTask.RunAsync(_ =>
      {
        using var t = new Transaction(document, $"Import family types");

        t.Start();
        var symbolsToLoad = new Dictionary<string, List<string>>();
        foreach (var symbol in vm.selectedFamilySymbols)
        {
          bool successfullyImported = document.LoadFamilySymbol(familyInfo[symbol.FamilyName].Path, symbol.Name);
          if (successfullyImported)
          {
            if (!symbolsToLoad.TryGetValue(symbol.FamilyName, out var symbolsOfFamily))
            {
              symbolsOfFamily = new List<string>();
              symbolsToLoad.Add(symbol.FamilyName, symbolsOfFamily);
            }
            symbolsOfFamily.Add(symbol.Name);
          }
        }

        if (symbolsToLoad.Count > 0)
        {
          foreach (var kvp in symbolsToLoad)
          {
            hostTypesContainer.AddTypesToCategory(kvp.Key, kvp.Value);
          }
          t.Commit();
          Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() {
            { "name", "Mappings Import Families" },
            { "count", vm.selectedFamilySymbols.Count }});
        }
        else
        {
          t.RollBack();
        }
      }).ConfigureAwait(false);

      //close current dialog body
      MainViewModel.CloseDialog();

      return;
    }

    private static void AddSymbolToAllSymbols(Dictionary<string, List<Symbol>> allSymbols, XmlDocument xmlDoc, XmlNamespaceManager nsman, string familyName, IEnumerable<ElementType> elementTypes)
    {
      var familyRoot = xmlDoc.GetElementsByTagName("A:family");
      if (familyRoot.Count != 1)
      {
        // TODO: logging
        return;
      }

      nsman.AddNamespace("A", familyRoot[0].NamespaceURI);
      nsman.AddNamespace("ab", "http://www.w3.org/2005/Atom");
      var familySymbols = familyRoot[0].SelectNodes("A:part/ab:title", nsman);

      if (familySymbols.Count == 0) return;

      if (!allSymbols.TryGetValue(familyName, out var symbols))
      {
        symbols = new List<Symbol>();
        allSymbols[familyName] = symbols;
      }

      var loadedSymbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach (var elementType in elementTypes)
      {
        if (elementType.FamilyName == familyName)
        {
          loadedSymbols.Add(elementType.Name);
        }
      }

      foreach (var symbol in familySymbols)
      {
        if (symbol is not XmlElement el)
        {
          continue;
        }

        var isAlreadyLoaded = loadedSymbols.Contains(el.InnerText);
        symbols.Add(new Symbol(el.InnerText, familyName, isAlreadyLoaded));
      }
    }

    private IElementTypeInfo<BuiltInCategory> GetTypeInfo(XmlDocument xmlDoc, XmlNamespaceManager nsman, IRevitElementTypeRetriever<ElementType, BuiltInCategory> typeRetriever)
    {
      var catRoot = xmlDoc.GetElementsByTagName("category");
      var category = typeRetriever.UndefinedTypeInfo;
      foreach (var node in catRoot)
      {
        if (node is not XmlElement xmlNode) continue;

        var term = xmlNode.SelectSingleNode("ab:term", nsman);
        if (term == null) continue;

        category = typeRetriever.GetRevitTypeInfo(term.InnerText);

        if (category != typeRetriever.UndefinedTypeInfo)
          break;
      }

      return category;
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
  }
}
