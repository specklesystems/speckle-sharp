using Objects.BuiltElements.Revit;
using Objects.Organization;
using ReactiveUI;
using Speckle.Core.Api;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DesktopUI2.ViewModels.MappingTool
{
  public class MappingsViewModel : ViewModelBase, IScreen
  {
    public string TitleFull => "Speckle Mappings";
    public RoutingState Router { get; private set; }

    public MappingsBindings Bindings { get; private set; } = new DummyMappingsBindings();

    internal List<RevitMetadataViewModel> _revitCategories = new List<RevitMetadataViewModel>();

    private List<SchemaType> _tmpSchemaTypes;
    private List<SchemaType> _schemaTypes = new List<SchemaType>();

    public List<SchemaType> SchemaTypes
    {
      get => _schemaTypes;
      set
      {
        this.RaiseAndSetIfChanged(ref _schemaTypes, value);
      }
    }

    private SchemaType _selectedSchemaType;

    public SchemaType SelectedSchemaType
    {
      get => _selectedSchemaType;
      set
      {
        this.RaiseAndSetIfChanged(ref _selectedSchemaType, value);
      }
    }



    //private RevitElementType _selectedRevitType;

    //public RevitElementType SelectedRevitType
    //{
    //  get => _selectedRevitType;
    //  set => this.RaiseAndSetIfChanged(ref _selectedRevitType, value);
    //}

    public static RoutingState RouterInstance { get; private set; }

    public ReactiveCommand<Unit, Unit> GoBack => Router.NavigateBack;

    internal static MappingsViewModel Instance { get; private set; }
    public List<Base> Selection
    {
      get => _selection;
      set
      {
        _selection = value;
        this.RaiseAndSetIfChanged(ref _selection, value);
      }
    }

    private bool _showProgress;
    public bool ShowProgress
    {
      get => _showProgress;
      private set
      {
        this.RaiseAndSetIfChanged(ref _showProgress, value);
      }
    }

    public StreamSelectorViewModel StreamSelector { get; private set; } = new StreamSelectorViewModel();

    private List<Base> _selection;



    public MappingsViewModel()
    {
      Init();
    }

    public MappingsViewModel(MappingsBindings bindings)
    {
      Bindings = bindings;
      Init();
    }


    public void Init()
    {
      Instance = this;
      Router = new RoutingState();

      RouterInstance = Router; // makes the router available app-wide

      Selection = Bindings.GetSelection();




      StreamSelector.ObservableForProperty(x => x.SelectedBranch).Subscribe(x =>
      Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        Task.Run(() => GetCommit().ConfigureAwait(false))
      ));
    }

    private async Task GetCommit()
    {
      try
      {
        if (StreamSelector.SelectedBranch == null || StreamSelector.SelectedBranch.commits == null || StreamSelector.SelectedBranch.commits.items.Count == 0)
          return;

        ShowProgress = true;

        var client = new Client(StreamSelector.SelectedStream.Account);
        string referencedObject = StreamSelector.SelectedBranch.commits.items.FirstOrDefault().referencedObject;

        var transport = new ServerTransport(StreamSelector.SelectedStream.Account, StreamSelector.SelectedStream.Stream.id);
        var model = await Operations.Receive(
            referencedObject,
            transport,
            disposeTransports: true
            ) as Model;

        var types = model["Types"] as Base;

        var mappings = new List<RevitMetadataViewModel>();

        foreach (var baseCategory in types.GetMembers(DynamicBaseMemberType.Instance | DynamicBaseMemberType.Dynamic))
        {
          try
          {
            var categoryName = baseCategory.Key.Replace("@", "");
            var elementTypes = (baseCategory.Value as List<object>).Cast<RevitElementType>().OrderBy(x => x.family + x.type).ToList();
            if (!elementTypes.Any())
              continue;

            mappings.Add(new RevitMetadataViewModel
            {
              Category = categoryName,
              Types = elementTypes
            });

          }
          catch (Exception ex)
          {
            continue;
          }
        }
        //needed to trigger binding refresh
        _revitCategories = mappings;

        _tmpSchemaTypes = new List<SchemaType>();
        //populate schema infos
        foreach (var type in ListAvailableTypes(false))
          RecurseNamespace(type.Namespace.Split('.'), type);

        //needed to trigger binding refresh
        SchemaTypes = _tmpSchemaTypes;
        SelectedSchemaType = SchemaTypes[0];
        //SelectedRevitMapping = RevitMappings[0];
        //SelectedRevitType = SelectedRevitMapping.Types[0];
      }
      catch (Exception e)
      {

      }
      finally
      {
        ShowProgress = false;
      }
    }


    public void SetMappingsCommand()
    {
      Bindings.SetMappings(Selection.Cast<object>().ToList(), "");
    }

    public void RefreshSelectionCommand()
    {
      Selection = Bindings.GetSelection();
    }

    private void RecurseNamespace(string[] ns, Type t)
    {
      if (ns.Length > 1)
      {
        RecurseNamespace(ns.Skip(1).ToArray(), t);
      }
      else
      {
        var temp = GetValidConstr(t, false);
        try
        {
          foreach (var item in temp)
          {
            var info = item.GetCustomAttribute<SchemaInfo>();
            if (info.Category != "Revit")
              continue;

            var name = info.Name.Replace("Revit", "");



            _tmpSchemaTypes.Add(new SchemaType(name, info.Description, item));
          }
        }
        catch (Exception e)
        {
          Console.WriteLine(e);
        }

      }
    }


    //TODO: move to Core? Copy pasted from GrasshopperUtiles
    public static List<Type> ListAvailableTypes(bool includeDeprecated = true)
    {
      // exclude types that don't have any constructors with a SchemaInfo attribute
      return KitManager.Types.Where(
        x => GetValidConstr(x, includeDeprecated).Any()).OrderBy(x => x.Name).ToList();
    }

    public static IEnumerable<ConstructorInfo> GetValidConstr(Type type, bool includeDeprecated = true)
    {

      return type.GetConstructors().Where(y =>
      {
        var hasSchemaInfo = y.GetCustomAttribute<SchemaInfo>() != null;
        var isDeprecated = y.GetCustomAttribute<SchemaDeprecated>() != null;
        return includeDeprecated
          ? hasSchemaInfo
          : hasSchemaInfo && !isDeprecated;
      });
    }

    public static ConstructorInfo FindConstructor(string ConstructorName, string TypeName)
    {
      var type = KitManager.Types.FirstOrDefault(x => x.FullName == TypeName);
      if (type == null)
        return null;

      var constructors = GetValidConstr(type);
      var constructor = constructors.FirstOrDefault(x => MethodFullName(x) == ConstructorName);
      return constructor;
    }

    public static string MethodFullName(MethodBase m)
    {
      var s = m.ReflectedType.FullName;
      if (!m.IsConstructor)
      {
        s += ".";
      }

      s += m.Name;
      if (m.GetParameters().Any())
      {
        //jamie rainfall bug, had to replace + with .
        s += "(" + string.Join(",", m.GetParameters().Select(o => string.Format("{0}", o.ParameterType).Replace("+", ".")).ToArray()) + ")";
      }
      return s;
    }
  }


}

