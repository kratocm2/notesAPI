using Microsoft.EntityFrameworkCore;
using NotesAPI.Data;
using NotesAPI.DTOs;
using System.Security.Claims;

namespace NotesAPI.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(
        this WebApplication app)
    {
        // All endpoints in this group require the Admin or MainAdmin role.
        var adminApi = app.MapGroup("/admin")
            .RequireAuthorization("AdminOnly");

        // =========================
        // GET /admin/users
        // =========================

        adminApi.MapGet("/users", async (
            NotesDbContext db) =>
        {
            // Return only the user information needed by the admin panel.
            var users = await db.Users
                .Select(user => new
                {
                    user.Id,
                    user.Username,
                    user.Role
                })
                .OrderBy(user => user.Id)
                .ToListAsync();

            return Results.Ok(users);
        });

        // =========================
        // PUT /admin/users/{id}/role
        // =========================

        adminApi.MapPut("/users/{id}/role", async (
            int id,
            ChangeUserRoleRequest request,
            NotesDbContext db,
            ClaimsPrincipal currentUser) =>
        {
            // MainAdmin cannot be assigned through the API.
            if (request.Role != "User" &&
                request.Role != "Admin")
            {
                return Results.BadRequest(new
                {
                    error =
                        "Role must be either User or Admin."
                });
            }

            var currentUserId = int.Parse(
                currentUser.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

            var currentUserRole =
                currentUser.FindFirstValue(
                    ClaimTypes.Role);

            var user = await db.Users
                .FirstOrDefaultAsync(user =>
                    user.Id == id);

            if (user == null)
                return Results.NotFound();

            // Nobody can modify a MainAdmin.
            if (user.Role == "MainAdmin")
            {
                return Results.BadRequest(new
                {
                    error =
                        "MainAdmin role cannot be changed through the API."
                });
            }

            // Nobody can change their own role.
            if (user.Id == currentUserId)
            {
                return Results.BadRequest(new
                {
                    error =
                        "You cannot change your own role."
                });
            }

            // Only MainAdmin can modify an existing Admin.
            if (user.Role == "Admin" &&
                currentUserRole != "MainAdmin")
            {
                return Results.Forbid();
            }

            user.Role = request.Role;

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                user.Id,
                user.Username,
                user.Role
            });
        })
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);

        // =========================
        // DELETE /admin/users/{id}
        // =========================

        adminApi.MapDelete("/users/{id}", async (
            int id,
            NotesDbContext db,
            ClaimsPrincipal currentUser) =>
        {
            var currentUserId = int.Parse(
                currentUser.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

            var currentUserRole =
                currentUser.FindFirstValue(
                    ClaimTypes.Role);

            var user = await db.Users
                .FirstOrDefaultAsync(user =>
                    user.Id == id);

            if (user == null)
                return Results.NotFound();

            // Nobody can delete themselves.
            if (user.Id == currentUserId)
            {
                return Results.BadRequest(new
                {
                    error =
                        "You cannot delete your own account."
                });
            }

            // Nobody can delete a MainAdmin.
            if (user.Role == "MainAdmin")
            {
                return Results.BadRequest(new
                {
                    error =
                        "MainAdmin accounts cannot be deleted through the API."
                });
            }

            // Only MainAdmin can delete an existing Admin.
            if (user.Role == "Admin" &&
                currentUserRole != "MainAdmin")
            {
                return Results.Forbid();
            }

            db.Users.Remove(user);

            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }
}