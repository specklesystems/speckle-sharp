using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DesktopUI2.ViewModels;

namespace DesktopUI2.Views.Windows.Dialogs;

public class AddFromUrlDialog : DialogUserControl
{
  private TextBox UrlField;

  public AddFromUrlDialog(string url)
  {
    Url = url;
    InitializeComponent();
  }

  public AddFromUrlDialog()
  {
    InitializeComponent();
  }

  private string Url { get; set; }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
    this.DataContext = new DialogViewModel();
    UrlField = this.FindControl<TextBox>("url");

    UrlField.Text = Url;
    UrlField.Focus(); //not working :(
  }

  public void Add_Click(object sender, RoutedEventArgs e)
  {
    //too lazy to create a view model for this or properly style the Dialogs
    Url = this.FindControl<TextBox>("url").Text;
    Close(Url);
  }

  public void Close_Click(object sender, RoutedEventArgs e)
  {
    Close(null);
  }
}
