using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using System.Linq;

namespace ConnectorGrasshopper
{
  public class CreateSchemaObjectDialog : Dialog
  {
    private ListBox list;
    private SearchBox search;
    private List<Type> types;
    private List<Type> typesFiltered;

    public bool HasResult = false;
    public Type SelectedType = null;

    public CreateSchemaObjectDialog()
    {
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
      list.SelectedIndexChanged += List_SelectedIndexChanged;

      Content = new TableLayout(search, list) { Spacing = new Size(5, 5), Padding = new Padding(10) }; ;

      // buttons
      DefaultButton = new Button { Text = "Create" };
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

    private void List_SelectedIndexChanged(object sender, EventArgs e)
    {
      if (DefaultButton == null)
        return;

      if (list.SelectedIndex == -1)
        DefaultButton.Enabled = false;
      else
      {
        SelectedType = (Type)list.SelectedValue; //TODO: replace with binding... how?
        DefaultButton.Enabled = true;
      }

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

  }
}
