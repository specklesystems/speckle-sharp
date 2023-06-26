using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Autodesk.Revit.DB;
using Avalonia.Threading;
using DesktopUI2.Models.TypeMappingOnReceive;
using DesktopUI2.ViewModels;
using DesktopUI2.Views.Windows.Dialogs;
using Revit.Async;
using RevitSharedResources.Interfaces;
using Speckle.Core.Logging;
using static DesktopUI2.ViewModels.ImportFamiliesDialogViewModel;

namespace ConnectorRevit.TypeMapping
{
  internal sealed class FamilyImporter
  {
    private readonly Document document;
    private readonly IAllRevitCategoriesExposer revitCategoriesExposer;
    private readonly IRevitElementTypeRetriever typeRetriever;

    public FamilyImporter(Document document, IAllRevitCategoriesExposer revitCategoriesExposer, IRevitElementTypeRetriever typeRetriever)
    {
      this.document = document;
      this.revitCategoriesExposer = revitCategoriesExposer;
      this.typeRetriever = typeRetriever;
    }

    /// <summary>
    /// Imports new family types into Revit
    /// </summary>
    /// <param name="hostTypesDict"></param>
    /// <returns>
    /// New host types dictionary with newly imported types added (if applicable)
    /// </returns>
    public async Task ImportFamilyTypes(HostTypeContainer hostTypesContainer)
    {
      var familyPaths = await Dispatcher.UIThread.InvokeAsync<string[]>(() =>
      {
        using var windowsDialog = new OpenFileDialog
        {
          Title = "Choose Revit Families",
          Filter = "Revit Families (*.rfa)|*.rfa",
          Multiselect = true
        };
        var _ = windowsDialog.ShowDialog();
        return windowsDialog.FileNames;
      }).ConfigureAwait(false);

      if (familyPaths.Length == 0) return;

      var allSymbols = new Dictionary<string, List<Symbol>>();
      var familyInfo = new Dictionary<string, FamilyInfo>();
      await PopulateSymbolAndFamilyInfo(familyPaths, allSymbols, familyInfo).ConfigureAwait(false);

      var vm = new ImportFamiliesDialogViewModel(allSymbols);
      await Dispatcher.UIThread.InvokeAsync(async () =>
      {
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

      await ImportTypesIntoDocument(hostTypesContainer, familyInfo, vm).ConfigureAwait(false);

      return;
    }

    private async Task ImportTypesIntoDocument(HostTypeContainer hostTypesContainer, Dictionary<string, FamilyInfo> familyInfo, ImportFamiliesDialogViewModel vm)
    {
      await RevitTask.RunAsync(_ =>
      {
        using var t = new Transaction(document, $"Import family types");

        t.Start();
        var symbolsToLoad = new Dictionary<string, List<ISingleHostType>>();
        foreach (var symbol in vm.selectedFamilySymbols)
        {
          bool successfullyImported = document.LoadFamilySymbol(familyInfo[symbol.FamilyName].Path, symbol.Name);
          if (successfullyImported)
          {
            if (!symbolsToLoad.TryGetValue(familyInfo[symbol.FamilyName].Category, out var symbolsOfCategory))
            {
              symbolsOfCategory = new List<ISingleHostType>();
              symbolsToLoad.Add(familyInfo[symbol.FamilyName].Category, symbolsOfCategory);
            }
            symbolsOfCategory.Add(new RevitHostType(symbol.FamilyName, symbol.Name));
          }
        }

        if (symbolsToLoad.Count > 0)
        {
          foreach (var kvp in symbolsToLoad)
          {
            hostTypesContainer.AddTypesToCategory(kvp.Key, kvp.Value);
            typeRetriever.InvalidateElementTypeCache(kvp.Key);
          }
          t.Commit();
          Analytics.TrackEvent(Analytics.Events.DUIAction, new Dictionary<string, object>() {
            { "name", "Type Map" },
            { "method", "Import Types" },
            { "count", vm.selectedFamilySymbols.Count }});
        }
        else
        {
          t.RollBack();
        }
      }).ConfigureAwait(false);

      //close current dialog body
      MainViewModel.CloseDialog();
    }

    private async Task PopulateSymbolAndFamilyInfo(string[] familyPaths, Dictionary<string, List<Symbol>> allSymbols, Dictionary<string, FamilyInfo> familyInfo)
    {
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

        var typeInfo = GetTypeInfo(xmlDoc, nsman);
        familyInfo.Add(familyName, new FamilyInfo(path, typeInfo.CategoryName));

        var elementTypes = typeRetriever.GetOrAddAvailibleTypes(typeInfo);
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
    }

    private static void AddSymbolToAllSymbols(Dictionary<string, List<Symbol>> allSymbols, XmlDocument xmlDoc, XmlNamespaceManager nsman, string familyName, IEnumerable<ElementType> elementTypes)
    {
      var familyRoot = xmlDoc.GetElementsByTagName("A:family");
      if (familyRoot.Count != 1)
      {
        throw new SpeckleException($"Incorrect assumption of how the partAtom family format works for family named {familyName}");
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

    private IRevitCategoryInfo GetTypeInfo(XmlDocument xmlDoc, XmlNamespaceManager nsman)
    {
      var catRoot = xmlDoc.GetElementsByTagName("category");
      var category = revitCategoriesExposer.AllCategories.UndefinedCategory;
      foreach (var node in catRoot)
      {
        if (node is not XmlElement xmlNode) continue;

        var term = xmlNode.SelectSingleNode("ab:term", nsman);
        if (term == null) continue;

        category = revitCategoriesExposer.AllCategories.GetRevitCategoryInfo(term.InnerText);

        if (category != revitCategoriesExposer.AllCategories.UndefinedCategory)
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
