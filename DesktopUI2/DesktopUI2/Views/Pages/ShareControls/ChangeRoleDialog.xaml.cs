using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DesktopUI2.Views.Windows.Dialogs;

namespace DesktopUI2.Views.Pages.ShareControls
{
  public partial class ChangeRoleDialog : DialogUserControl
  {
    public ChangeRoleDialog()
    {
      InitializeComponent();
    }


    private string Role { get; set; }

    ComboBox RoleField;


    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);
      RoleField = this.FindControl<ComboBox>("role");


    }

    public void Save_Click(object sender, RoutedEventArgs e)
    {
      //too lazy to create a view model for this or properly style the Dialogs
      Role = (RoleField.SelectedItem as ComboBoxItem).Content as string;
      this.Close(Role);
    }

    public void Close_Click(object sender, RoutedEventArgs e)
    {
      this.Close(null);
    }
  }
}
