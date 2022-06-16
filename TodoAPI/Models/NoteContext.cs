using Microsoft.EntityFrameworkCore; // place this line at the beginning of file.

namespace TodoAPI.Notes
{
    class NoteDb: DbContext {
        public NoteDb(DbContextOptions<NoteDb> options): base(options) {

        }
        public DbSet<TodoAPI.DTOs.Note> Notes => Set<TodoAPI.DTOs.Note>();
    }

}