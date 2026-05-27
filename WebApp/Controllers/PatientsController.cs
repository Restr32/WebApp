using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Dtos;
using WebApp.Models;

namespace WebApp.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PatientsController : ControllerBase {
    private readonly HospitalContext _context;

    public PatientsController(HospitalContext context) {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetPatients([FromQuery] string? search) {
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

    [HttpPost("{pesel}/bedassignments")]
    public async Task<IActionResult> AssignBed(string pesel, [FromBody] BedAssignmentRequestDto dto) {
        if (dto.To.HasValue && dto.To <= dto.From)
        {
            return BadRequest("Data zakończenia musi być późniejsza niż data rozpoczęcia.");
        }

        var patientExists = await _context.Patients.AnyAsync(p => p.Pesel == pesel);
        if (!patientExists)
        {
            return NotFound($"Nie znaleziono pacjenta o podanym numerze PESEL: {pesel}.");
        }

        var wardExists = await _context.Wards.AnyAsync(w => w.Name == dto.Ward);
        if (!wardExists)
        {
            return NotFound($"Wskazany oddział '{dto.Ward}' nie istnieje w bazie danych.");
        }

        var bedTypeExists = await _context.BedTypes.AnyAsync(bt => bt.Name == dto.BedType);
        if (!bedTypeExists)
        {
            return NotFound($"Wskazany typ łóżka '{dto.BedType}' nie istnieje w bazie danych.");
        }

        var matchingBedIds = await _context.Beds
            .Where(b => b.Room.Ward.Name == dto.Ward && b.BedType.Name == dto.BedType)
            .Select(b => b.Id)
            .ToListAsync();

        if (!matchingBedIds.Any())
        {
            return NotFound($"W oddziale '{dto.Ward}' nie ma żadnych łóżek o typie '{dto.BedType}'.");
        }

        int? freeBedId = null;

        foreach (var bedId in matchingBedIds)
        {
            bool isOccupied = await _context.BedAssignments
                .Where(ba => ba.BedId == bedId)
                .AnyAsync(ba => ba.From < (dto.To ?? DateTime.MaxValue) && (!ba.To.HasValue || ba.To > dto.From));

            if (!isOccupied)
            {
                freeBedId = bedId;
                break;
            }
        }

        if (freeBedId == null)
        {
            return NotFound(
                $"Brak dostępnych łóżek typu '{dto.BedType}' w oddziale '{dto.Ward}' w wybranym przedziale czasowym.");
        }

        var newAssignment = new BedAssignment
        {
            PatientPesel = pesel,
            BedId = freeBedId.Value,
            From = dto.From,
            To = dto.To
        };

        _context.BedAssignments.Add(newAssignment);
        await _context.SaveChangesAsync();

        return Created(string.Empty, new { Message = "Pomyślnie przypisano łóżko.", BedId = freeBedId.Value });
    }
}