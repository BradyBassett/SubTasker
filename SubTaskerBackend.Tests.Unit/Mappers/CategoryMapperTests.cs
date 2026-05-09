using SubTaskerBackend.Models;
using SubTaskerBackend.Mappers;

namespace SubTaskerBackend.Tests.Unit.Mappers;

public class CategoryMapperTests
{
    [Fact]
    public void ToDto_WithValidCategory_ShouldMapAllProperties()
    {
        var category = new Category
        {
            Id = 1,
            Name = "Test Category",
            UserId = 1111111111,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = category.ToDto();

        Assert.Equal(category.Id, result.Id);
        Assert.Equal(category.Name, result.Name);
        Assert.Equal(category.UserId, result.UserId);
        Assert.Equal(category.CreatedAt, result.CreatedAt);
        Assert.Equal(category.UpdatedAt, result.UpdatedAt);
    }

    [Fact]
    public void ToDtoList_WithMultipleCategories_ShouldMapAllCategories()
    {
        var categories = new List<Category>
        {
            new Category
            {
                Id = 1,
                Name = "Test Category 1",
                UserId = 1111111111,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category
            {
                Id = 2,
                Name = "Test Category 2",
                UserId = 222222222,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Category
            {
                Id = 3,
                Name = "Test Category 3",
                UserId = 333333333,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        var result = categories.ToDtoList();

        Assert.Equal(categories.Count, result.Count);
        for (int i = 0; i < categories.Count; i++)
        {
            Assert.Equal(categories[i].Id, result.ElementAt(i).Id);
            Assert.Equal(categories[i].Name, result.ElementAt(i).Name);
            Assert.Equal(categories[i].UserId, result.ElementAt(i).UserId);
            Assert.Equal(categories[i].CreatedAt, result.ElementAt(i).CreatedAt);
            Assert.Equal(categories[i].UpdatedAt, result.ElementAt(i).UpdatedAt);
        }
    }
}
