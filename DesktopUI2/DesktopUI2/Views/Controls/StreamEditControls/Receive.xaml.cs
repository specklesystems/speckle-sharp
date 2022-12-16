using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DesktopUI2.ViewModels;
using System;

namespace DesktopUI2.Views.Controls.StreamEditControls
{
  public partial class Receive : UserControl
  {
    Viewbox PreviewBox360 { get; set; }
    Image Image360 { get; set; }
    Image ImageBasic { get; set; }
    public Receive()
    {
      InitializeComponent();

      PreviewBox360 = this.FindControl<Viewbox>("PreviewBox360");
      Image360 = this.FindControl<Image>("Image360");
      ImageBasic = this.FindControl<Image>("ImageBasic");

      PreviewBox360.PointerMoved += PreviewBox360_PointerMoved;
      PreviewBox360.PointerEnter += PreviewBox360_PointerEnter;
      PreviewBox360.PointerLeave += PreviewBox360_PointerLeave;

    }



    private void PreviewBox360_PointerMoved(object sender, Avalonia.Input.PointerEventArgs e)
    {


      var mouseX = e.GetPosition(PreviewBox360).X + PreviewBox360.Margin.Left;
      SetMargin(mouseX);
    }

    private void PreviewBox360_PointerEnter(object sender, Avalonia.Input.PointerEventArgs e)
    {
      var svm = PreviewBox360.DataContext as StreamViewModel;
      if (!svm.PreviewImage360Loaded)
        return;
      Image360.Opacity = 1;
      ImageBasic.IsVisible = false;
    }

    private void PreviewBox360_PointerLeave(object sender, Avalonia.Input.PointerEventArgs e)
    {
      Image360.Opacity = 0;
      ImageBasic.IsVisible = true;
    }

    private void SetMargin(double mouseX)
    {
      if (mouseX == 0)
        mouseX = 1;
      var imageWidth = Image360.Bounds.Width == 0 ? 34300 : Image360.Bounds.Width; //34300
      var imageStepWidth = imageWidth / 24.5; //1400 - WHY 24.5??? There seems to be some f* offset at the ends
      var imageOffset = (imageWidth - (imageStepWidth * 24)) / 2;


      var viewboxWidth = PreviewBox360.Bounds.Right;
      var stepWidth = viewboxWidth / 24;


      var index = Math.Abs(Math.Round(mouseX / stepWidth));
      if (index >= 24)
        index = 24 - 1;

      var centerOffset = (imageStepWidth - viewboxWidth) / 2; //since the image fram is wider, let's center the model

      var offset = (-1 * index * imageStepWidth) - imageOffset - centerOffset;
      PreviewBox360.Margin = new Avalonia.Thickness(offset, 0, 0, 0);
    }

    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);
    }
  }
}
