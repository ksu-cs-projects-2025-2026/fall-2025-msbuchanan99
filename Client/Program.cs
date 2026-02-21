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


            builder.Services.AddScoped<Admin_FlossState>();
            builder.Services.AddScoped<Admin_FlossService>();
            builder.Services.AddScoped<Admin_ProjectFlossState>();
            builder.Services.AddScoped<Admin_ProjectFlossService>();
            builder.Services.AddScoped<Admin_ProjectState>();
            builder.Services.AddScoped<Admin_ProjectService>();
            builder.Services.AddScoped<Admin_UserFlossState>();
            builder.Services.AddScoped<Admin_UserFlossService>();
            builder.Services.AddScoped<Admin_UserState>();
            builder.Services.AddScoped<Admin_UserService>();
            builder.Services.AddScoped<FlossState>();
            builder.Services.AddScoped<FlossService>();
            builder.Services.AddScoped<ProjectFlossService>();
            builder.Services.AddScoped<ProjectFlossState>();
            builder.Services.AddScoped<ProjectState>();
            builder.Services.AddScoped<ProjectService>();
            builder.Services.AddScoped<UserFlossState>();
            builder.Services.AddScoped<UserFlossService>();
            builder.Services.AddScoped<UserState>();
            builder.Services.AddScoped<UserService>();

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
