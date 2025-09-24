using EmployeeManagementAPI.Data;
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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Employee>>> Get(
            [FromQuery] string? sortBy = null,
            [FromQuery] string? order = "asc") // default ascending
        {
            IQueryable<Employee> query = _dbContext.Employees;

            switch (sortBy?.ToLower())
            {
                case "name":
                    query = order.ToLower() == "desc"
                        ? query.OrderByDescending(e => e.Name)
                        : query.OrderBy(e => e.Name);
                    break;

                case "hireDate":
                    query = order.ToLower() == "desc"
                        ? query.OrderByDescending(e => e.HireDate)
                        : query.OrderBy(e => e.HireDate);
                    break;

                default:
                    // no sorting if sortBy is null/invalid
                    break;
            }

            var employees = await query.ToListAsync();
            return Ok(employees);
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
            if (string.IsNullOrEmpty(employee.Name) || string.IsNullOrEmpty(employee.Email) || string.IsNullOrEmpty(employee.Department))
            {
                return BadRequest("Invalid Request");
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
            if (employee.Id == 0 || string.IsNullOrEmpty(employee.Name) || string.IsNullOrEmpty(employee.Email) || string.IsNullOrEmpty(employee.Department))
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
