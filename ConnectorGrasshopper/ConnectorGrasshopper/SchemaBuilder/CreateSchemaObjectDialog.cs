using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using System.Linq;
using System.Reflection;
using Speckle.Core.Kits;

namespace ConnectorGrasshopper
{
  public class CreateSchemaObjectDialog : Dialog
  {
    private ListBox list;
    private SearchBox search;
    private TextArea description;

    private List<Type> types;
    private List<Type> typesFiltered;

    public bool HasResult = false;

    public CSOViewModel model;

    public CreateSchemaObjectDialog()
    {

      model = new CSOViewModel();
      DataContext = model;

      Title = "Create an Object by Schema";
      Padding = 5;
      Resizable = true;

      types = ListAvailableTypes();
      typesFiltered = types;

      search = new SearchBox
      {
        PlaceholderText = "Search for a schema class"
      };
      search.Focus();
      search.TextChanged += Search_TextChanged;


      list = new ListBox
      {
        Size = new Size(200, 200),
        ItemTextBinding = Binding.Property<Type, string>(x => x.Name),
        DataStore = typesFiltered,
        SelectedIndex = 0
      };
      list.SelectedIndexBinding.BindDataContext((CSOViewModel m) => m.SelectedIndex, DualBindingMode.OneWayToSource);
      list.SelectedValueBinding.BindDataContext((CSOViewModel m) => m.SelectedType, DualBindingMode.OneWayToSource);

      description = new TextArea
      {
        ReadOnly = true,
        Size = new Size(200, 200)
      };

      description.TextBinding.BindDataContext(Binding.Property((CSOViewModel m) => m.SelectedType).
        Convert(x => GetDescription(x)), DualBindingMode.OneWay);

      Content = new TableLayout
      {
        Spacing = new Size(5, 5),
        Padding = new Padding(10),
        Rows =
        {
          new TableRow(search),
          new TableRow(list, description)
        }
      };

      // buttons
      DefaultButton = new Button { Text = "Create" };
      DefaultButton.BindDataContext(x => x.Enabled, Binding.Property((CSOViewModel m) => m.SelectedIndex)
        .Convert(x => x > -1), DualBindingMode.OneWay);

      DefaultButton.Click += (sender, e) =>
      {
        HasResult = true;
        Close();
      };
      PositiveButtons.Add(DefaultButton);

      AbortButton = new Button { Text = "C&ancel" };
      AbortButton.Click += (sender, e) => Close();
      NegativeButtons.Add(AbortButton);

    }



    private List<Type> ListAvailableTypes()
    {
      return Speckle.Core.Kits.KitManager.Types.ToList();
    }

    private void Search_TextChanged(object sender, EventArgs e)
    {

      if (!string.IsNullOrEmpty(search.Text))
        typesFiltered = types.Where(x => x.Name.ToLowerInvariant().Contains(search.Text.ToLowerInvariant())).ToList();
      else
        typesFiltered = types;

      list.DataStore = typesFiltered;
    }

    private string GetDescription(Type t)
    {
      if (t == null)
        return "";

      var description = t.FullName;

      var attr = t.GetCustomAttributes().Where(x => x is SchemaDescriptionAttribute).FirstOrDefault();
      if (attr != null)
      {
        description += "\n\n" + ((SchemaDescriptionAttribute)attr).Description;
      }

      return description;
    }


  }

}
