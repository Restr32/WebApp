using Microsoft.EntityFrameworkCore;

namespace WebApp.Models;

public class HospitalContext : DbContext
{
    public HospitalContext(DbContextOptions<HospitalContext> options) : base(options) { }

    public virtual DbSet<Admission> Admissions { get; set; }
    public virtual DbSet<Bed> Beds { get; set; }
    public virtual DbSet<BedAssignment> BedAssignments { get; set; }
    public virtual DbSet<BedType> BedTypes { get; set; }
    public virtual DbSet<Patient> Patients { get; set; }
    public virtual DbSet<Room> Rooms { get; set; }
    public virtual DbSet<Ward> Wards { get; set; }
}