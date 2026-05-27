using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;

public class BedType
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    public virtual ICollection<Bed> Beds { get; set; } = new List<Bed>();
}