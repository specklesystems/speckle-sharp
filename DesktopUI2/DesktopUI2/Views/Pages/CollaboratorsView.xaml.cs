using System;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using DesktopUI2.ViewModels;
using ReactiveUI;
using Speckle.Core.Logging;

namespace DesktopUI2.Views.Pages;

[Obsolete(message: "Collaborators view is not available ")]
public class CollaboratorsView : ReactiveUserControl<CollaboratorsViewModel>
{
  public CollaboratorsView()
  {
    InitializeComponent();
  }

  private void InitializeComponent()
  {
    this.WhenActivated(disposables => { });
    AvaloniaXamlLoader.Load(this);
  }
}
