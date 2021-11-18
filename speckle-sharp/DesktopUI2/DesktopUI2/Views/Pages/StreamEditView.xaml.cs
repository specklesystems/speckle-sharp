using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using ReactiveUI;

namespace DesktopUI2.Views.Pages
{
  public class StreamEditView : ReactiveUserControl<StreamEditViewModel>
  {
    public StreamEditView()
    {
      this.WhenActivated(disposables => { });
      AvaloniaXamlLoader.Load(this);
    }


  }
}