using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIDApi.Models;

namespace OpenIDServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly OpenIdDbContext _context;
        public UserController(OpenIdDbContext context)
        {
            _context = context;

        }

        [HttpGet("users-count")]
        public async Task<ActionResult<int>> GetUsersCount()
        {
            return await _context.Users.CountAsync();
        }


        [HttpGet("users-with-client")]
        [Authorize(Policy = "ClientApiPolicy")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsersWithClientApi()
        {
            return await _context.Users.ToListAsync();
        }


        [HttpGet("users-with-postman")]
        [Authorize(Policy = "PostmanPolicy")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsersWithPostman()
        {
            return await _context.Users.ToListAsync();
        }

        [HttpGet("{id}/user-detail-by-id")]
        [Authorize]
        public async Task<ActionResult<object>> GetUserDetailsById(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var hasClientScope = User.HasClaim("scope", "clientapi");
            var hasPostmanScope = User.HasClaim("scope", "postman");

            if (hasPostmanScope)
            {
                return new User
                {
                    Id = user.Id,
                    Username = user.Username,
                    Password = user.Password,
                    Email = user.Email
                };
            }
            else if (hasClientScope)
            {
                return new User
                {
                    Username = user.Username
                };
            }

            return Forbid();
        }

    }
}
