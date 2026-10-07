using Microsoft.EntityFrameworkCore;
using NotesAPI.Data;
using NotesAPI.DTOs;
using NotesAPI.Models;
using NotesAPI.Services;

namespace NotesAPI.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(
        this WebApplication app)
    {
        var authApi = app.MapGroup("/auth");

        // =========================
        // POST /auth/register
        // =========================

        authApi.MapPost("/register", async (
            RegisterRequest request,
            NotesDbContext db,
            PasswordService passwordService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return Results.BadRequest(new
                {
                    error = "Username is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new
                {
                    error = "Password is required."
                });
            }

            if (request.Username.Length > 50)
            {
                return Results.BadRequest(new
                {
                    error =
                        "Username cannot exceed 50 characters."
                });
            }

            if (request.Password.Length < 8)
            {
                return Results.BadRequest(new
                {
                    error =
                        "Password must be at least 8 characters."
                });
            }

            var usernameExists = await db.Users
                .AnyAsync(user =>
                    user.Username == request.Username);

            if (usernameExists)
            {
                return Results.BadRequest(new
                {
                    error = "Username is already taken."
                });
            }

            var user = new User
            {
                Username = request.Username,

                // Store a secure password hash instead of the original password.
                PasswordHash =
                    passwordService.HashPassword(
                        request.Password),

                // New accounts always start with the User role.
                Role = "User"
            };

            db.Users.Add(user);

            await db.SaveChangesAsync();

            return Results.Created(
                $"/users/{user.Id}",
                new
                {
                    user.Id,
                    user.Username
                });
        });

        // =========================
        // POST /auth/login
        // =========================

        authApi.MapPost("/login", async (
            LoginRequest request,
            NotesDbContext db,
            PasswordService passwordService,
            JwtService jwtService) =>
        {
            var user = await db.Users
                .FirstOrDefaultAsync(user =>
                    user.Username == request.Username);

            if (user == null)
            {
                return Results.Unauthorized();
            }

            // Verify the entered password against the stored hash.
            var passwordValid =
                passwordService.VerifyPassword(
                    request.Password,
                    user.PasswordHash);

            if (!passwordValid)
            {
                return Results.Unauthorized();
            }

            // Create a JWT containing the authenticated user's identity and role.
            var token = jwtService.CreateToken(user);

            return Results.Ok(new LoginResponse
            {
                Token = token
            });
        });
    }
}