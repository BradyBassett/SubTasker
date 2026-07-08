using Microsoft.AspNetCore.Mvc;
using SubTaskerBackend.DTOs.Tags;
using SubTaskerBackend.Interfaces;
using SubTaskerBackend.Mappers;
using SubTaskerBackend.Models;

namespace SubTaskerBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagController : ControllerBase
    {
        private readonly ITagService _tagService;

        public TagController(ITagService tagService)
        {
            _tagService = tagService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTags()
        {
            List<Tag> tags = await _tagService.GetAllTagsAsync();

            return Ok(tags.ToDtoList());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTagById(int id)
        {
            Tag tag = await _tagService.GetTagByIdAsync(id);

            return Ok(tag.ToDto());
        }

        [HttpGet("{tagId}/tasks")]
        public async Task<IActionResult> GetTasksByTagId(int tagId)
        {
            Tag tag = await _tagService.GetTagByIdAsync(tagId);

            return Ok(tag.Tasks.ToDtoList());
        }

        [HttpPost]
        public async Task<IActionResult> CreateTag([FromBody] TagWriteDto tagCreateDto)
        {
            Tag tag = await _tagService.CreateTagAsync(tagCreateDto);

            return CreatedAtAction(nameof(GetTagById), new { id = tag.Id }, tag.ToDto());
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateTag(int id, [FromBody] TagWriteDto tagUpdateDto)
        {
            Tag tag = await _tagService.UpdateTagAsync(id, tagUpdateDto);

            return Ok(tag.ToDto());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTag(int id)
        {
            await _tagService.DeleteTagAsync(id);

            return NoContent();
        }
    }
}