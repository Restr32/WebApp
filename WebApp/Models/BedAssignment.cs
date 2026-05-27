using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;

public class BedAssignment
{
    [Key]
    public int Id { get; set; }
    public string PatientPesel { get; set; } = null!;
    public int BedId { get; set; }
    public DateTime From { get; set; }
    public DateTime? To { get; set; }

    public virtual Bed Bed { get; set; } = null!;
    public virtual Patient Patient { get; set; } = null!;
}