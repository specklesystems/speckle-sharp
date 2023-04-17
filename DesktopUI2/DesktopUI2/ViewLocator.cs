using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DesktopUI2.ViewModels;

namespace DesktopUI2;

public class ViewLocator : IDataTemplate
{
  public bool SupportsRecycling => false;

  public IControl Build(object data)
  {
    var name = data.GetType().FullName!.Replace("ViewModel", "View");
    var type = Type.GetType(name);

    if (type != null)
      return (Control)Activator.CreateInstance(type)!;
    return new TextBlock { Text = "Not Found: " + name };
  }

  public bool Match(object data)
  {
    return data is ViewModelBase;
  }
}
