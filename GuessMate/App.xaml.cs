using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ServerSide;

namespace GuessMate
{
    public partial class App : Application
    {
        private ServiceProvider serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Set up the DI container
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IGameServerFactory, GameServerFactory>();
            serviceCollection.AddTransient<MainWindow>();

            // Build the service provider
            serviceProvider = serviceCollection.BuildServiceProvider();

            // Resolve the MainWindow and show it
            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}