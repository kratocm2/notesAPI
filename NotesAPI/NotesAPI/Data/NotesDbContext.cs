using Microsoft.EntityFrameworkCore;
using NotesAPI.Models;

namespace NotesAPI.Data;

public class NotesDbContext : DbContext
{
    public NotesDbContext(
        DbContextOptions<NotesDbContext> options)
        : base(options)
    {
    }

    public DbSet<Note> Notes => Set<Note>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // A user's notes are deleted automatically when the user is deleted.
        modelBuilder.Entity<Note>()
            .HasOne(note => note.User)
            .WithMany(user => user.Notes)
            .HasForeignKey(note => note.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // A user's categories are deleted automatically when the user is deleted.
        modelBuilder.Entity<Category>()
            .HasOne(category => category.User)
            .WithMany(user => user.Categories)
            .HasForeignKey(category => category.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}