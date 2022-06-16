using Microsoft.EntityFrameworkCore; // place this line at the beginning of file.
using Microsoft.AspNetCore.Authorization;

namespace TodoAPI.Endpoints
{
    public static class NoteMethods
    {
        public static void ListNotes(WebApplication app)
        {
            app.MapGet("/api/v1/notes", [Authorize] async (TodoAPI.Notes.NoteDb db) => await db.Notes.ToListAsync());
        }

        public static void PostNotes(WebApplication app)
        {
            app.MapPost("/api/v1/notes/", [Authorize] async (TodoAPI.DTOs.Note n, TodoAPI.Notes.NoteDb db) =>
            {
                db.Notes.Add(n);
                await db.SaveChangesAsync();

                return Results.Created($"/notes/{n.id}", n);
            });
        }

        public static void FindById(WebApplication app)
        {
            app.MapGet("/api/v1/notes/{id:int}", [Authorize] async (int id, TodoAPI.Notes.NoteDb db) =>
            {
                return await db.Notes.FindAsync(id)
                    is TodoAPI.DTOs.Note n
                    ? Results.Ok(n)
                    : Results.NotFound();
            });
        }

        public static void FindByStatus(WebApplication app)
        {
            app.MapGet("/api/v1/notes/status/{status:int}", [Authorize] async (int status, TodoAPI.Notes.NoteDb db) =>
            {
                string query = "SELECT * FROM \"Notes\" WHERE status=" + status;
                var note = db.Notes
                .FromSqlRaw(query)
                .ToList();
                return note;
            });
        }

        public static void UpdateNote(WebApplication app)
        {
            app.MapPut("/api/v1/notes/{id:int}", [Authorize] async (int id, TodoAPI.DTOs.Note n, TodoAPI.Notes.NoteDb db) =>
            {
                if (n.id != id)
                {
                    return Results.BadRequest();
                }

                var note = await db.Notes.FindAsync(id);
                if (note is null) return Results.NotFound();
                //if note is found then
                note.text = n.text;
                note.status = n.status;
                note.name = n.name;
                note.userId = n.userId;
                note.updated = DateTime.UtcNow;
                await db.SaveChangesAsync();
                return Results.Ok();
            });
        }

        public static void DeleteNote(WebApplication app)
        {
            app.MapDelete("/api/v1/notes/{id:int}", [Authorize] async (int id, TodoAPI.Notes.NoteDb db) =>
            {
                var note = await db.Notes.FindAsync(id);
                if (note is not null)
                {
                    db.Notes.Remove(note);
                    await db.SaveChangesAsync();
                }
                return Results.NoContent();
            });
        }
    }
}