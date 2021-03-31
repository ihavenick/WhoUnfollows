using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.MobileBlazorBindings;
using MudBlazor.Services;
using Xamarin.Forms;

namespace WhoUnfollows
{
    public class App : Application
    {
        public App(IFileProvider fileProvider = null)
        {
            var hostBuilder = MobileBlazorBindingsHost.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    // Adds web-specific services such as NavigationManager
                    services.AddBlazorHybrid();
                    services.AddMudServices();

                    // Register app-specific services
                    services.AddSingleton<CounterState>();
                    services.AddSingleton<UserState>();
                })
                .UseWebRoot("wwwroot");

            if (fileProvider != null)
                hostBuilder.UseStaticFiles(fileProvider);
            else
                hostBuilder.UseStaticFiles();
            var host = hostBuilder.Build();

            MainPage = new ContentPage {Title = "WhoUnfollows"};
            host.AddComponent<Main>(MainPage);
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}