using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gringotts.Domain.Entities;

public class Transaction
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public Guid Id { get; set; }

  [Required]
  public DateTime Date { get; set; }

  [ForeignKey("SenderCustomer")]
  public long? SenderId { get; set; }

  [Required]
  [ForeignKey("RecipientCustomer")]
  public long RecipientId { get; set; }

  [ForeignKey(nameof(Employee))]
  public Guid? EmployeeId { get; set; }

  [Column(TypeName = "decimal(18,2)")]
  public decimal Amount { get; set; }

  [MaxLength(500)]
  public string Description { get; set; } = string.Empty;
}
