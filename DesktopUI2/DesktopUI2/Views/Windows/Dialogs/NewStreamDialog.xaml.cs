using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DesktopUI2.ViewModels;
using Speckle.Core.Credentials;

namespace DesktopUI2.Views.Windows.Dialogs;

public class NewStreamDialog : DialogUserControl
{
  public NewStreamDialog() { }

  public NewStreamDialog(List<AccountViewModel> accounts)
  {
    InitializeComponent();
    var combo = this.FindControl<ComboBox>("accounts");
    combo.Items = accounts;
    try
    {
      combo.SelectedIndex = accounts.FindIndex(x => x.Account.isDefault);
    }
    catch
    {
      combo.SelectedIndex = 0;
    }
  }

  public Account Account { get; set; }
  public string StreamName { get; set; }
  public string Description { get; set; }
  public bool IsPublic { get; set; }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }

  public void Create_Click(object sender, RoutedEventArgs e)
  {
    var isPublic = this.FindControl<ToggleSwitch>("isPublic").IsChecked;
    //too lazy to create a view model for this or properly style the Dialogs
    Account = (this.FindControl<ComboBox>("accounts").SelectedItem as AccountViewModel).Account;
    StreamName = this.FindControl<TextBox>("name").Text;
    Description = this.FindControl<TextBox>("description").Text;
    IsPublic = isPublic.HasValue ? isPublic.Value : false;
    Close(true);
  }

  public void Close_Click(object sender, RoutedEventArgs e)
  {
    Close(false);
  }
}
