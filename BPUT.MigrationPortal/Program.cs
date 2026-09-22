// Program.cs
using BPUT.MigrationPortal.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IMigrationRepository, MigrationRepository>();
builder.Services.AddScoped<IMigrationDataRepository, MigrationDataRepository>();

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
    pattern: "{controller=Migration}/{action=DataEntry}/{id?}");

app.Run();