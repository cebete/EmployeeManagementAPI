using EmployeeManagementAPI.Handlers;
using EmployeeManagementAPI.Models.Entities;
using Microsoft.EntityFrameworkCore;



namespace EmployeeManagementAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
            
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<UserAccount> UserAccounts { get; set; }
    }
}
