using Avalonia;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using ReactiveUI;
using Speckle.Core.Logging;
using System.Collections.Generic;
using Avalonia.Interactivity;


namespace DesktopUI2.Views.Windows.Dialogs
{
  public partial class ImportFamiliesDialog : ReactiveWindow<ImportFamiliesDialogViewModel>
  {


    public ImportFamiliesDialog()
    {

      this.WhenActivated(disposables => { });
      AvaloniaXamlLoader.Load(this);
      Instance = this;


#if DEBUG
      this.AttachDevTools(KeyGesture.Parse("CTRL+R"));
#endif
    }

    public static ImportFamiliesDialog Instance { get; private set; }

    public void Close_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
    }


    //protected override void OnClosing(CancelEventArgs e)
    //{
    //  this.Hide();
    //  e.Cancel = true;
    //  base.OnClosing(e);
    //}
  }
}