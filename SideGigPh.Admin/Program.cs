using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using SideGigPh.Admin.Components;
using SideGigPh.Admin.Components.Badge;
using SideGigPh.Admin.Components.Dashboard;
using SideGigPh.Admin.Components.ErrorReport;
using SideGigPh.Admin.Components.Seeker;
using SideGigPh.Admin.Components.Shared;
using SideGigPh.Admin.Components.User;
using SideGigPh.Admin.Components.Worker;
using SideGigPh.Admin.Options;
using SideGigPh.Admin.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

// Sign-in and the API.
var firebase = builder.Configuration.GetSection("Firebase").Get<FirebaseWebOptions>() ?? new FirebaseWebOptions();
builder.Services.AddSingleton(firebase);
builder.Services.AddSingleton<IAdminSignIn, FirebaseWebSignIn>();

// Hosted by the Function App itself (…/api/operator/), the API is the page's own "../": same origin, no CORS.
// Api:BaseUrl (appsettings.Development.json) points a local run at the live API instead.
var apiUrl = builder.Configuration["Api:BaseUrl"] is { Length: > 0 } configured
    ? new Uri(configured)
    : new Uri(new Uri(builder.HostEnvironment.BaseAddress), "../");
builder.Services.AddSingleton(sp => new AdminApi(new HttpClient { BaseAddress = apiUrl }, sp.GetRequiredService<IAdminSignIn>()));
builder.Services.AddSingleton<AuthService>();

// One service per feature (Components/<Feature>/<Feature>Service.cs).
builder.Services.AddSingleton<ReferenceService>();
builder.Services.AddSingleton<DashboardService>();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<SeekerService>();
builder.Services.AddSingleton<WorkerService>();
builder.Services.AddSingleton<BadgeService>();
builder.Services.AddSingleton<ErrorReportService>();

await builder.Build().RunAsync();
