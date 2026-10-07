namespace NotesAPI.Models;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string Role { get; set; } = "User";

    public List<Note> Notes { get; set; } = new();

    public List<Category> Categories { get; set; } = new();
}