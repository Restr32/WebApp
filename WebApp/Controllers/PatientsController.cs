using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Controllers;
[Route("api/[controller]")]
[ApiController]
public class PatientsController : ControllerBase{
    private readonly HospitalContext _context;

    public PatientsController(HospitalContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetPatients([FromQuery] string? search)
    {
        var query = _context.Patients
            .Include(p => p.Admissions).ThenInclude(a => a.Ward)
            .Include(p => p.BedAssignments).ThenInclude(ba => ba.Bed).ThenInclude(b => b.BedType)
            .Include(p => p.BedAssignments).ThenInclude(ba => ba.Bed).ThenInclude(b => b.Room).ThenInclude(r => r.Ward)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => EF.Functions.Like(p.FirstName, $"%{search}%") 
                                  || EF.Functions.Like(p.LastName, $"%{search}%"));
        }

        var patients = await query.ToListAsync();

        var result = patients.Select(p => new
        {
            pesel = p.Pesel,
            firstName = p.FirstName,
            lastName = p.LastName,
            age = p.Age,
            sex = p.Sex ? "Male" : "Female",
            admissions = p.Admissions.Select(a => new
            {
                id = a.Id,
                admissionDate = a.AdmissionDate,
                dischargeDate = a.DischargeDate,
                ward = new
                {
                    id = a.Ward.Id,
                    name = a.Ward.Name,
                    description = a.Ward.Description
                }
            }),
            bedAssignments = p.BedAssignments.Select(ba => new
            {
                id = ba.Id,
                from = ba.From,
                to = ba.To,
                bed = new
                {
                    id = ba.Bed.Id,
                    bedType = new
                    {
                        id = ba.Bed.BedType.Id,
                        name = ba.Bed.BedType.Name,
                        description = ba.Bed.BedType.Description
                    },
                    room = new
                    {
                        id = ba.Bed.Room.Id,
                        hasTv = ba.Bed.Room.HasTv,
                        ward = new
                        {
                            id = ba.Bed.Room.Ward.Id,
                            name = ba.Bed.Room.Ward.Name,
                            description = ba.Bed.Room.Ward.Description
                        }
                    }
                }
            })
        });

        return Ok(result);
    }
}