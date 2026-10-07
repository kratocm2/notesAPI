using NotesAPI.Models;

namespace NotesAPI.DTOs;

public static class ResponseMapper
{
    public static NoteResponse ToResponse(
        this Note note)
    {
        return new NoteResponse
        {
            Id = note.Id,
            Title = note.Title,
            Content = note.Content,
            CreatedAt = note.CreatedAt,
            LastChangedAt = note.LastChangedAt,
            CategoryId = note.CategoryId
        };
    }


    public static CategoryResponse ToResponse(
        this Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name
        };
    }
}