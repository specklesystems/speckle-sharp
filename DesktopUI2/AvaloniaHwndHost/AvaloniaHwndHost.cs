using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Input;
using Avalonia.Platform;

namespace AvaloniaHwndHost;

public class AvaloniaHwndHost : HwndHost
{
  private EmbeddableControlRoot _root;

  public AvaloniaHwndHost()
  {
    DataContextChanged += AvaloniaHwndHost_DataContextChanged;
  }

  //Switching documents in Revit causes the Panel content to "reset",
  //as a consequence BuildWindowCore gets called again the _root needs to be initialized again
  private EmbeddableControlRoot root
  {
    get
    {
      if (_root == null || _root.Renderer == null)
        _root = new EmbeddableControlRoot(); // TODO: this crashes rhino periodically!
      return _root;
    }
  }

  public Control Content
  {
    get => (Control)root.Content;
    set
    {
      root.Content = value;
      if (value != null)
        value.DataContext = DataContext;
    }
  }

  private void AvaloniaHwndHost_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
  {
    if (Content != null)
      Content.DataContext = e.NewValue;
  }

  protected override HandleRef BuildWindowCore(HandleRef hwndParent)
  {
    root.Prepare();
    root.Renderer.Start();

    var handle = ((IWindowImpl)root.PlatformImpl)?.Handle?.Handle ?? IntPtr.Zero;

    //var wpfWindow = Window.GetWindow(this);
    //var parentHandle = new WindowInteropHelper(wpfWindow).Handle;
    _ = UnmanagedMethods.SetParent(handle, hwndParent.Handle);

    if (IsFocused)
      // Focus Avalonia, if host was focused before handle was created.
      FocusManager.Instance.Focus(null);

    return new HandleRef(root, handle);
  }

  protected override void DestroyWindowCore(HandleRef hwnd)
  {
    root.Dispose();
  }
}
