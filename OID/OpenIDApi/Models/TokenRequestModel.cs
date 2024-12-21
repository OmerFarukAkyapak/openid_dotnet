namespace OpenIDApi.Models
{
    public class TokenRequestModel
    {
        public string ClientId { get; set; } // open-id-api
        public string ClientSecret { get; set; } // open-id-api-secret
        public string GrantType { get; set; } // client_credentials
        public string Scope { get; set; } // api
    }
}
