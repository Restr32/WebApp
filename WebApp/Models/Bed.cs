using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;

public class Bed
{
    [Key]
    public int Id { get; set; }
    public string RoomId { get; set; } = null!;
    public int BedTypeId { get; set; }

    public virtual BedType BedType { get; set; } = null!;
    public virtual Room Room { get; set; } = null!;
    public virtual ICollection<BedAssignment> BedAssignments { get; set; } = new List<BedAssignment>();
}