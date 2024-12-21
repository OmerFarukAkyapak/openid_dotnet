using Microsoft.AspNetCore.Mvc;
using OpenIDApi.Models;

namespace OpenIDApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TokenController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public TokenController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("get-token")]
        public async Task<IActionResult> GetToken([FromBody] TokenRequestModel requestModel)
        {
            var tokenEndpoint = "https://localhost:7264/connect/token";

            var client = _httpClientFactory.CreateClient();

            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", requestModel.ClientId),
                new KeyValuePair<string, string>("client_secret", requestModel.ClientSecret),
                new KeyValuePair<string, string>("grant_type", requestModel.GrantType),
                new KeyValuePair<string, string>("scope", requestModel.Scope)
            });

            var response = await client.PostAsync(tokenEndpoint, formData);

            if (!response.IsSuccessStatusCode)
            {
                return BadRequest(new
                {
                    Error = "Failed to get token",
                    StatusCode = (int)response.StatusCode,
                    Details = await response.Content.ReadAsStringAsync()
                });
            }

            var content = await response.Content.ReadAsStringAsync();
            return Ok(content);
        }
    }
}
