using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentActivity
{
  [Key]
  public int DocumentActivityId { get; set; }

  public int DocumentId { get; set; }

  public int ActorUserId { get; set; }

  [Required, MaxLength(50)]
  public string Action { get; set; } = string.Empty;

  [MaxLength(1000)]
  public string? Details { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  [ForeignKey(nameof(DocumentId))]
  public virtual Document Document { get; set; } = null!;

  [ForeignKey(nameof(ActorUserId))]
  public virtual User ActorUser { get; set; } = null!;
}
