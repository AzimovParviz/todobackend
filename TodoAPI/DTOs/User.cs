using System.ComponentModel.DataAnnotations;

namespace TodoAPI.DTOs
{
    record User (string username)
        {
            [Key]
            public int userId { get; set; }
            public byte[] salt { get; set; }
            public string password { get; set; }
            public string email { get; set; } = default!;
            public DateTime userCreated { get; set; } = DateTime.UtcNow;
            public DateTime userUpdated { get; set; } = default!;
        }
}