using System.Text.RegularExpressions;
using EmployeeManagementAPI.Data;
using EmployeeManagementAPI.Models.Api;
using EmployeeManagementAPI.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public EmployeeController(AppDbContext dbContext) =>
            _dbContext = dbContext;

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<Employee>>> Get(
            [FromQuery] string? sortBy = null,
            [FromQuery] string? order = "asc",
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            if (pageNumber < 0 || pageSize < 0)
            {
                return BadRequest("Page number and page size must be greater than zero.");
            }

            IQueryable<Employee> query = _dbContext.Employees;

            switch (sortBy?.ToLower())
            {
                case "name":
                    query = order.ToLower() == "desc"
                        ? query.OrderByDescending(e => e.Name)
                        : query.OrderBy(e => e.Name);
                    break;

                case "hiredate":
                    query = order.ToLower() == "desc"
                        ? query.OrderByDescending(e => e.HireDate)
                        : query.OrderBy(e => e.HireDate);
                    break;

                default:
                    break;
            }

            var totalCount = await query.CountAsync();

            var employees = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new PagedResult<Employee>
            {
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Data = employees
            });
        }

        [HttpGet("hired")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetByHireDateRange(
            [FromQuery] DateOnly start,
            [FromQuery] DateOnly end)
        {
            if (start > end)
            {
                return BadRequest("Start date must be earlier than end date"); 
            }

            var employees = await _dbContext.Employees
                .Where(e => e.HireDate >= start && e.HireDate <= end)
                .ToListAsync();

            if (!employees.Any())
            {
                return NotFound("No employees found in the given date range.");
            }

            return Ok(employees);
        }


        [HttpGet("department/{department}")]
        public async Task<ActionResult<IEnumerable<Employee>>> GetByDepartment(string department)
        {
            var employees = await _dbContext.Employees
                .Where(e => e.Department == department)
                .ToListAsync();

            if (!employees.Any())
            {
                return NotFound($"No employees found in department: {department}");
            }

            return Ok(employees);
        }

        [HttpGet("{id}")]
        public async Task<Employee> GetById(int id)
        {
            return await _dbContext.Employees.FirstOrDefaultAsync(x => x.Id == id);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] Employee employee)
        {
            if (string.IsNullOrEmpty(employee.Name) || string.IsNullOrEmpty(employee.Email) || string.IsNullOrEmpty(employee.Department) || employee.HireDate > DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest("Invalid Request");
            }

            if (!IsValidEmail(employee.Email))
            {
                return BadRequest("Invalid Request: Email format is not valid.");
            }

            var existingEmployee = await _dbContext.Employees
                .FirstOrDefaultAsync(e => e.Email == employee.Email);

            if (existingEmployee != null)
            {
                return Conflict(new
                {
                    Message = "Email is already in use by another employee",
                    ExistingEmployee = existingEmployee
                });
            }

            await _dbContext.Employees.AddAsync(employee);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
        }

        [Authorize]
        [HttpPut]
        public async Task<ActionResult> Update([FromBody] Employee employee)
        {
            if (employee.Id == 0 || string.IsNullOrEmpty(employee.Name) || string.IsNullOrEmpty(employee.Email) || string.IsNullOrEmpty(employee.Department) || employee.HireDate > DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest("Invalid Request");
            }

            var existingEmployee = await _dbContext.Employees
                .FirstOrDefaultAsync(e => e.Email == employee.Email && e.Id != employee.Id);

            if (existingEmployee != null)
            {
                return Conflict(new
                {
                    Message = "Email is already in use by another employee",
                    ExistingEmployee = existingEmployee
                });
            }

            _dbContext.Employees.Update(employee);
            await _dbContext.SaveChangesAsync();

            return Ok();
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var employee = await GetById(id);

            if (employee is null)
            {
                return NotFound();
            }

            _dbContext.Employees.Remove(employee);
            await _dbContext.SaveChangesAsync();

            return Ok();
        }
    }
}
