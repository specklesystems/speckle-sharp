using Avalonia.Controls;
using Avalonia.Controls.Templates;
using DesktopUI2.ViewModels;
using System;

namespace DesktopUI2.Views.DataTemplates
{
  public class FilterTemplateSelector : IDataTemplate
  {


    public IControl Build(object data)
    {
      var fvm = data as FilterViewModel;
      var control = (Control)Activator.CreateInstance(fvm.Filter.ViewType);
      control.DataContext = fvm;
      return control;
    }

    public bool Match(object data)
    {
      return data is FilterViewModel;
    }
  }
}
