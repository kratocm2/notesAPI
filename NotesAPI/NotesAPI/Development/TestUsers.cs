using Microsoft.EntityFrameworkCore;
using NotesAPI.Data;
using NotesAPI.Models;
using NotesAPI.Services;

namespace NotesAPI.Development;

public static class TestUsers
{
    public static async Task SeedAsync(
        NotesDbContext db,
        PasswordService passwordService)
    {
        var testUsers = new[]
        {
            new
            {
                Username = "testuserA",
                Password = "testuserA",
                Role = "User"
            },
            new
            {
                Username = "testuserB",
                Password = "testuserB",
                Role = "User"
            },
            new
            {
                Username = "testuserC",
                Password = "testuserC",
                Role = "User"
            },
            new
            {
                Username = "testadminA",
                Password = "testadminA",
                Role = "Admin"
            },
            new
            {
                Username = "testadminB",
                Password = "testadminB",
                Role = "Admin"
            },
            new
            {
                Username = "testmainadmin",
                Password = "testmainadmin",
                Role = "MainAdmin"
            }
        };

        foreach (var testUser in testUsers)
        {
            var user = await db.Users
                .FirstOrDefaultAsync(user =>
                    user.Username == testUser.Username);

            if (user == null)
            {
                db.Users.Add(new User
                {
                    Username = testUser.Username,
                    PasswordHash =
                        passwordService.HashPassword(
                            testUser.Password),
                    Role = testUser.Role
                });
            }
            else if (testUser.Username == "testmainadmin" &&
                     user.Role != "MainAdmin")
            {
                user.Role = "MainAdmin";
            }
        }

        await db.SaveChangesAsync();
    }
}