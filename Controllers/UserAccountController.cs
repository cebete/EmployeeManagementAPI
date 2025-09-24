using EmployeeManagementAPI.Data;
using EmployeeManagementAPI.Handlers;
using EmployeeManagementAPI.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserAccountController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public UserAccountController(AppDbContext dbContext) =>
            _dbContext = dbContext;

        [HttpGet]
        public async Task<List<UserAccount>> Get()
        {
            return await _dbContext.UserAccounts.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<UserAccount> GetById(int id)
        {
            return await _dbContext.UserAccounts.FirstOrDefaultAsync(x => x.Id == id);
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] UserAccount userAccount)
        {
            if (string.IsNullOrWhiteSpace(userAccount.Name) ||
                string.IsNullOrWhiteSpace(userAccount.UserName) ||
                string.IsNullOrWhiteSpace(userAccount.Password))
            {
                return BadRequest("Invalid Request");
            }

            userAccount.Password = PasswordHashHandler.HashPassword(userAccount.Password);
            await _dbContext.UserAccounts.AddAsync(userAccount);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = userAccount.Id }, userAccount);
        }

        [HttpPut]
        public async Task<ActionResult> Update([FromBody] UserAccount userAccount)
        {
            if (userAccount.Id == 0 ||
                string.IsNullOrWhiteSpace(userAccount.Name) ||
                string.IsNullOrWhiteSpace(userAccount.UserName) ||
                string.IsNullOrWhiteSpace(userAccount.Password))
            {
                return BadRequest("Invalid Request");
            }

            userAccount.Password = PasswordHashHandler.HashPassword(userAccount.Password);
            _dbContext.UserAccounts.Update(userAccount);
            await _dbContext.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var userAccount = await GetById(id);
            if (userAccount is null)
                return NotFound();

            _dbContext.UserAccounts.Remove(userAccount);
            await _dbContext.SaveChangesAsync();

            return Ok();
        }

    }
}
