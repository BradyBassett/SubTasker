using SubTaskerBackend.Models;
using SubTaskerBackend.Mappers;

namespace SubTaskerBackend.Tests.Unit.Mappers;

public class TagMapperTests
{
    [Fact]
    public void ToDto_WithValidTag_ShouldMapAllProperties()
    {
        var tag = new Tag
        {
            Id = 1,
            Name = "Test Tag",
            UserId = 1111111111,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = tag.ToDto();

        Assert.Equal(tag.Id, result.Id);
        Assert.Equal(tag.Name, result.Name);
        Assert.Equal(tag.UserId, result.UserId);
        Assert.Equal(tag.CreatedAt, result.CreatedAt);
        Assert.Equal(tag.UpdatedAt, result.UpdatedAt);
    }

    [Fact]
    public void ToDtoList_WithMultipleTags_ShouldMapAllTags()
    {
        var tags = new List<Tag>
        {
            new Tag
            {
                Id = 1,
                Name = "Test Tag 1",
                UserId = 1111111111,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Tag
            {
                Id = 2,
                Name = "Test Tag 2",
                UserId = 222222222,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Tag
            {
                Id = 3,
                Name = "Test Tag 3",
                UserId = 333333333,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var result = tags.ToDtoList();

        Assert.Equal(tags.Count, result.Count);
        for (int i = 0; i < tags.Count; i++)
        {
            Assert.Equal(tags[i].Id, result.ElementAt(i).Id);
            Assert.Equal(tags[i].Name, result.ElementAt(i).Name);
            Assert.Equal(tags[i].UserId, result.ElementAt(i).UserId);
            Assert.Equal(tags[i].CreatedAt, result.ElementAt(i).CreatedAt);
            Assert.Equal(tags[i].UpdatedAt, result.ElementAt(i).UpdatedAt);
        }
    }
}
