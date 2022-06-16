using Microsoft.EntityFrameworkCore; // place this line at the beginning of file.

namespace TodoAPI.Users
{
    class UserDb: DbContext {
    public UserDb(DbContextOptions<UserDb> options): base(options) {

    }
    public DbSet<TodoAPI.DTOs.User> Users => Set<TodoAPI.DTOs.User>();
}
}