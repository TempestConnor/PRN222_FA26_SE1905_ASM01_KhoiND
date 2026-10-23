using HorseClub.Services.KhoiND;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IHorseServiceKhoiND, HorseServiceKhoiND>();
builder.Services.AddScoped<IOwnerServiceKhoiND, OwnerServiceKhoiND>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.MapStaticAssets();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Horses}/{action=Index}/{id?}")
   .WithStaticAssets();

app.Run();
