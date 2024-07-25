using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using Autodesk.Navisworks.Api.Plugins;
using Speckle.ConnectorNavisworks.Bindings;
using Speckle.Core.Helpers;
using Speckle.Core.Logging;
using NavisworksApp = Autodesk.Navisworks.Api.Application;

#if DEBUG
using System.Text;
#endif

namespace Speckle.ConnectorNavisworks.Entry;

[
  Plugin("SpeckleNavisworks", "Speckle", DisplayName = "Speckle"),
  Strings("Ribbon.name"),
  RibbonLayout("Ribbon.xaml"),
  RibbonTab("Speckle", DisplayName = "Speckle", LoadForCanExecute = true),
  Command(
    LaunchSpeckleConnector.COMMAND,
    LoadForCanExecute = true,
    Icon = "Resources/logo16.ico",
    LargeIcon = "Resources/logo32.ico",
    Shortcut = "Ctrl+Shift+S",
    ToolTip = "Speckle Connector for Navisworks",
    DisplayName = "Speckle\rConnector"
  ),
  Command(
    Community.COMMAND,
    Icon = "Resources/forum16.png",
    LargeIcon = "Resources/forum32.png",
    Shortcut = "Ctrl+Shift+C",
    ToolTip = "Visit the Speckle Support Community",
    DisplayName = "Speckle\rCommunity"
  ),
  Command(
    RetryLastConversionSend.COMMAND,
    LoadForCanExecute = true,
    Icon = "Resources/retry16.ico",
    LargeIcon = "Resources/retry32.ico",
    Shortcut = "Ctrl+Shift+R",
    ToolTip = "Retries sending the last complete conversion to Speckle.",
    DisplayName = "Retry\rSend"
  ),
  Command(
    TurnPersistCacheOn.COMMAND,
    LoadForCanExecute = true,
    Icon = "Resources/empty32.ico",
    LargeIcon = "Resources/empty32.ico",
    ToolTip = "Cache persistence is off.",
    DisplayName = "Cache"
  ),
  Command(
    TurnPersistCacheOff.COMMAND,
    LoadForCanExecute = true,
    Icon = "Resources/logo16.ico",
    LargeIcon = "Resources/logo32.ico",
    ToolTip = "Cache persistence is on.",
    ExtendedToolTip = "Cache persistence is on. If you send a model, the converted objects will be held in memory and will be sent to the Stream and Branch you specify. No setting changes will be applied.",
    DisplayName = "Cache"
  ),
]
[SuppressMessage(
  "design",
  "CA1812:Avoid uninstantiated internal classes",
  Justification = "Instantiated by Navisworks"
)]
internal sealed class RibbonHandler : CommandHandlerPlugin
{
  private static readonly Dictionary<Plugin, bool> s_loadedPlugins = new();

  /// <summary>
  /// Determines the state of a command in Navisworks.
  /// </summary>
  /// <param name="commandId">The ID of the command to check.</param>
  /// <returns>The state of the command.</returns>
  public override CommandState CanExecuteCommand(string commandId)
  {
    return commandId switch
    {
      TurnPersistCacheOn.COMMAND
        => new CommandState
        {
#if DEBUG
          IsVisible = !ConnectorBindingsNavisworks.PersistCache,
#else
          IsVisible = false,
#endif
          IsEnabled = !ConnectorBindingsNavisworks.PersistCache
        },
      TurnPersistCacheOff.COMMAND
        => new CommandState
        {
#if DEBUG
          IsVisible = ConnectorBindingsNavisworks.PersistCache,
#else
          IsVisible = false,
#endif
          IsEnabled = ConnectorBindingsNavisworks.PersistCache
        },
      _
        => commandId == RetryLastConversionSend.COMMAND
          ? new CommandState(ConnectorBindingsNavisworks.CachedConversion)
          : new CommandState(true)
    };
  }

  /// <summary>
  /// Loads a plugin in Navisworks.
  /// </summary>
  /// <param name="plugin">The name of the plugin to load.</param>
  /// <param name="notAutomatedCheck">Optional. Specifies whether to check if the application is automated. Default is true.</param>
  /// <param name="command">Optional. The command associated with the plugin. Default is an empty string.</param>
  private static void LoadPlugin(string plugin, bool notAutomatedCheck = true, string command = "")
  {
    if (ShouldSkipLoad(notAutomatedCheck))
    {
      return;
    }

    if (ShouldSkipPluginLoad(plugin, command))
    {
      return;
    }

    var pluginRecord = NavisworksApp.Plugins.FindPlugin(plugin + ".Speckle");
    if (pluginRecord is null)
    {
      return;
    }

    var loadedPlugin = pluginRecord.LoadedPlugin ?? pluginRecord.LoadPlugin();

    ActivatePluginPane(pluginRecord, loadedPlugin, command);
  }

  /// <summary>
  /// Checks whether the load should be skipped based on the notAutomatedCheck flag and application automation status.
  /// </summary>
  /// <param name="notAutomatedCheck">The flag indicating whether to check if the application is automated.</param>
  /// <returns>True if the load should be skipped, False otherwise.</returns>
  private static bool ShouldSkipLoad(bool notAutomatedCheck) => notAutomatedCheck && NavisworksApp.IsAutomated;

  /// <summary>
  /// Checks whether the plugin load should be skipped based on the plugin and command values.
  /// </summary>
  /// <param name="plugin">The name of the plugin.</param>
  /// <param name="command">The command associated with the plugin.</param>
  /// <returns>True if the plugin load should be skipped, False otherwise.</returns>
  private static bool ShouldSkipPluginLoad(string plugin, string command) =>
    string.IsNullOrEmpty(plugin) || string.IsNullOrEmpty(command);

  /// <summary>
  /// Activates the plugin's pane if it is of the right type.
  /// </summary>
  /// <param name="pluginRecord">The plugin record.</param>
  /// <param name="loadedPlugin">The loaded plugin instance.</param>
  /// <param name="command">The command associated with the plugin.</param>
  private static void ActivatePluginPane(PluginRecord pluginRecord, object loadedPlugin, string command)
  {
    if (ShouldActivatePluginPane(pluginRecord))
    {
      var dockPanePlugin = (DockPanePlugin)loadedPlugin;
      dockPanePlugin.ActivatePane();

      s_loadedPlugins[dockPanePlugin] = true;
    }
    else
    {
#if DEBUG
      ShowPluginInfoMessageBox();
      ShowPluginNotLoadedMessageBox(command);
#endif
    }
  }

  /// <summary>
  /// Checks whether the plugin's pane should be activated based on the plugin record.
  /// </summary>
  /// <param name="pluginRecord">The plugin record.</param>
  /// <returns>True if the plugin's pane should be activated, False otherwise.</returns>
  private static bool ShouldActivatePluginPane(PluginRecord pluginRecord) =>
    pluginRecord.IsLoaded && pluginRecord is DockPanePluginRecord && pluginRecord.IsEnabled;

  public override int ExecuteCommand(string commandId, params string[] parameters)
  {
    // ReSharper disable once RedundantAssignment
    var buildVersion = string.Empty;

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
#if NAVMAN21
    buildVersion = "2024";
#endif
#if NAVMAN22
    buildVersion = "2025";
#endif

    // Version
    if (!NavisworksApp.Version.RuntimeProductName.Contains(buildVersion))
    {
      MessageBox.Show(
        "This Add-In was built for Navisworks "
          + buildVersion
          + ", please contact jonathon@speckle.systems for assistance...",
        "Cannot Continue!",
        MessageBoxButtons.OK,
        MessageBoxIcon.Error
      );
      return 0;
    }

    switch (commandId)
    {
      case LaunchSpeckleConnector.COMMAND:
      {
        LoadPlugin(LaunchSpeckleConnector.PLUGIN, command: commandId);
        break;
      }

      case RetryLastConversionSend.COMMAND:
      {
        LoadPlugin(RetryLastConversionSend.PLUGIN, command: commandId);

        var retryPlugin = NavisworksApp.Plugins.FindPlugin(RetryLastConversionSend.PLUGIN + ".Speckle").LoadedPlugin;

        s_loadedPlugins.TryGetValue(retryPlugin, out var loaded);

        if (loaded)
        {
          try
          {
            var speckleCommand = retryPlugin as SpeckleNavisworksCommandPlugin;

            speckleCommand?.Bindings.RetryLastConversionSend();
          }
          catch (NotImplementedException)
          {
            MessageBox.Show("This command is not implemented yet.");
          }
          catch (SpeckleException ex)
          {
            MessageBox.Show(ex.Message);
          }
        }

        break;
      }

      case Community.COMMAND:
      {
        Open.Url("https://speckle.community/tag/navisworks");
        break;
      }

      case TurnPersistCacheOff.COMMAND
      or TurnPersistCacheOn.COMMAND:
      {
        ConnectorBindingsNavisworks.PersistCache = !ConnectorBindingsNavisworks.PersistCache;

        if (ConnectorBindingsNavisworks.PersistCache == false)
        {
          ConnectorBindingsNavisworks.CachedConvertedElements = null;
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

#if DEBUG
  /// <summary>
  /// Shows a message box displaying plugin information.
  /// </summary>
  private static void ShowPluginInfoMessageBox()
  {
    var sb = new StringBuilder();
    foreach (var pr in NavisworksApp.Plugins.PluginRecords)
    {
      sb.AppendLine(pr.Name + ": " + pr.DisplayName + ", " + pr.Id);
    }

    MessageBox.Show(sb.ToString());
  }

  /// <summary>
  /// Shows a message box indicating that the plugin was not loaded.
  /// </summary>
  /// <param name="command">The command associated with the plugin.</param>
  private static void ShowPluginNotLoadedMessageBox(string command) => MessageBox.Show(command + " Plugin not loaded.");
#endif
}
