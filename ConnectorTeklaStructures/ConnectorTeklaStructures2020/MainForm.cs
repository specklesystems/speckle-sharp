using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;

using Tekla.Structures.Model;
using Tekla.Structures.Dialog;

using DesktopUI2.ViewModels;
using DesktopUI2.Views;
using Assembly = System.Reflection.Assembly;
using Speckle.ConnectorTeklaStructures.UI;

namespace Speckle.ConnectorTeklaStructures
{
  public partial class MainForm : PluginFormBase
  {
        // Enable inserting of objects in a model
        private readonly Model model;
        public MainForm()
        {
            // Link to model.         
            model = new Model();
            Bindings = new ConnectorBindingsTeklaStructures(model);

            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);
                CreateOrFocusSpeckle();
            }
            catch (Exception ex)
            {

            }
        }

        public Model Model
        {
            get { return model; }
        }
        public static Window MainWindow { get; private set; }
        public static ConnectorBindingsTeklaStructures Bindings { get; set; }

        static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly a = null;
            var name = args.Name.Split(',')[0];
            string path = Path.GetDirectoryName(typeof(MainPlugin).Assembly.Location);

            string assemblyFile = Path.Combine(path, name + ".dll");

            if (File.Exists(assemblyFile))
                a = Assembly.LoadFrom(assemblyFile);

            return a;
        }
        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<DesktopUI2.App>()
            .UsePlatformDetect()
            .With(new SkiaOptions { MaxGpuResourceSizeBytes = 8096000 })
            .With(new Win32PlatformOptions { AllowEglInitialization = true, EnableMultitouch = false })
            .LogToTrace()
            .UseReactiveUI();

        private static void AppMain(Application app, string[] args)
        {
            var viewModel = new MainViewModel(Bindings);
            MainWindow = new DesktopUI2.Views.MainWindow
            {
                DataContext = viewModel
            };

            app.Run(MainWindow);
            //System.Threading.Tasks.Task.Run(() => app.Run(MainWindow));
        }
        public static void CreateOrFocusSpeckle()
        {
            if (MainWindow == null)
            {
                BuildAvaloniaApp().Start(AppMain, null);
            }

            MainWindow.Show();
            MainWindow.Activate();
        }

    }
}
