using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ETABSv1;

using Speckle.DesktopUI;
using System.Timers;
using System.Windows;
using System.Diagnostics;
using Speckle.Core.Logging;

using ConnectorETABSShared.Util;
using ConnectorETABSShared.UI;


namespace Speckle.ConnectorETABS
{
    public class cPlugin
    {
        public static cPluginCallback pluginCallback { get; set; }
        public static Bootstrapper Bootstrapper { get; set; }
        public static bool isSpeckleClosed { get; set; } = false;

        public Timer SelectionTimer;


        public static void OpenOrFocusSpeckle(ConnectorETABSDocument doc)
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
                    Bindings = new ConnectorBindingsETABS(doc)
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

        private static ConnectorETABSDocument AttachToRunningInstance()
        {
            try
            {
                cHelper helper = new Helper();
                var etabsObject = helper.GetObject("CSI.ETABS.API.ETABSObject");
                var doc = new ConnectorETABSDocument();
                doc.Document = etabsObject.SapModel;
                return doc;
            }
            catch
            {
                return null;
            }
        }

        public int Info(ref string Text)
        {
            Text = "This is a Speckle plugin for ETABS";
            return 0;
        }

        public void Main(ref cSapModel SapModel, ref cPluginCallback ISapPlugin)
        {
            pluginCallback = ISapPlugin;
            ConnectorETABSDocument doc = AttachToRunningInstance();
            if (doc == null)
            {
                ISapPlugin.Finish(0);
                return;
            }
            try
            {
                OpenOrFocusSpeckle(doc);
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
