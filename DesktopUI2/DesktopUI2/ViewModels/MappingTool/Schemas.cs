using Avalonia.Metadata;
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
  [DataContract]
  public abstract class Schema : ReactiveObject
  {
    [DataMember]
    public abstract string Name { get; }
    [DataMember]
    public bool HasData;

    //only used to highlight/clear mapped elements
    //needs to be set by the GetExistingSchemaElements method

    public string ApplicationId;

    public abstract string Summary { get; }
    public abstract string GetSerializedSchema();

    public abstract bool IsValid { get; }

    public string GetSerializedViewModel()
    {
      HasData = true;
      //needed bc we're serializing an interface
      var settings = new JsonSerializerSettings()
      {
        TypeNameHandling = TypeNameHandling.All
      };
      var serializedViewModel = JsonConvert.SerializeObject(this, settings);
      return serializedViewModel;
    }

  }

  /// <summary>
  /// Basic Revit Element with Family, Type and Level properties
  /// </summary>
  public class RevitBasicViewModel : Schema
  {
    public override string Name => "";

    private List<RevitFamily> _families;
    public List<RevitFamily> Families
    {
      get => _families;
      set
      {

        this.RaiseAndSetIfChanged(ref _families, value);

        //force the refresh of the dropdown after these lists have been updated
        SelectedFamily = Families?.FirstOrDefault(x => x.Name == SelectedFamily?.Name);
        SelectedType = SelectedFamily?.Types?.FirstOrDefault(x => x == SelectedType);
      }
    }

    private RevitFamily _selectedFamily;
    [DataMember]
    public RevitFamily SelectedFamily
    {
      get => _selectedFamily;
      set
      {
        this.RaiseAndSetIfChanged(ref _selectedFamily, value);
        this.RaisePropertyChanged(nameof(IsValid));
      }
    }

    private string _selectedtype;
    [DataMember]
    public string SelectedType
    {
      get => _selectedtype;
      set
      {
        this.RaiseAndSetIfChanged(ref _selectedtype, value);
        this.RaisePropertyChanged(nameof(IsValid));
      }
    }

    private List<string> _levels;
    public List<string> Levels
    {
      get => _levels;
      set
      {
        this.RaiseAndSetIfChanged(ref _levels, value);

        //force the refresh of the dropdown after these lists have been updated
        SelectedLevel = Levels?.FirstOrDefault(x => x == SelectedLevel);
      }
    }


    private string _selectedLevel;
    [DataMember]
    public string SelectedLevel
    {
      get => _selectedLevel;
      set
      {
        this.RaiseAndSetIfChanged(ref _selectedLevel, value);
        this.RaisePropertyChanged(nameof(IsValid));
      }
    }

    public override string Summary => $"{SelectedFamily?.Name} - {SelectedType}";


    public RevitBasicViewModel()
    {
    }

    public override string GetSerializedSchema()
    {
      return "";
    }

    public override bool IsValid
    {
      get
      {
        return
          SelectedFamily != null &&
          !string.IsNullOrEmpty(SelectedType) &&
          !string.IsNullOrEmpty(SelectedLevel);
      }
    }
  }

  /// <summary>
  /// Revit MEP view model with dimension and system properties
  /// </summary>
  public class RevitMEPViewModel : RevitBasicViewModel
  {
    private double _selectedHeight;
    [DataMember]
    public double SelectedHeight
    {
      get => _selectedHeight;
      set
      {
        this.RaiseAndSetIfChanged(ref _selectedHeight, value);
        this.RaisePropertyChanged(nameof(IsValid));
      }
    }

    private double _selectedWidth;
    [DataMember]
    public double SelectedWidth
    {
      get => _selectedWidth;
      set
      {
        this.RaiseAndSetIfChanged(ref _selectedWidth, value);
        this.RaisePropertyChanged(nameof(IsValid));
      }
    }

    private double _selectedDiameter;
    [DataMember]
    public double SelectedDiameter
    {
      get => _selectedDiameter;
      set
      {
        this.RaiseAndSetIfChanged(ref _selectedDiameter, value);
        this.RaisePropertyChanged(nameof(IsValid));
      }
    }

    public RevitMEPViewModel()
    {
    }

    public override bool IsValid
    {
      get
      {
        return
          SelectedFamily != null &&
          !string.IsNullOrEmpty(SelectedType) &&
          !string.IsNullOrEmpty(SelectedLevel) &&
          ((SelectedHeight > 0 && SelectedWidth > 0) || SelectedDiameter > 0);
      }
    }
  }

  public class RevitWallViewModel : RevitBasicViewModel
  {
    public override string Name => "Wall";


    public override string GetSerializedSchema()
    {
      var obj = new RevitWall(SelectedFamily.Name, SelectedType, null, new RevitLevel(SelectedLevel), 0);
      return Operations.Serialize(obj);
    }
  }

  public class RevitFloorViewModel : RevitBasicViewModel
  {
    public override string Name => "Floor";

    public override string GetSerializedSchema()
    {
      var obj = new RevitFloor(SelectedFamily.Name, SelectedType, null, new RevitLevel(SelectedLevel), false, 0, null, null);
      return Operations.Serialize(obj);
    }
  }

  public class RevitBeamViewModel : RevitBasicViewModel
  {
    public override string Name => "Beam";

    public override string GetSerializedSchema()
    {
      var obj = new RevitBeam(SelectedFamily.Name, SelectedType, null, new RevitLevel(SelectedLevel));
      return Operations.Serialize(obj);
    }
  }

  public class RevitBraceViewModel : RevitBasicViewModel
  {
    public override string Name => "Brace";

    public override string GetSerializedSchema()
    {
      var obj = new RevitBrace(SelectedFamily.Name, SelectedType, null, new RevitLevel(SelectedLevel));
      return Operations.Serialize(obj);
    }
  }

  public class RevitColumnViewModel : RevitBasicViewModel
  {
    public override string Name => "Column";

    public override string GetSerializedSchema()
    {
      var obj = new RevitColumn(SelectedFamily.Name, SelectedType, null, new RevitLevel(SelectedLevel), false);
      return Operations.Serialize(obj);
    }
  }

  public class RevitPipeViewModel : RevitMEPViewModel
  {
    public override string Name => "Pipe";

    public override string GetSerializedSchema()
    {
      var obj = new RevitPipe(SelectedFamily.Name, SelectedType, null, SelectedDiameter, new RevitLevel(SelectedLevel));
      return Operations.Serialize(obj);
    }
  }

  public class RevitDuctViewModel : RevitMEPViewModel
  {
    public override string Name => "Duct";

    public override string GetSerializedSchema()
    {
      var obj = new RevitDuct(SelectedFamily.Name, SelectedType, null, null, null, new RevitLevel(SelectedLevel), SelectedWidth, SelectedHeight, SelectedDiameter);
      return Operations.Serialize(obj);
    }
  }

  public class RevitTopographyViewModel : Schema
  {
    public override string Name => "Topography";

    public override string Summary => $"Topography";

    public override string GetSerializedSchema()
    {
      var obj = new RevitTopography();
      return Operations.Serialize(obj);
    }

    public override bool IsValid => true;
  }

  public class RevitFamilyInstanceViewModel : RevitBasicViewModel
  {
    public override string Name => "Family Instance";

    public override string GetSerializedSchema()
    {
      var obj = new FamilyInstance(null, SelectedFamily.Name, SelectedType, new RevitLevel(SelectedLevel));
      return Operations.Serialize(obj);
    }
  }

  public class DirectShapeFreeformViewModel : Schema
  {
    public override string Name => "DirectShape";

    private string _shapeName;
    [DataMember]
    public string ShapeName
    {
      get => _shapeName;
      set
      {
        this.RaiseAndSetIfChanged(ref _shapeName, value);
        this.RaisePropertyChanged(nameof(IsValid));
      }
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
      set
      {
        this.RaiseAndSetIfChanged(ref _selectedCategory, value);
        this.RaisePropertyChanged(nameof(IsValid));
      }
    }

    private bool _freeform;
    [DataMember]
    public bool Freeform
    {
      get => _freeform;
      set => this.RaiseAndSetIfChanged(ref _freeform, value);
    }

    public override string Summary => $"{ShapeName} - {SelectedCategory}";

    public DirectShapeFreeformViewModel()
    {
      Categories = Enum.GetValues(typeof(RevitCategory)).Cast<RevitCategory>().Select(x => x.ToString()).ToList(); ;
    }


    public override string GetSerializedSchema()
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

    public override bool IsValid
    {
      get
      {
        return
          !string.IsNullOrEmpty(ShapeName) &&
          !string.IsNullOrEmpty(SelectedCategory);
      }
    }
  }

  [DataContract]
  public class RevitFamily : ReactiveObject
  {
    [DataMember]
    public string Name { get; set; }

    [DataMember]
    public string Shape { get; set; }

    private List<string> _types;
    public List<string> Types
    {
      get => _types;
      set => this.RaiseAndSetIfChanged(ref _types, value);
    }

    public RevitFamily(string name, List<string> types, string shape = "")
    {
      Name = name;
      Types = types;
      Shape = shape;
    }

    public RevitFamily()
    {
    }
  }


  public class SchemaGroup
  {
    public string Name { get; set; }
    public List<Schema> Schemas { get; set; }

    public SchemaGroup(string name, List<Schema> schemas)
    {
      Name = name;
      Schemas = schemas;
    }
  }
}
