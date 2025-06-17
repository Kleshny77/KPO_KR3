using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payments_service.Data;
using payments_service.Models;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace payments_service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly PaymentsDbContext _db;
        private readonly ILogger<AccountsController> _logger;
        
        public AccountsController(PaymentsDbContext db, ILogger<AccountsController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest req)
        {
            if (req == null)
                return BadRequest("Request is null");
            if (string.IsNullOrEmpty(req.UserId))
                return BadRequest("UserId is required");
            if (!Guid.TryParse(req.UserId, out var guidUserId))
                return BadRequest("Invalid userId format");
            if (await _db.Accounts.AnyAsync(a => a.UserId == guidUserId))
            {
                _logger.LogWarning("Account already exists for userId: {UserId}", guidUserId);
                return BadRequest("Account already exists");
            }
            var acc = new Account { UserId = guidUserId, Balance = 0 };
            _db.Accounts.Add(acc);
            await _db.SaveChangesAsync();
            return Ok(acc);
        }

        [HttpPost("topup")]
        public async Task<IActionResult> TopUp([FromBody] TopUpRequest req)
        {
            if (!Guid.TryParse(req.UserId, out var guidUserId))
                return BadRequest("Invalid userId format");
            var acc = await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == guidUserId);
            if (acc == null) return NotFound("Account not found");
            acc.Balance += req.Amount;
            await _db.SaveChangesAsync();
            return Ok(acc);
        }

        [HttpGet("balance/{userId}")]
        public async Task<IActionResult> GetBalance(string userId)
        {
            if (!Guid.TryParse(userId, out var guidUserId))
                return BadRequest("Invalid userId format");
            var acc = await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == guidUserId);
            if (acc == null) return NotFound("Account not found");
            return Ok(new { acc.UserId, acc.Balance });
        }
    }

    public class TopUpRequest
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;
        public long Amount { get; set; }
    }

    public class CreateAccountRequest
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;
    }
} 