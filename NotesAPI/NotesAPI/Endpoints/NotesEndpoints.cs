using Microsoft.EntityFrameworkCore;
using NotesAPI.Data;
using NotesAPI.DTOs;
using NotesAPI.Models;
using NotesAPI.Validation;
using System.Security.Claims;

namespace NotesAPI.Endpoints;

public static class NotesEndpoints
{
    public static void MapNotesEndpoints(this WebApplication app)
    {
        // All note endpoints require an authenticated user.
        var notesApi = app.MapGroup("/notes")
            .RequireAuthorization();

        // =========================
        // GET /notes/
        // =========================

        notesApi.MapGet("/", async (
            string? search,
            int? categoryId,
            int? page,
            int? pageSize,
            string? sort,
            NotesDbContext db,
            ClaimsPrincipal user) =>
        {
            // Get the ID of the currently authenticated user.
            var userId = int.Parse(
                user.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

            // Start with only the notes belonging to the current user.
            var query = db.Notes
                .Where(note => note.UserId == userId)
                .AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(search))
            {
                // Add the search conditions to the existing database query.
                query = query.Where(note =>
                    note.Title.Contains(search) ||
                    note.Content.Contains(search));
            }

            // Category filter
            if (categoryId.HasValue)
            {
                // Limit the results to notes in the selected category.
                query = query.Where(note =>
                    note.CategoryId == categoryId.Value);
            }

            // Sorting
            query = sort?.ToLower() switch
            {
                "last_changed" =>
                    query.OrderByDescending(
                        note => note.LastChangedAt),

                "newest" =>
                    query.OrderByDescending(
                        note => note.CreatedAt),

                "oldest" =>
                    query.OrderBy(
                        note => note.CreatedAt),

                "title_asc" =>
                    query.OrderBy(
                        note => note.Title),

                "title_desc" =>
                    query.OrderByDescending(
                        note => note.Title),

                // Use last changed as the default sorting option.
                _ =>
                    query.OrderByDescending(
                        note => note.LastChangedAt)
            };

            // Count the results before pagination is applied.
            var totalItems = await query.CountAsync();

            var currentPage = page ?? 1;
            var currentPageSize = pageSize ?? 10;

            // Prevent invalid page numbers.
            if (currentPage < 1)
                currentPage = 1;

            // Prevent invalid page sizes.
            if (currentPageSize < 1)
                currentPageSize = 10;

            // Return only the notes belonging to the requested page.
            var notes = await query
                .Skip((currentPage - 1) * currentPageSize)
                .Take(currentPageSize)
                .ToListAsync();

            var noteResponses = notes
                .Select(note => note.ToResponse())
                .ToList();

            return Results.Ok(new
            {
                items = noteResponses,
                page = currentPage,
                pageSize = currentPageSize,
                totalItems
            });
        })
        .Produces(StatusCodes.Status200OK);

        // =========================
        // GET /notes/{id}
        // =========================

        notesApi.MapGet("/{id}", async (
            int id,
            NotesDbContext db,
            ClaimsPrincipal user) =>
        {
            // Get the ID of the currently authenticated user.
            var userId = int.Parse(
                user.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

            // Only find the note if it belongs to the current user.
            var note = await db.Notes
                .FirstOrDefaultAsync(note =>
                    note.Id == id &&
                    note.UserId == userId);

            if (note == null)
                return Results.NotFound();

            return Results.Ok(note.ToResponse());
        })
        .Produces<NoteResponse>(
            StatusCodes.Status200OK)
        .Produces(
            StatusCodes.Status404NotFound);

        // =========================
        // POST /notes/
        // =========================

        notesApi.MapPost("/", async (
            CreateNoteRequest request,
            NotesDbContext db,
            ClaimsPrincipal user) =>
        {
            if (!ValidationHelper.IsValid(
                    request,
                    out var errors))
            {
                return Results.BadRequest(new
                {
                    error = errors
                });
            }

            // Get the ID of the currently authenticated user.
            var userId = int.Parse(
                user.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

            if (request.CategoryId.HasValue)
            {
                // Verify that the selected category belongs to the current user.
                var categoryExists = await db.Categories
                    .AnyAsync(category =>
                        category.Id ==
                            request.CategoryId.Value &&
                        category.UserId == userId);

                if (!categoryExists)
                {
                    return Results.BadRequest(new
                    {
                        error =
                            "Category does not exist."
                    });
                }
            }

            // Use one timestamp for both creation and last change.
            var now = DateTime.UtcNow;

            var note = new Note
            {
                Title = request.Title,
                Content = request.Content,
                CreatedAt = now,
                LastChangedAt = now,
                CategoryId = request.CategoryId,
                UserId = userId
            };

            db.Notes.Add(note);

            await db.SaveChangesAsync();

            return Results.Created(
                $"/notes/{note.Id}",
                note.ToResponse());
        })
        .Produces<NoteResponse>(
            StatusCodes.Status201Created)
        .Produces(
            StatusCodes.Status400BadRequest);

        // =========================
        // PUT /notes/{id}
        // =========================

        notesApi.MapPut("/{id}", async (
            int id,
            UpdateNoteRequest request,
            NotesDbContext db,
            ClaimsPrincipal user) =>
        {
            if (!ValidationHelper.IsValid(
                    request,
                    out var errors))
            {
                return Results.BadRequest(new
                {
                    error = errors
                });
            }

            // Get the ID of the currently authenticated user.
            var userId = int.Parse(
                user.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

            // Only find notes owned by the current user.
            var note = await db.Notes
                .FirstOrDefaultAsync(note =>
                    note.Id == id &&
                    note.UserId == userId);

            if (note == null)
                return Results.NotFound();

            if (request.CategoryId.HasValue)
            {
                // Verify that the selected category belongs to the current user.
                var categoryExists = await db.Categories
                    .AnyAsync(category =>
                        category.Id ==
                            request.CategoryId.Value &&
                        category.UserId == userId);

                if (!categoryExists)
                {
                    return Results.BadRequest(new
                    {
                        error =
                            "Category does not exist."
                    });
                }
            }

            note.Title = request.Title;
            note.Content = request.Content;
            note.CategoryId = request.CategoryId;

            // Update the timestamp used by the last-changed sorting.
            note.LastChangedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(note.ToResponse());
        })
        .Produces<NoteResponse>(
            StatusCodes.Status200OK)
        .Produces(
            StatusCodes.Status400BadRequest)
        .Produces(
            StatusCodes.Status404NotFound);

        // =========================
        // DELETE /notes/{id}
        // =========================

        notesApi.MapDelete("/{id}", async (
            int id,
            NotesDbContext db,
            ClaimsPrincipal user) =>
        {
            // Get the ID of the currently authenticated user.
            var userId = int.Parse(
                user.FindFirstValue(
                    ClaimTypes.NameIdentifier)!);

            // Only find notes owned by the current user.
            var note = await db.Notes
                .FirstOrDefaultAsync(note =>
                    note.Id == id &&
                    note.UserId == userId);

            if (note == null)
                return Results.NotFound();

            db.Notes.Remove(note);

            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .Produces(
            StatusCodes.Status204NoContent)
        .Produces(
            StatusCodes.Status404NotFound);
    }
}