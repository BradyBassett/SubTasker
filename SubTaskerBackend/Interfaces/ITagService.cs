using SubTaskerBackend.DTOs.Tags;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Interfaces
{
    public interface ITagService
    {
        Task<List<Tag>> GetAllTagsAsync();

        Task<Tag> GetTagByIdAsync(int id);

        Task<Tag> CreateTagAsync(TagWriteDto tagCreateDto);

        Task<Tag> UpdateTagAsync(int id, TagWriteDto tagUpdateDto);

        Task DeleteTagAsync(int id);
    }
}