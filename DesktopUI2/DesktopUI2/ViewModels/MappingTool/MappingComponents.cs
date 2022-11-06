using ReactiveUI;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DesktopUI2.ViewModels.MappingTool
{
  public class SchemaType : ReactiveObject
  {
    public string Name { get; set; }
    public string Description { get; set; }
    public ConstructorInfo Ctor { get; set; }

    private List<SchemaProp> _schemaProps = new List<SchemaProp>();

    public List<SchemaProp> SchemaProps
    {
      get => _schemaProps;
      set => this.RaiseAndSetIfChanged(ref _schemaProps, value);
    }

    public SchemaType(string name, string description, ConstructorInfo ctor)
    {
      Name = name;
      Description = description;
      Ctor = ctor;
      GenerateSchemaProps();

    }

    private void GenerateSchemaProps()
    {
      var props = Ctor.GetParameters();
      var schemaProps = new List<SchemaProp>();

      foreach (var param in props)
      {
        if (param.GetCustomAttribute<SchemaMainParam>() != null)
          continue;

        // get property name and value
        Type propType = param.ParameterType;
        if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
          propType = Nullable.GetUnderlyingType(propType);

        object propValue = param;

        var inputDesc = param.GetCustomAttribute<SchemaParamInfo>();
        var d = inputDesc != null ? inputDesc.Description : "";
        if (param.IsOptional)
        {
          if (!string.IsNullOrEmpty(d))
            d += ", ";
          var def = param.DefaultValue == null ? "null" : param.DefaultValue.ToString();
          d += "default = " + def;
        }


        SchemaProp schemaProp = null;

        if (propType == typeof(bool))
          schemaProp = new SchemaPropBool();
        else if (param.Name == "family")
        {
          schemaProp = new SchemaPropList();
          ((SchemaPropList)schemaProp).Options = MappingsViewModel.Instance._revitCategories[0].Types.Select(x => x.family).ToList();
        }
        else if (param.Name == "type")
        {
          schemaProp = new SchemaPropList();
          ((SchemaPropList)schemaProp).Options = MappingsViewModel.Instance._revitCategories[0].Types.Select(x => x.type).ToList();
        }
        else
        {
          schemaProp = new SchemaPropText();
        }




        schemaProp.Name = param.Name;
        schemaProp.Description = d;
        schemaProp.IsOptional = param.IsOptional;

        schemaProps.Add(schemaProp);


        // check if input needs to be a list or item access
        //bool isCollection = typeof(System.Collections.IEnumerable).IsAssignableFrom(propType) &&
        //                    propType != typeof(string) && !propType.Name.ToLower().Contains("dictionary");
        //if (isCollection == true)
        //{
        //  newInputParam.Access = GH_ParamAccess.list;
        //}
        //else
        //{
        //  newInputParam.Access = GH_ParamAccess.item;
        //}


        //add dropdown
        //if (propType.IsEnum)
        //{
        //  //expire solution so that node gets proper size
        //  ExpireSolution(true);

        //  var instance = Activator.CreateInstance(propType);

        //  var vals = Enum.GetValues(propType).Cast<Enum>().Select(x => x.ToString()).ToList();
        //  var options = CreateDropDown(propName, vals, Attributes.Bounds.X, Params.Input[index].Attributes.Bounds.Y);
        //  _document.AddObject(options, false);
        //  Params.Input[index].AddSource(options);
        //}
      }

      SchemaProps = schemaProps;
    }
  }

  public class SchemaProp : ReactiveObject
  {
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsOptional { get; set; }

    public ParameterInfo ParamInfo { get; set; }



  }

  public class SchemaPropText : SchemaProp
  {
    private string _value;
    public string Value
    {
      get => _value;
      set => this.RaiseAndSetIfChanged(ref _value, value);
    }
  }

  public class SchemaPropBool : SchemaProp
  {
    private bool _isChecked;
    public bool IsChecked
    {
      get => _isChecked;
      set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }
  }

  public class SchemaPropList : SchemaProp
  {
    private List<string> _option;
    public List<string> Options
    {
      get => _option;
      set => this.RaiseAndSetIfChanged(ref _option, value);
    }
  }
}
