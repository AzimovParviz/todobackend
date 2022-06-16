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
var userConnectionString = builder.Configuration.GetConnectionString("UserConnection");
builder.Services.AddDbContext<TodoAPI.Notes.NoteDb>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDbContext<TodoAPI.Users.UserDb>(options=>
    options.UseNpgsql(userConnectionString));
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

app.MapGet("/api/v1", () => "Welcome to Notes API!");
app.MapGet("/api/v1/notes", [Authorize] async (TodoAPI.Notes.NoteDb db) => await db.Notes.ToListAsync());

/* POST request for the Note(Todo) item */
app.MapPost("/api/v1/notes/", [Authorize] async(TodoAPI.DTOs.Note n, TodoAPI.Notes.NoteDb db)=> {
    db.Notes.Add(n);
    await db.SaveChangesAsync();

    return Results.Created($"/notes/{n.id}", n);
});

/* GET request by ID of the note*/
app.MapGet("/api/v1/notes/{id:int}", [Authorize] async(int id, TodoAPI.Notes.NoteDb db)=> {
    return await db.Notes.FindAsync(id)
        is TodoAPI.DTOs.Note n
        ? Results.Ok(n)
        : Results.NotFound();
});
/* GET by status of the todo */
app.MapGet("/api/v1/notes/status/{status:int}", [Authorize] async(int status, TodoAPI.Notes.NoteDb db)=> {
    string query  = "SELECT * FROM \"Notes\" WHERE status="+status;
    var note = db.Notes
    .FromSqlRaw(query)
    .ToList();
    return note;
});

/* PUT request to update the note*/
app.MapPut("/api/v1/notes/{id:int}", [Authorize] async(int id, TodoAPI.DTOs.Note n, TodoAPI.Notes.NoteDb db)=> {
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
    note.updated = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok();
});

/* DELETE request */
app.MapDelete("/api/v1/notes/{id:int}", [Authorize] async(int id, TodoAPI.Notes.NoteDb db)=>{
    var note = await db.Notes.FindAsync(id);
    if (note is not null) {
        db.Notes.Remove(note);
        await db.SaveChangesAsync();
    }
    return Results.NoContent();
});

/* SIGNIN request */
/* https://dev.to/moe23/net-6-minimal-api-authentication-jwt-with-swagger-and-open-api-2chh */
app.MapPost("/api/v1/signin", [AllowAnonymous] (TodoAPI.DTOs.User user, TodoAPI.Users.UserDb db) => {
    TodoAPI.DTOs.User loginattempt = db.Users.SingleOrDefault(u => u.username == user.username);
    /*
    if the user is not found it will return 401
    */
    if(user.username == loginattempt.username
    && loginattempt.password == Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: user.password,
            salt: loginattempt.salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8)))
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

/* sign the user up, requires to be logged in */
app.MapPost("/api/v1/signup", [AllowAnonymous] async (TodoAPI.DTOs.User user, TodoAPI.Users.UserDb db)=> {
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
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/users/{user.userId}", user);
});
/* PUT request to change password of a user */
app.MapPut("/api/v1/changePassword", [Authorize] async (TodoAPI.DTOs.passwordChange pc, TodoAPI.Users.UserDb db)=> {
    TodoAPI.DTOs.User foundUser = db.Users.SingleOrDefault(u => u.username == pc.username);
    if (foundUser is null)
    {
        return Results.NotFound();
    }
    var updatedUser = await db.Users.FindAsync(foundUser.userId);
    //if note is found then
    string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: pc.newPassword,
            salt: foundUser.salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));
    updatedUser.password = hashed;
    updatedUser.userUpdated = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.Ok();
});

app.Run();