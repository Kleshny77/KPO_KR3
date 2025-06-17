using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using payments_service.Data;
using payments_service.Models;

namespace payments_service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly PaymentsDbContext _db;
        public AccountsController(PaymentsDbContext db)
        {
            _db = db;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateAccount([FromBody] string userId)
        {
            if (await _db.Accounts.AnyAsync(a => a.UserId == userId))
                return BadRequest("Account already exists");
            var acc = new Account { UserId = userId, Balance = 0 };
            _db.Accounts.Add(acc);
            await _db.SaveChangesAsync();
            return Ok(acc);
        }

        [HttpPost("topup")]
        public async Task<IActionResult> TopUp([FromBody] TopUpRequest req)
        {
            var acc = await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == req.UserId);
            if (acc == null) return NotFound("Account not found");
            acc.Balance += req.Amount;
            await _db.SaveChangesAsync();
            return Ok(acc);
        }

        [HttpGet("balance/{userId}")]
        public async Task<IActionResult> GetBalance(string userId)
        {
            var acc = await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            if (acc == null) return NotFound("Account not found");
            return Ok(new { acc.UserId, acc.Balance });
        }
    }

    public class TopUpRequest
    {
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
} 