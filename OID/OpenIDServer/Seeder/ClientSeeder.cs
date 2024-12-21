using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenIDServer.Seeder
{
    public class ClientSeeder : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public ClientSeeder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            await PopulateScopes(scope, cancellationToken);

            await PopulateClientApp(scope, cancellationToken);

            await PopulateServiceApp(scope, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        private async ValueTask PopulateScopes(IServiceScope scope, CancellationToken cancellationToken)
        {
            var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

            var scopes = new[]
            {
                new OpenIddictScopeDescriptor
                {
                    Name = "clientapi",
                    DisplayName = "Client Api Access",
                    Resources = { "api" }
                },
                new OpenIddictScopeDescriptor
                {
                    Name = "postman",
                    DisplayName = "Postman Access",
                    Resources = { "api" }
                }
            };

            foreach (var scopeDescriptor in scopes)
            {
                var scopeInstance = await scopeManager.FindByNameAsync(scopeDescriptor.Name, cancellationToken);

                if (scopeInstance == null)
                {
                    await scopeManager.CreateAsync(scopeDescriptor, cancellationToken);
                }
                else
                {
                    await scopeManager.UpdateAsync(scopeInstance, scopeDescriptor, cancellationToken);
                }
            }
        }

        private async ValueTask PopulateClientApp(IServiceScope scopeService, CancellationToken cancellationToken)
        {
            var appManager = scopeService.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

            var appDescriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "open-id-api",
                ClientSecret = "open-id-api-secret",
                DisplayName = "OpenIDApi",
                RedirectUris = { new Uri("https://localhost:7085/swagger/oauth2-redirect.html") },
                ClientType = ClientTypes.Confidential,
                ConsentType = ConsentTypes.Explicit,
                Permissions =
                    {
                        Permissions.Endpoints.Token,
                        Permissions.Endpoints.Authorization,
                        Permissions.Endpoints.Introspection,
                        Permissions.Endpoints.Revocation,

                        Permissions.ResponseTypes.Code,

                        Permissions.GrantTypes.ClientCredentials,
                        Permissions.GrantTypes.AuthorizationCode,
                        Permissions.GrantTypes.RefreshToken,

                        Permissions.Scopes.Profile,
                        Permissions.Scopes.Email,
                        Permissions.Prefixes.Scope + "clientapi",

                    }
            };

            var client = await appManager.FindByClientIdAsync(appDescriptor.ClientId, cancellationToken);

            if (client == null)
            {
                await appManager.CreateAsync(appDescriptor, cancellationToken);
            }
            else
            {
                await appManager.UpdateAsync(client, appDescriptor, cancellationToken);
            }
        }

        private async ValueTask PopulateServiceApp(IServiceScope scopeService, CancellationToken cancellationToken)
        {
            var appManager = scopeService.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

            var appDescriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "open-id-postman",
                ClientSecret = "open-id-postman-secret",
                DisplayName = "OpenIDPostman",
                RedirectUris = { new Uri("https://oauth.pstmn.io/v1/callback") },
                ClientType = ClientTypes.Confidential,
                ConsentType = ConsentTypes.Explicit,
                Permissions =
                    {
                        Permissions.Endpoints.Token,
                        Permissions.Endpoints.Authorization,
                        Permissions.Endpoints.Introspection,
                        Permissions.Endpoints.Revocation,

                        Permissions.ResponseTypes.Code,

                        Permissions.GrantTypes.ClientCredentials,
                        Permissions.GrantTypes.AuthorizationCode,
                        Permissions.GrantTypes.RefreshToken,

                        Permissions.Scopes.Profile,
                        Permissions.Scopes.Email,
                        Permissions.Prefixes.Scope + "postman"
                    }
            };

            var client = await appManager.FindByClientIdAsync(appDescriptor.ClientId, cancellationToken);

            if (client == null)
            {
                await appManager.CreateAsync(appDescriptor, cancellationToken);
            }
            else
            {
                await appManager.UpdateAsync(client, appDescriptor, cancellationToken);
            }
        }
    }
}
