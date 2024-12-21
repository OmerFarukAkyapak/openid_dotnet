using OpenIDServer.Models;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIDServer.Seeder;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/auth/login";
});
//.AddGoogle(googleOptions =>
//{
//    googleOptions.ClientId = builder.Configuration["Google:ClientId"];
//    googleOptions.ClientSecret = builder.Configuration["Google:ClientSecret"];
//    googleOptions.Events.OnRedirectToAuthorizationEndpoint = context =>
//    {
//        context.Response.Redirect(context.RedirectUri + "&prompt=consent");
//        return Task.CompletedTask;
//    };
//});

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<OpenIdDbContext>();
    })
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token")
               .SetAuthorizationEndpointUris("/connect/authorize")
               .SetLogoutEndpointUris("/connect/logout");

        options.AllowClientCredentialsFlow()
               .AllowAuthorizationCodeFlow();

        options.AddEphemeralEncryptionKey()
               .AddEphemeralSigningKey()
               .DisableAccessTokenEncryption();

        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .EnableAuthorizationEndpointPassthrough()
               .EnableLogoutEndpointPassthrough();

        options.AddSigningKey(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["App:SigningKey"])));


        options.RegisterScopes(OpenIddictConstants.Scopes.OpenId, "profile", "email", "clientapi" , "postman");

    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});



builder.Services.AddDbContext<OpenIdDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseOpenIddict();
});

builder.Services.AddHostedService<ClientSeeder>();


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
