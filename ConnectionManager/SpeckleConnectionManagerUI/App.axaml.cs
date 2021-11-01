using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SpeckleConnectionManagerUI.Services;
using SpeckleConnectionManagerUI.ViewModels;
using SpeckleConnectionManagerUI.Views;
using System;
using System.Reflection;

namespace SpeckleConnectionManagerUI
{
    public class App : Application
    {
        public static Version RunningVersion { get => Assembly.GetExecutingAssembly().GetName().Version; }
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var db = new Database();
                var mainWindowModel = new MainWindowViewModel(db);
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainWindowModel,
                };

                await BackgroundRefreshTokenProcess.Main();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}