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

namespace TodoAPI.Endpoints
{
    public static class UserMethods
    {
        public static void SignIn(WebApplication app, WebApplicationBuilder builder)
        {
            app.MapPost("/api/v1/signin", [AllowAnonymous] (TodoAPI.DTOs.User user, TodoAPI.Users.UserDb db) =>
            {
                TodoAPI.DTOs.User loginattempt = db.Users.SingleOrDefault(u => u.username == user.username);
                /*
                if the user is not found it will return 401
                */
                if (user.username == loginattempt.username
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
                        Subject = new ClaimsIdentity(new[] {
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
        }

        public static void SignUp(WebApplication app)
        {
            app.MapPost("/api/v1/signup", [AllowAnonymous] async (TodoAPI.DTOs.User user, TodoAPI.Users.UserDb db) =>
            {
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
        }

        public static void ChangePassword(WebApplication app)
        {
            app.MapPut("/api/v1/changePassword", [Authorize] async (TodoAPI.DTOs.passwordChange pc, TodoAPI.Users.UserDb db) =>
            {
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
        }
    }
}