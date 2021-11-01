using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace SpeckleConnectionManagerUI.Services
{
    public class BackgroundRefreshTokenProcess
    {
        public static async Task Main()
        {
            await new HostBuilder()
                .ConfigureServices((hostContext, services) => { services.AddHostedService<TimedRefreshToken>(); })
                .RunConsoleAsync();
        }
    }
}