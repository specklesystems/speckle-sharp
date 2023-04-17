using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace DesktopUI2.Views.Windows.Dialogs;

public class ChangeRoleDialog : DialogUserControl
{
  private ComboBox RoleField;

  public ChangeRoleDialog()
  {
    InitializeComponent();
  }

  private string Role { get; set; }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
    RoleField = this.FindControl<ComboBox>("role");
  }

  public void Save_Click(object sender, RoutedEventArgs e)
  {
    //too lazy to create a view model for this or properly style the Dialogs
    Role = (RoleField.SelectedItem as ComboBoxItem).Content as string;
    Close(Role);
  }

  public void Close_Click(object sender, RoutedEventArgs e)
  {
    Close(null);
  }
}
