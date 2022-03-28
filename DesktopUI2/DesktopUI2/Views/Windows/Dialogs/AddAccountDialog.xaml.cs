using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace DesktopUI2.Views.Windows.Dialogs
{
  public partial class AddAccountDialog : Window
  {

    public string Url { get; set; }
    public bool Add = false;

    TextBox UrlField;
    public AddAccountDialog(string url)
    {
      Url = url;
      InitializeComponent();
#if DEBUG
      this.AttachDevTools();
#endif
    }

    public AddAccountDialog()
    {
      InitializeComponent();
    }

    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);
      UrlField = this.FindControl<TextBox>("url");


      UrlField.Text = Url;
      UrlField.Focus(); //not working :(
    }

    public void Add_Click(object sender, RoutedEventArgs e)
    {
      //too lazy to create a view model for this or properly style the Dialogs
      Url = this.FindControl<TextBox>("url").Text;
      Add = true;
      this.Close();
    }

    public void Close_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
    }
  }
}
