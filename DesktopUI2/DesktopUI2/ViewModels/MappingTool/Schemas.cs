using Objects.BuiltElements.Revit;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DesktopUI2.ViewModels.MappingTool
{
  public interface ISchema
  {
    abstract string Name { get; }
    abstract string GetSerializedSchema();
  }

  [DataContract]
  public class RevitFamily : ReactiveObject
  {
    [DataMember]
    public string Name { get; set; }

    private List<string> _types;
    public List<string> Types
    {
      get => _types;
      set => this.RaiseAndSetIfChanged(ref _types, value);
    }

    public RevitFamily(string name, List<string> types)
    {
      Name = name;
      Types = types;
    }
  }

  [DataContract]
  public class RevitWallViewModel : ReactiveObject, ISchema
  {
    [DataMember]
    public string Name => "Wall";

    private List<RevitFamily> _families;
    public List<RevitFamily> Families
    {
      get => _families;
      set => this.RaiseAndSetIfChanged(ref _families, value);
    }

    private RevitFamily _selectedFamily;
    [DataMember]
    public RevitFamily SelectedFamily
    {
      get => _selectedFamily;
      set => this.RaiseAndSetIfChanged(ref _selectedFamily, value);
    }

    private string _selectedtype;
    [DataMember]
    public string SelectedType
    {
      get => _selectedtype;
      set => this.RaiseAndSetIfChanged(ref _selectedtype, value);
    }

    private List<string> _levels;
    public List<string> Levels
    {
      get => _levels;
      set => this.RaiseAndSetIfChanged(ref _levels, value);
    }


    private string _selectedLevel;
    [DataMember]
    public string SelectedLevel
    {
      get => _selectedLevel;
      set => this.RaiseAndSetIfChanged(ref _selectedLevel, value);
    }


    public RevitWallViewModel(List<RevitFamily> families, List<string> levels)
    {
      Families = families;
      Levels = levels;
    }

    public RevitWallViewModel()
    {
    }

    public string GetSerializedSchema()
    {
      var obj = new RevitWall(SelectedFamily.Name, SelectedType, null, new RevitLevel(SelectedLevel), 0);
      return Operations.Serialize(obj);
    }
  }

  [DataContract]
  public class DirectShapeFreeformViewModel : ReactiveObject, ISchema
  {
    [DataMember]
    public string Name => "DirectShape";

    private string _shapeName;
    [DataMember]
    public string ShapeName
    {
      get => _shapeName;
      set => this.RaiseAndSetIfChanged(ref _shapeName, value);
    }


    private List<string> _categories;
    public List<string> Categories
    {
      get => _categories;
      set => this.RaiseAndSetIfChanged(ref _categories, value);
    }

    private string _selectedCategory;
    [DataMember]
    public string SelectedCategory
    {
      get => _selectedCategory;
      set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }

    private bool _freeform;
    [DataMember]
    public bool Freeform
    {
      get => _freeform;
      set => this.RaiseAndSetIfChanged(ref _freeform, value);
    }

    public DirectShapeFreeformViewModel()
    {
      Categories = Enum.GetValues(typeof(RevitCategory)).Cast<RevitCategory>().Select(x => x.ToString()).ToList(); ;
    }


    public string GetSerializedSchema()
    {
      //TODO: look into supporting the category and name filed for freeform elements too
      if (Freeform)
        return Operations.Serialize(new FreeformElement());

      var cat = RevitCategory.GenericModels;
      Enum.TryParse(SelectedCategory, out cat);
      var ds = new DirectShape(); //don't use the constructor
      ds.name = ShapeName;
      ds.category = cat;
      return Operations.Serialize(ds);
    }
  }


}
