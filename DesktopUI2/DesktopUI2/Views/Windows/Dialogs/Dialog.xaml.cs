using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Material.Dialog.Icons;
using Material.Icons.Avalonia;

namespace DesktopUI2.Views.Windows.Dialogs
{
  public partial class Dialog : DialogUserControl
  {
    public Dialog()
    {
      AvaloniaXamlLoader.Load(this);
    }
    public Dialog(string title, string message, DialogIconKind icon)
    {
      AvaloniaXamlLoader.Load(this);
      var TitleBox = this.FindControl<TextBlock>("Title");
      var MessageBox = this.FindControl<TextBlock>("Message");
      var IconControl = this.FindControl<MaterialIcon>("Icon");


      TitleBox.Text = title;
      MessageBox.Text = message;
      switch (icon)
      {

        case DialogIconKind.Error:
          IconControl.Kind = Material.Icons.MaterialIconKind.Error;
          break;
        case DialogIconKind.Info:
          IconControl.Kind = Material.Icons.MaterialIconKind.Information;
          break;
        case DialogIconKind.Help:
          IconControl.Kind = Material.Icons.MaterialIconKind.Help;
          break;
        case DialogIconKind.Success:
          IconControl.Kind = Material.Icons.MaterialIconKind.HandOkay;
          break;
        default:
          IconControl.Kind = Material.Icons.MaterialIconKind.QuestionMark;
          break;

      }

    }

    public void Close_Click(object sender, RoutedEventArgs e)
    {
      this.Close(null);
    }
  }
}
