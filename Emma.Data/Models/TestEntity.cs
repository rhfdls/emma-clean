using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emma.Data.Models
{
    [Table("test_entities")]
    public class TestEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Column(TypeName = "timestamp with time zone")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
