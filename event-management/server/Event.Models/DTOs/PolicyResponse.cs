using System;

namespace Event.Models.DTOs
{
    public class PolicyResponse
    {
        public int TermsId { get; set; }
        public string Version { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
