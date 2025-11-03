using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gringotts.Domain.Entities;

public class Customer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PersonalName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string CharacterName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; }

    public override string ToString()
    {
        var displayName = string.IsNullOrWhiteSpace(CharacterName) ? PersonalName : CharacterName;
        return $"{displayName} ({UserName})";
    }
}
