using PISM.Core.Options;
using PISM.Core.Services;
using PISM.Data;
using PISM.Web.Hubs;
using PISM.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.Section));
builder.Services.Configure<EncryptionOptions>(builder.Configuration.GetSection(EncryptionOptions.Section));
builder.Services.Configure<TestingOptions>(builder.Configuration.GetSection(TestingOptions.Section));

builder.Services.AddSingleton<IEncryptionService, AesGcmEncryptionService>();
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

builder.Services.AddPismData(builder.Configuration.GetConnectionString("Default")!);
builder.Services.AddSignalR();
builder.Services.AddHostedService<ScanBackgroundService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ScanHub>(ScanHub.Url);

app.Run();
