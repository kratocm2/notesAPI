namespace NotesAPI.Models;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public List<Note> Notes { get; set; } = new();

    public int UserId { get; set; }

    public User User { get; set; } = null!;
}