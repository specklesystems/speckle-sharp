using Autodesk.Navisworks.Api.Plugins;
using Speckle.ConnectorNavisworks.Bindings;
using Speckle.ConnectorNavisworks.Entry;
using System.Reflection;
using System.IO;
using System;
using System.Text;
using System.Windows.Forms;
using NavisworksApp = Autodesk.Navisworks.Api.Application;
using System.Diagnostics;

namespace Speckle.ConnectorNavisworks.Entry
{
    [Plugin("SpeckleNavisworks", "Speckle", DisplayName = "Speckle")]
    [Strings("Ribbon.name")]
    [RibbonLayout("Ribbon.xaml")]
    [RibbonTab("Speckle", DisplayName = "Speckle", LoadForCanExecute = true)]
    //[Command(OneClickSend.Command,
    //    CallCanExecute = CallCanExecute.DocumentNotClear,
    //    LoadForCanExecute = true,
    //    Icon = "Resources/logo16.ico",
    //    LargeIcon = "Resources/logo32.ico",
    //    Shortcut = "Ctrl+Shift+Q",
    //    ToolTip = "Command to send selection to the document stream, or everything if nothing is selected",
    //    DisplayName = "Quick\rSend"
    //)]
    [Command(LaunchSpeckleConnector.Command,
        LoadForCanExecute = true,
        Icon = "Resources/logo16.ico",
        LargeIcon = "Resources/logo32.ico",
        Shortcut = "Ctrl+Shift+S",
        ToolTip = "Speckle Connector for Navisworks",
        DisplayName = "Speckle\rConnector"
    )]
    [Command(Community.Command,
        LoadForCanExecute = true,
        Icon = "Resources/forum16.png",
        LargeIcon = "Resources/forum32.png",
        Shortcut = "Ctrl+Shift+S",
        ToolTip = "Visit the Speckle Support Community",
        DisplayName = "Speckle\rCommunity"
    )]
    internal class RibbonHandler : CommandHandlerPlugin
    {
        public override CommandState CanExecuteCommand(string commandId)
        {
            CommandState state = new CommandState(true);

            // Currently there is only one command that needs responsive state management. There could be others.
            //switch (commandId)
            //{
            //    case OneClickSend.Command:
            //    {
            //        state.IsEnabled = false; // Until I am ready to implement this
            //                                 break;
            //    }
            //}

            return state;
        }

        public bool LoadPlugin(string plugin, bool notAutomatedCheck = true, string command = "")
        {
            if (notAutomatedCheck && NavisworksApp.IsAutomated)
            {
                return false;
            }

            if (plugin.Length == 0 || command.Length == 0)
            {
                return false;
            }

            PluginRecord pluginRecord = NavisworksApp.Plugins.FindPlugin(plugin + ".Speckle");

            if (pluginRecord is null)
            {
                return false;
            }

            Plugin loadedPlugin = pluginRecord.LoadedPlugin ?? pluginRecord.LoadPlugin();

            // Activate the plugin's pane if it is of the right type
            // At this time the associated commands are handled separately to this parent plugin 
            if (pluginRecord.IsLoaded && pluginRecord is DockPanePluginRecord && pluginRecord.IsEnabled)
            {
                DockPanePlugin dockPanePlugin = (DockPanePlugin)loadedPlugin;
                dockPanePlugin.ActivatePane();
            }
            else
            {
#if DEBUG
                StringBuilder sb = new StringBuilder();

                foreach (PluginRecord pr in NavisworksApp.Plugins.PluginRecords)
                {
                    sb.AppendLine(pr.Name + ": " + pr.DisplayName + ", " + pr.Id);
                }

                MessageBox.Show(sb.ToString());
                MessageBox.Show(command + " Plugin not loaded.");
#endif
            }

            return pluginRecord.IsLoaded;
        }


        public override int ExecuteCommand(string commandId, params string[] parameters)
        {
            string buildVersion = "2023";

#if NAVMAN17
                buildVersion = "2020";
#endif
#if NAVMAN18
                buildVersion = "2021";
#endif
#if NAVMAN19
                buildVersion = "2022";
#endif
#if NAVMAN20
            buildVersion = "2023";
#endif

            // Version
            if (!NavisworksApp.Version.RuntimeProductName.Contains(buildVersion))
            {
                MessageBox.Show(
                    "This Add-In was built for Navisworks " + buildVersion +
                    ", please contact jonathon@speckle.systems for assistance...",
                    "Cannot Continue!",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return 0;
            }


            switch (commandId)
            {
                //case OneClickSend.Command:
                //{
                //    OneClickSendCommand.SendCommand();
                //    break;
                //}


                case LaunchSpeckleConnector.Command:
                {
                    LoadPlugin(plugin: LaunchSpeckleConnector.Plugin, command: commandId);
                    break;
                }

                case Community.Command:
                {
                    try
                    {
                        Process.Start("https://speckle.community/");
                    }
                    catch
                    {
                        // ignored
                    }

                    break;
                }

                default:
                {
                    MessageBox.Show("You have clicked on an unexpected command with ID = '" + commandId + "'");
                    break;
                }
            }

            return 0;
        }
    }
}