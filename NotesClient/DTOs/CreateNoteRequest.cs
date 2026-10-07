namespace NotesClient.DTOs;

public class CreateNoteRequest
{
    public string Title { get; set; } = "";

    public string Content { get; set; } = "";

    public int? CategoryId { get; set; }
}