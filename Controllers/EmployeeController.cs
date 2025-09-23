using EmployeeManagementAPI.Data;
using EmployeeManagementAPI.Models;
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
        public async Task<List<Employee>> Get()
        {
            return await _dbContext.Employees.ToListAsync();
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

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] Employee employee)
        {
            if (string.IsNullOrEmpty(employee.Name) || string.IsNullOrEmpty(employee.Email) || string.IsNullOrEmpty(employee.Department))
            {
                return BadRequest("Invalid Request");
            }

            await _dbContext.Employees.AddAsync(employee);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
        }

        [HttpPut]
        public async Task<ActionResult> Update([FromBody] Employee employee)
        {
            if (employee.Id == 0 || string.IsNullOrEmpty(employee.Name) || string.IsNullOrEmpty(employee.Email) || string.IsNullOrEmpty(employee.Department))
            {
                return BadRequest("Invalid Request");
            }

            _dbContext.Employees.Update(employee);
            await _dbContext.SaveChangesAsync();

            return Ok();
        }

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
