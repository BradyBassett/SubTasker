using System.Text.Json.Serialization;

namespace SubTaskerBackend.DTOs.TaskItems
{
    public class TaskItemUpdateDto
    {
        public string? Title { get; set; }

        private string? _description;
        private DateTime? _dueDate;
        private int? _categoryId;
        private int? _parentTaskId;
        private bool _descriptionSpecified;
        private bool _dueDateSpecified;
        private bool _categoryIdSpecified;
        private bool _parentTaskIdSpecified;

        public string? Description
        {
            get => _description;
            set
            {
                _description = value;
                _descriptionSpecified = true;
            }
        }

        public Enums.TaskStatus? Status { get; set; }

        public Enums.PriorityLevel? Priority { get; set; }

        public DateTime? DueDate
        {
            get => _dueDate;
            set
            {
                _dueDate = value;
                _dueDateSpecified = true;
            }
        }

        public int? CategoryId
        {
            get => _categoryId;
            set
            {
                _categoryId = value;
                _categoryIdSpecified = true;
            }
        }

        public List<int>? TagIds { get; set; }

        public int? ParentTaskId
        {
            get => _parentTaskId;
            set
            {
                _parentTaskId = value;
                _parentTaskIdSpecified = true;
            }
        }

        public List<int>? SubTaskIds { get; set; }

        [JsonIgnore]
        public bool DescriptionSpecified => _descriptionSpecified;

        [JsonIgnore]
        public bool DueDateSpecified => _dueDateSpecified;

        [JsonIgnore]
        public bool CategoryIdSpecified => _categoryIdSpecified;

        [JsonIgnore]
        public bool ParentTaskIdSpecified => _parentTaskIdSpecified;
    }
}