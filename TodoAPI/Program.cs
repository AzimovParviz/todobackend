using Microsoft.EntityFrameworkCore; // place this line at the beginning of file.
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Connect to PostgreSQL Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<NoteDb>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDbContext<UserDb>(options=> 
    options.UseNpgsql(connectionString));
// Add JWT configuration
builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
       ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey
            (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
    };
});

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => "Welcome to Notes API!");
app.MapGet("/notes", [Authorize] async (NoteDb db) => await db.Notes.ToListAsync());

/* POST request for the Note(Todo) item */
app.MapPost("/notes/", [Authorize] async(Note n, NoteDb db)=> {
    db.Notes.Add(n);
    await db.SaveChangesAsync();

    return Results.Created($"/notes/{n.id}", n);
});

/* GET request */
app.MapGet("/notes/{id:int}", [Authorize] async(int id, NoteDb db)=> {
    return await db.Notes.FindAsync(id)
        is Note n
        ? Results.Ok(n)
        : Results.NotFound();
});

/* UPDATE request */
app.MapPut("/notes/{id:int}", [Authorize] async(int id, Note n, NoteDb db)=> {
    if (n.id != id)
    {
        return Results.BadRequest();
    }

    var note = await db.Notes.FindAsync(id);
    if(note is null) return Results.NotFound();

    //if note is found then
    note.text = n.text;
    note.status = n.status;
    note.name = n.name;
    note.userId = n.userId;
    note.created = DateTime.Now;
    await db.SaveChangesAsync();
    return Results.Ok();
});

/* DELETE request */
app.MapDelete("/notes/{id:int}", [Authorize] async(int id, NoteDb db)=>{
    var note = await db.Notes.FindAsync(id);
    if (note is not null) {
        db.Notes.Remove(note);
        await db.SaveChangesAsync();
    }
    return Results.NoContent();
});

app.MapPost("/signin", [AllowAnonymous] (UserDto user) => {
    if(user.username == "admin" && user.password == "admin")
    {
        var secureKey = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);

        var issuer = builder.Configuration["Jwt:Issuer"];
        var audience = builder.Configuration["Jwt:Audience"];
        var securityKey = new SymmetricSecurityKey(secureKey);
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512);

        var jwtTokenHandler = new JwtSecurityTokenHandler();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new [] {
                new Claim("Id", "1"),
                new Claim(JwtRegisteredClaimNames.Sub, user.username),
                new Claim(JwtRegisteredClaimNames.Email, user.username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            Expires = DateTime.Now.AddMinutes(5),
            Audience = audience,
            Issuer = issuer,
            SigningCredentials = credentials
        };

        var token = jwtTokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = jwtTokenHandler.WriteToken(token);
        return Results.Ok(jwtToken);  
    }
    return Results.Unauthorized();
});

app.MapPost("/signup", [Authorize] async (UserDto user, UserDb db)=> {
    // generate a 128-bit salt using a cryptographically strong random sequence of nonzero values
        user.salt = new byte[128 / 8];
        using (var rngCsp = new RNGCryptoServiceProvider())
        {
            rngCsp.GetNonZeroBytes(user.salt);
        }
        user.password = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: user.password,
            salt: user.salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));
    db.UserDtos.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/users/{user.userId}", user);
});

app.Run();
record UserDto (string username)
{
    [Key]
    public int userId { get; set; }
    public byte[] salt { get; set; }
    public string password { get; set; }
    public string email { get; set; } = default!;

    public DateTime userCreated { get; set; } = default!;

    public DateTime userUpdated { get; set; } = default!;
}
/*
User:
● Id: Unique identifier
● Email: Email address
● Password: Hash of the password
● Created timestamp: When the user is created
● Updated timestamp: When the user is last updated

*/

record Note(int id){
    public string text {get;set;} = default!;
    public string name { get; set; } = default!;
    public string status {get;set;} = default!;//needs to be enum

    public int userId { get; set; } = default!;

    public DateTime created { get; set; } = default!;

    public DateTime updated { get; set; } = default!;
}
/*
Todo:
● Id: Unique identifier
● Name: Name of the todo item
● Description (optional): Description of the toto item
● User id: Id of the user who owns this todo item
● Created timestamp: When the item is created
● Updated timestamp: When the item is last updated
● Status: An enum of either: NotStarted, OnGoing, Completed
*/

class NoteDb: DbContext {
    public NoteDb(DbContextOptions<NoteDb> options): base(options) {

    }
    public DbSet<Note> Notes => Set<Note>();
}
class UserDb: DbContext {
    public UserDb(DbContextOptions<UserDb> options): base(options) {

    }
    public DbSet<UserDto> UserDtos => Set<UserDto>();
}