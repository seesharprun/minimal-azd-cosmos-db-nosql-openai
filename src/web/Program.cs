using Microsoft.Samples.Cosmos.Basic.Web.Components;
using Microsoft.Samples.Cosmos.Basic.Web.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.Configure<Connection>(builder.Configuration.GetSection(nameof(Connection)));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

await app.RunAsync();