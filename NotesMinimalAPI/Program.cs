using Microsoft.EntityFrameworkCore; // place this line at the beginning of file.


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Connect to PostgreSQL Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<NoteDb>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Welcome to Notes API!");
app.MapGet("/notes", async (NoteDb db) => await db.Notes.ToListAsync());

/* POST request */
app.MapPost("/notes/", async(Note n, NoteDb db)=> {
    db.Notes.Add(n);
    await db.SaveChangesAsync();

    return Results.Created($"/notes/{n.id}", n);
});

/* GET request */
app.MapGet("/notes/{id:int}", async(int id, NoteDb db)=> {
    return await db.Notes.FindAsync(id)
        is Note n
        ? Results.Ok(n)
        : Results.NotFound();
});

/* UPDATE request */
app.MapPut("/notes/{id:int}", async(int id, Note n, NoteDb db)=> {
    if (n.id != id)
    {
        return Results.BadRequest();
    }

    var note = await db.Notes.FindAsync(id);
    if(note is null) return Results.NotFound();

    //if note is found then
    note.text = n.text;
    note.done = n.done;
    await db.SaveChangesAsync();
    return Results.Ok();
});

/* DELETE request */
app.MapDelete("/notes/{id:int}", async(int id, NoteDb db)=>{
    var note = await db.Notes.FindAsync(id);
    if (note is not null) {
        db.Notes.Remove(note);
        await db.SaveChangesAsync();
    }
    return Results.NoContent();
});

app.Run();

record Note(int id){
    public string text {get;set;} = default!;
    public bool done {get;set;} = default!;
}

class NoteDb: DbContext {
    public NoteDb(DbContextOptions<NoteDb> options): base(options) {

    }
    public DbSet<Note> Notes => Set<Note>();
}