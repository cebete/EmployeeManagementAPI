using EmployeeManagementAPI.Models;
using Microsoft.EntityFrameworkCore;



namespace EmployeeManagementAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
            
        }

        public DbSet<Employee> Employees { get; set; }
    }
}
