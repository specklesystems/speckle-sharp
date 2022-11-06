using Objects.BuiltElements.Revit;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace DesktopUI2.ViewModels.MappingTool
{
  public class RevitMetadataViewModel : ReactiveObject
  {
    private string _category;

    public string Category
    {
      get => _category;
      set => this.RaiseAndSetIfChanged(ref _category, value);
    }

    private List<RevitElementType> _types = new List<RevitElementType>();

    public List<RevitElementType> Types
    {
      get => _types;
      set => this.RaiseAndSetIfChanged(ref _types, value);
    }

    public RevitMetadataViewModel()
    {

    }
  }
}
