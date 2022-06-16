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

//Lists all of posted notes
TodoAPI.Endpoints.NoteMethods.ListNotes(app);
/* POST request for the Note(Todo) item */
TodoAPI.Endpoints.NoteMethods.PostNotes(app);

/* GET request by ID of the note*/
TodoAPI.Endpoints.NoteMethods.FindById(app);
/* GET by status of the todo */
TodoAPI.Endpoints.NoteMethods.FindByStatus(app);

/* PUT request to update the note*/
TodoAPI.Endpoints.NoteMethods.UpdateNote(app);
/* DELETE request */
TodoAPI.Endpoints.NoteMethods.DeleteNote(app);

/* SIGNIN request */
/* https://dev.to/moe23/net-6-minimal-api-authentication-jwt-with-swagger-and-open-api-2chh */
TodoAPI.Endpoints.UserMethods.SignIn(app, builder);
/* sign the user up, requires to be logged in */
TodoAPI.Endpoints.UserMethods.SignUp(app);
/* PUT request to change password of a user */
TodoAPI.Endpoints.UserMethods.ChangePassword(app);

app.Run();