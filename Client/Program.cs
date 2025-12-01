using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using System.Net.Http;
using Client.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            var serverApiBase = builder.Configuration["ServerApiBaseUrl"]; 
            if (string.IsNullOrWhiteSpace(serverApiBase))
            {
                serverApiBase = "https://localhost:7120/"; 
            }

            builder.Services.AddTransient<BrowserCredentialsHandler>();

            // Preferred: named client via IHttpClientFactory
            builder.Services.AddHttpClient("ServerAPI", client =>
            {
                client.BaseAddress = new Uri(serverApiBase);
            })
            .AddHttpMessageHandler<BrowserCredentialsHandler>();

            builder.Services.AddScoped(sp =>
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("ServerAPI"));

            builder.Services.AddScoped<UserState>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<ProjectState>();
            builder.Services.AddScoped<ProjectService>();

            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            await builder.Build().RunAsync();
        }

        // Applies withCredentials=include to every request made by this client
        public sealed class BrowserCredentialsHandler : DelegatingHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include); // <- from the Http package
                return base.SendAsync(request, ct);
            }
        }
    }
}
