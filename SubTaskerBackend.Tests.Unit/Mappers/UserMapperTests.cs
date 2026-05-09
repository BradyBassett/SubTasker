using SubTaskerBackend.Models;
using SubTaskerBackend.Mappers;

namespace SubTaskerBackend.Tests.Unit.Mappers;

public class UserMapperTests
{
    [Fact]
    public void ToDto_WithValidUser_ShouldMapAllProperties()
    {
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = user.ToDto();

        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Username, result.Username);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.CreatedAt, result.CreatedAt);
        Assert.Equal(user.UpdatedAt, result.UpdatedAt);
    }

    [Fact]
    public void ToDtoList_WithMultipleUsers_ShouldMapAllUsers()
    {
        var users = new List<User>
        {
            new User
            {
                Id = 1,
                Username = "testuser1",
                Email = "test1@example.com",
                PasswordHash = "hashedpassword1",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = 2,
                Username = "testuser2",
                Email = "test2@example.com",
                PasswordHash = "hashedpassword2",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = 3,
                Username = "testuser3",
                Email = "test3@example.com",
                PasswordHash = "hashedpassword3",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var result = users.ToDtoList();

        Assert.Equal(users.Count, result.Count);
        for (int i = 0; i < users.Count; i++)
        {
            Assert.Equal(users[i].Id, result.ElementAt(i).Id);
            Assert.Equal(users[i].Username, result.ElementAt(i).Username);
            Assert.Equal(users[i].Email, result.ElementAt(i).Email);
            Assert.Equal(users[i].CreatedAt, result.ElementAt(i).CreatedAt);
            Assert.Equal(users[i].UpdatedAt, result.ElementAt(i).UpdatedAt);
        }
    }
}
