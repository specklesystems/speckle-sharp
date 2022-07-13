using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using ReactiveUI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DesktopUI2.Views.Windows.Dialogs
{
  public partial class MappingView : ReactiveWindow<MappingViewModel>
  {
    public MappingView()
    {
      this.WhenActivated(disposables => { });
      AvaloniaXamlLoader.Load(this);
      Instance = this;
      //this.WhenActivated(d => d(ViewModel!.BuyMusicCommand.Subscribe(Close)));
    }

    public static MappingView Instance { get; private set; }

    public Dictionary<string,string> Close_Click(object sender, RoutedEventArgs e)
    {
      Close();
      return ViewModel.mapping;
    }
  }
}
