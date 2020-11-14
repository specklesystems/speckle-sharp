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
    public bool HasResult = false;

    private List<string> items = new List<string> { "giallo", "rosso", "blu", "verde" };
    public CreateSchemaObjectDialog()
    {
      Title = "Create an Object by Schema";
      Width = 237;
      Padding = 10;
      Resizable = true;

      search = new SearchBox();
      search.Width = 200;
      search.TextChanged += Search_TextChanged;
      search.Focus();
      search.PlaceholderText = "Search for a schema class";


      list = new ListBox();
      list.Width = 200;
      list.Height = 200;

      for (int i = 0; i < items.Count; i++)
      {
        list.Items.Add(new ListItem { Text = items[i] });
      }


      var layout = new StackLayout();
      layout.Items.Add(search);
      layout.Items.Add(list);

      Content = layout;

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

    private void Search_TextChanged(object sender, EventArgs e)
    {
      list.Items.Clear();
      var filter = new List<string>();

      if (!string.IsNullOrEmpty(search.Text))
        filter = items.Where(x => x.ToLowerInvariant().Contains(search.Text.ToLowerInvariant())).ToList();
      else
        filter = items;


      for (int i = 0; i < filter.Count; i++)
      {
        list.Items.Add(new ListItem { Text = filter[i] });
      }
    }

  }
}
