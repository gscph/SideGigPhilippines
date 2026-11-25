using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using SideGigPh.FrontDoor;
using SideGigPh.FrontDoor.EarlyAccess;
using SideGigPh.FrontDoor.Localization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// The published index.html already holds the finished page (BlazorWasmPreRendering.Build, csproj) and names its own root
// components; they are added here only when it doesn't (a local `dotnet run`).
if (!builder.RootComponents.Any())
{
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
}

ConfigureServices(builder.Services, builder.Configuration);

await builder.Build().RunAsync();

// A static local function by this name: the publish-time prerender calls it too, so the page renders the same there.
// The page's services: MudBlazor, the language, the sign-up and the few browser calls.
static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // The API's address: wwwroot/appsettings.json (live), appsettings.Development.json (a local `func start`).
    var apiUrl = configuration["ApiUrl"] ?? throw new InvalidOperationException("Missing ApiUrl in wwwroot/appsettings.json.");
    services.AddMudServices();
    services.AddScoped<PageLanguage>();
    services.AddScoped<SignupState>();
    services.AddScoped<PageScript>();
    services.AddScoped<IEarlyAccessSignup>(_ => new HttpEarlyAccessSignup(new HttpClient { BaseAddress = new Uri(apiUrl) }));
}
