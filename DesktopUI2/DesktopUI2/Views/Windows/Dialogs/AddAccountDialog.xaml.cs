using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace DesktopUI2.Views.Windows.Dialogs;

public class AddAccountDialog : DialogUserControl
{
  private TextBox UrlField;

  public AddAccountDialog(string url)
  {
    Url = url;
    InitializeComponent();
  }

  public AddAccountDialog()
  {
    InitializeComponent();
  }

  private string Url { get; set; }

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
    Close(Url);
  }

  public void Close_Click(object sender, RoutedEventArgs e)
  {
    Close(null);
  }
}
