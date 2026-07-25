using Azunt.AttachmentManagement;
using Azunt.Web.Components;
using Azunt.Web.Components.Pages.Attachments;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// The sample application runs without SQL Server.
builder.Services.AddDependencyInjectionContainerForAttachmentApp(
    mode: AttachmentServicesRegistrationExtensions.RepositoryMode.EfCoreInMemory);

var app = builder.Build();

await AttachmentSeedData.InitializeAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
