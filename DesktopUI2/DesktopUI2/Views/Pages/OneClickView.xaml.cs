using System;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using ReactiveUI;

namespace DesktopUI2.Views.Pages;

[Obsolete("One click is no longer available")]
public class OneClickView : ReactiveUserControl<OneClickViewModel>
{
  public OneClickView()
  {
    this.WhenActivated(disposables => { });
    AvaloniaXamlLoader.Load(this);
  }
}
