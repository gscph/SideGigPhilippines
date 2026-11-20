using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using SideGigPh.Admin;
using SideGigPh.Admin.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

var firebase = builder.Configuration.GetSection("Firebase").Get<FirebaseWebOptions>() ?? new FirebaseWebOptions();
builder.Services.AddSingleton(firebase);
builder.Services.AddSingleton<IAdminSignIn, FirebaseWebSignIn>();
builder.Services.AddSingleton<BadgeQueue>();

// Hosted by the Function App itself (…/api/operator/), the API is the page's own "../": same origin, no CORS.
// Api:BaseUrl (appsettings.Development.json) points a local run at the live API instead.
var apiUrl = builder.Configuration["Api:BaseUrl"] is { Length: > 0 } configured
    ? new Uri(configured)
    : new Uri(new Uri(builder.HostEnvironment.BaseAddress), "../");
builder.Services.AddSingleton(sp => new AdminApi(new HttpClient { BaseAddress = apiUrl }, sp.GetRequiredService<IAdminSignIn>()));

await builder.Build().RunAsync();
