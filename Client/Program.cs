using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            var serverApiBase = builder.Configuration["ServerApiBaseUrl"] ?? "https://localhost:7120";

            builder.Services.AddHttpClient("ServerAPI", client =>
            {
                client.BaseAddress = new Uri(serverApiBase);
            });

            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ServerAPI"));

            //var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            await builder.Build().RunAsync();
        }
    }
}
