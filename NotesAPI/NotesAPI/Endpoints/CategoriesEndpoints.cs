using Microsoft.EntityFrameworkCore;
using NotesAPI.Data;
using NotesAPI.DTOs;
using NotesAPI.Models;
using System.Security.Claims;

namespace NotesAPI.Endpoints;

public static class CategoriesEndpoints
{
    public static void MapCategoriesEndpoints(
        this WebApplication app)
    {
        // All category endpoints require an authenticated user.
        var categoriesApi = app.MapGroup("/categories")
            .RequireAuthorization();

        // =========================
        // GET /categories/
        // =========================

        categoriesApi.MapGet("/", async (
            NotesDbContext db,
            ClaimsPrincipal user) =>
        {
            // Get the ID of the currently authenticated user.
            var userId = int.Parse(
                user.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

            // Only return categories belonging to the current user.
            var categories = await db.Categories
                .Where(category =>
                    category.UserId == userId)
                .OrderBy(category => category.Name)
                .ToListAsync();

            var categoryResponses = categories
                .Select(category => category.ToResponse())
                .ToList();

            return Results.Ok(categoryResponses);
        })
        .Produces<List<CategoryResponse>>(
            StatusCodes.Status200OK);

        // =========================
        // GET /categories/{id}
        // =========================

        categoriesApi.MapGet("/{id}", async (
            int id,
            NotesDbContext db,
            ClaimsPrincipal user) =>
        {
            // Get the ID of the currently authenticated user.
            var userId = int.Parse(
                user.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

            // Find the category only if it belongs to the current user.
            var category = await db.Categories
                .FirstOrDefaultAsync(category =>
                    category.Id == id &&
                    category.UserId == userId);

            if (category == null)
                return Results.NotFound();

            return Results.Ok(
                category.ToResponse());
        })
        .Produces<CategoryResponse>(
            StatusCodes.Status200OK)
        .Produces(
            StatusCodes.Status404NotFound);

        // =========================
        // POST /categories/
        // =========================

        categoriesApi.MapPost("/", async (
            Category category,
            NotesDbContext db,
            ClaimsPrincipal user) =>
        {
            if (string.IsNullOrWhiteSpace(category.Name))
            {
                return Results.BadRequest(new
                {
                    error = "Category name is required."
                });
            }

            if (category.Name.Length > 100)
            {
                return Results.BadRequest(new
                {
                    error =
                        "Category name cannot exceed 100 characters."
                });
            }

            // Get the ID of the currently authenticated user.
            var userId = int.Parse(
                user.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

            var newCategory = new Category
            {
                Name = category.Name,
                UserId = userId
            };

            db.Categories.Add(newCategory);

            await db.SaveChangesAsync();

            return Results.Created(
                $"/categories/{newCategory.Id}",
                newCategory.ToResponse());
        })
        .Produces<CategoryResponse>(
            StatusCodes.Status201Created)
        .Produces(
            StatusCodes.Status400BadRequest);

        // =========================
        // PUT /categories/{id}
        // =========================

        categoriesApi.MapPut("/{id}", async (
            int id,
            Category updatedCategory,
            NotesDbContext db,
            ClaimsPrincipal user) =>
        {
            if (string.IsNullOrWhiteSpace(
                    updatedCategory.Name))
            {
                return Results.BadRequest(new
                {
                    error = "Category name is required."
                });
            }

            if (updatedCategory.Name.Length > 100)
            {
                return Results.BadRequest(new
                {
                    error =
                        "Category name cannot exceed 100 characters."
                });
            }

            // Get the ID of the currently authenticated user.
            var userId = int.Parse(
                user.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

            // Only find categories owned by the current user.
            var category = await db.Categories
                .FirstOrDefaultAsync(category =>
                    category.Id == id &&
                    category.UserId == userId);

            if (category == null)
                return Results.NotFound();

            category.Name = updatedCategory.Name;

            await db.SaveChangesAsync();

            return Results.Ok(
                category.ToResponse());
        })
        .Produces<CategoryResponse>(
            StatusCodes.Status200OK)
        .Produces(
            StatusCodes.Status400BadRequest)
        .Produces(
            StatusCodes.Status404NotFound);

        // =========================
        // DELETE /categories/{id}
        // =========================

        categoriesApi.MapDelete("/{id}", async (
            int id,
            NotesDbContext db,
            ClaimsPrincipal user) =>
        {
            // Get the ID of the currently authenticated user.
            var userId = int.Parse(
                user.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

            // Load the category together with its notes before deletion.
            var category = await db.Categories
                .Include(category => category.Notes)
                .FirstOrDefaultAsync(category =>
                    category.Id == id &&
                    category.UserId == userId);

            if (category == null)
                return Results.NotFound();

            // Remove the category from its notes without deleting the notes.
            foreach (var note in category.Notes)
            {
                note.CategoryId = null;
            }

            db.Categories.Remove(category);

            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .Produces(
            StatusCodes.Status204NoContent)
        .Produces(
            StatusCodes.Status404NotFound);
    }
}