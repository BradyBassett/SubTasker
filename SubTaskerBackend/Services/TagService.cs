using SubTaskerBackend.Data;
using SubTaskerBackend.DTOs.Tags;
using SubTaskerBackend.Interfaces;
using SubTaskerBackend.Models;
using SubTaskerBackend.Utilities;
using Microsoft.EntityFrameworkCore;
using SubTaskerBackend.Exceptions;


namespace SubTaskerBackend.Services
{
	public class TagService : ITagService
    {
        private readonly SubTaskerEfCoreDbContext _dbContext;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public TagService(SubTaskerEfCoreDbContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<List<Tag>> GetAllTagsAsync()
        {
            int userId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);

            List<Tag> tags = await _dbContext.Tags
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.Name)
                .ToListAsync();

            return tags;
        }

        public async Task<Tag> GetTagByIdAsync(int id)
        {
            int userId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);

            Tag? tag = await _dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (tag == null)
            {
                throw new NotFoundException("Tag not found.");
            }

            return tag;
        }

        public async Task<Tag> CreateTagAsync(TagWriteDto tagCreateDto)
        {
            if (string.IsNullOrWhiteSpace(tagCreateDto.Name))
            {
                throw new BadRequestException("Tag name is required.");
            }

            int userId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);

            Tag tag = new Tag
            {
                Name = tagCreateDto.Name.Trim(),
                UserId = userId
            };

            if (await _dbContext.Tags.AnyAsync(t => t.Name == tag.Name && t.UserId == userId))
            {
                throw new ConflictException("A tag with the same name already exists.");
            }

            _dbContext.Tags.Add(tag);
            await _dbContext.SaveChangesAsync();

            return tag;
        }

        public async Task<Tag> UpdateTagAsync(int id, TagWriteDto tagUpdateDto)
        {
            if (string.IsNullOrWhiteSpace(tagUpdateDto.Name))
            {
                throw new BadRequestException("Tag name is required.");
            }

            int userId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);

            Tag? tag = await _dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (tag == null)
            {
                throw new NotFoundException("Tag not found.");
            }

            tag.Name = tagUpdateDto.Name.Trim();

            if (await _dbContext.Tags.AnyAsync(t => t.Name == tag.Name && t.UserId == userId && t.Id != id))
            {
                throw new ConflictException("A tag with the same name already exists.");
            }

            await _dbContext.SaveChangesAsync();

            return tag;
        }

        public async Task DeleteTagAsync(int id)
        {
            int userId = ClaimHelper.GetUserIdFromClaims(_httpContextAccessor);

            Tag? tag = await _dbContext.Tags.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (tag == null)
            {
                throw new NotFoundException("Tag not found.");
            }

            _dbContext.Tags.Remove(tag);
            await _dbContext.SaveChangesAsync();
        }
    }
}