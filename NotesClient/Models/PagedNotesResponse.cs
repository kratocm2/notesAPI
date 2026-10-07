namespace NotesClient.Models;

public class PagedNotesResponse
{
    public List<Note> Items { get; set; } = new();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }
}