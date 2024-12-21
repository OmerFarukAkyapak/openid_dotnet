using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenIDApi.Models;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient();

builder.Services.AddDbContext<OpenIdDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.OAuth2,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri("https://localhost:7264/connect/authorize"),
                TokenUrl = new Uri("https://localhost:7264/connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "clientapi", "Client Api Access" }
                }
            },
        }
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
            },
            Array.Empty<string>()
        }
    });

});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
{
    options.Authority = builder.Configuration["OpenIdServer:Authority"];
    options.Audience = builder.Configuration["OpenIdServer:Audience"];
    options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("OpenIdServer:RequireHttpsMetadata");

    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["OpenIdServer:SigningKey"]))
    };
    options.Events = new()
    {
        OnTokenValidated = async context =>
        {
            if (context.Principal?.Identity is ClaimsIdentity claimsIdentity)
            {
                Claim? scopeClaim = claimsIdentity.FindFirst("scope");
                if (scopeClaim is not null)
                {
                    claimsIdentity.RemoveClaim(scopeClaim);
                    claimsIdentity.AddClaims(scopeClaim.Value.Split(" ")
                        .Select(s => new Claim("scope", s)).ToList());
                }
            }
            await Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("Authentication failed: " + context.Exception.Message);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("ClientApiPolicy", policy => policy.RequireClaim("scope", "clientapi"))
    .AddPolicy("PostmanPolicy", policy => policy.RequireClaim("scope", "postman"));


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.OAuthClientId("open-id-api");
        c.OAuthClientSecret("open-id-api-secret");
        c.OAuthScopes("clientapi");
    });
}

app.Use(async (context, next) =>
{
    await next();
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
