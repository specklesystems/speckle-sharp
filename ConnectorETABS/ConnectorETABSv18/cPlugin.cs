using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Speckle.DesktopUI;
using System.Timers;
using System.Windows;
using System.Diagnostics;

using ETABSv1;

using Speckle.Core.Logging;
using Speckle.ConnectorETABS.Util;
using Speckle.ConnectorETABS.UI;
using Objects.Converter.ETABS;


namespace SpeckleConnectorETABS
{
    public class cPlugin
    {
        public static cPluginCallback pluginCallback { get; set; }
        public static Bootstrapper Bootstrapper { get; set; }
        public static bool isSpeckleClosed { get; set; } = false;

        public Timer SelectionTimer;

        public static cSapModel model
 {get; set;}

        public static void OpenOrFocusSpeckle(cSapModel model)
        {
            try
            {
                Setup.Init("ConnectorETABS");
                if (Bootstrapper != null)
                {
                    Bootstrapper.ShowRootView();
                    return;
                }

                Bootstrapper = new Bootstrapper()
                {
                    Bindings = new ConnectorBindingsETABS(model)
                };

                if (Application.Current != null)
                    new StyletAppLoader() { Bootstrapper = Bootstrapper };
                else
                    new App(Bootstrapper);



                var processes = Process.GetProcesses();
                IntPtr ptr = IntPtr.Zero;
                foreach (var process in processes)
                {
                    if (process.ProcessName.ToLower().Contains("etabs"))
                    {
                        ptr = process.MainWindowHandle;
                        break;
                    }
                }
                if (ptr != IntPtr.Zero)
                {
                    //Application.Current.MainWindow.Closed += SpeckleWindowClosed;
                    Bootstrapper.Start(Application.Current);
                    Bootstrapper.SetParent(ptr);
                    Application.Current.MainWindow.Closed += SpeckleWindowClosed;
                }
            }
            catch
            {
                Bootstrapper = null;
            }
        }

        private static void SpeckleWindowClosed(object sender, EventArgs e)
        {
            isSpeckleClosed = true;
        }

        private void SelectionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (isSpeckleClosed == true)
            {
                pluginCallback.Finish(0);
            }
        }


        public int Info(ref string Text)
        {
            Text = "This is a Speckle plugin for ETABS";
            return 0;
        }

        public void Main(ref cSapModel SapModel, ref cPluginCallback ISapPlugin)
        {
            cSapModel model;
            pluginCallback = ISapPlugin;
            try
            {
                cHelper helper = new Helper();
                var etabsObject = helper.GetObject("CSI.ETABS.API.ETABSObject");
                model = etabsObject.SapModel;
            }
            catch
            {
                ISapPlugin.Finish(0);
                return;
            }

            try
            {
                OpenOrFocusSpeckle(model);
                SelectionTimer = new Timer(2000) { AutoReset = true, Enabled = true };
                SelectionTimer.Elapsed += SelectionTimer_Elapsed;
                SelectionTimer.Start();
            }
            catch
            {
                ISapPlugin.Finish(0);
                return;
            }
        }
    }
}
