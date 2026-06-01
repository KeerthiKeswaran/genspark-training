using System.ComponentModel.DataAnnotations;

namespace server.Core.Entities
{
    public class PlatformSetting
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public string Key { get; set; } = string.Empty;
        
        [Required]
        public string Value { get; set; } = string.Empty;
    }
}
