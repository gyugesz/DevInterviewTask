using System;

namespace TodoWpfClient.Models
{
    public class TodoItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }          
        public int Priority { get; set; }                 
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.MinValue;
    }
}
