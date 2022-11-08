using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace DesktopUI2.ViewModels.MappingTool
{
  //public class RevitElementType : Base
  //{
  //  public string family { get; set; }
  //  public string type { get; set; }
  //  public string category { get; set; }
  //  public string placementType { get; set; }
  //  public bool hasFamilySymbol { get; set; }

  //}
  public interface ISchema
  {
    abstract string Name { get; set; }
  }


  public class RevitFamily : ReactiveObject
  {
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
    public string Name { get; set; } = "Wall";
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

  }
}
