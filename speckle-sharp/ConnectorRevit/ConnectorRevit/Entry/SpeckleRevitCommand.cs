using System;
using System.IO;
using System.Reflection;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Speckle.ConnectorRevit.UI;
using Speckle.DesktopUI;
using Stylet.Xaml;

namespace Speckle.ConnectorRevit.Entry
{
  [Transaction(TransactionMode.Manual)]
  public class SpeckleRevitCommand : IExternalCommand
  {
    public static Bootstrapper Bootstrapper { get; set; }
    public static ConnectorBindingsRevit Bindings { get; set; }

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
      OpenOrFocusSpeckle(commandData.Application);
      return Result.Succeeded;
    }

    public static void OpenOrFocusSpeckle(UIApplication app)
    {
      try
      {
        AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);

        if (Bootstrapper != null)
        {
          Bootstrapper.ShowRootView();
          return;
        }

        Bootstrapper = new Bootstrapper() { Bindings = Bindings };

        if (Application.Current != null)
          new StyletAppLoader() { Bootstrapper = Bootstrapper };
        else
          new DesktopUI.App(Bootstrapper);

        Bootstrapper.Start(Application.Current);
        Bootstrapper.SetParent(app.MainWindowHandle);
      }
      catch (Exception e)
      {
        Bootstrapper = null;
      }
    }

    static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
    {
      Assembly a = null;
      var name = args.Name.Split(',')[0];
      string path = Path.GetDirectoryName(typeof(App).Assembly.Location);

      string assemblyFile = Path.Combine(path, name + ".dll");

      if (File.Exists(assemblyFile))
        a = Assembly.LoadFrom(assemblyFile);

      return a;
    }
  }

}
