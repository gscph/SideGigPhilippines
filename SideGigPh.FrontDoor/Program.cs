using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SideGigPh.FrontDoor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The API's address: wwwroot/appsettings.json (live), appsettings.Development.json (a local `func start`).
var apiUrl = builder.Configuration["ApiUrl"] ?? throw new InvalidOperationException("Missing ApiUrl in wwwroot/appsettings.json.");
builder.Services.AddFrontDoor(apiUrl);

await builder.Build().RunAsync();
