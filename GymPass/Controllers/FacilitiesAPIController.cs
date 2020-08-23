using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GymPass.Data;
using GymPass.Models;

namespace GymPass.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacilitiesAPIController : ControllerBase
    {
        private readonly FacilityContext _context;

        public FacilitiesAPIController(FacilityContext context)
        {
            _context = context;
        }

        // GET: api/FacilitiesAPI
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Facility>>> GetFacility()
        {
            return await _context.Facilities.ToListAsync();
        }

        // GET: api/FacilitiesAPI/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Facility>> GetFacility(int id)
        {
            var facility = await _context.Facilities.FindAsync(id);

            if (facility == null)
            {
                return NotFound();
            }

            return facility;
        }

        // PUT: api/FacilitiesAPI/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFacility(int id, Facility facility)
        {
            if (id != facility.FacilityID)
            {
                return BadRequest();
            }

            _context.Entry(facility).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FacilityExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/FacilitiesAPI
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        public async Task<ActionResult<Facility>> PostFacility(Facility facility)
        {
            _context.Facilities.Add(facility);
            await _context.SaveChangesAsync();

            // gets the value of the facility item from the body of the HTTP request.
            return CreatedAtAction(nameof(GetFacility), new { id = facility.FacilityID }, facility);
        }

        // DELETE: api/FacilitiesAPI/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Facility>> DeleteFacility(int id)
        {
            var facility = await _context.Facilities.FindAsync(id);
            if (facility == null)
            {
                return NotFound();
            }

            _context.Facilities.Remove(facility);
            await _context.SaveChangesAsync();

            return facility;
        }

        private bool FacilityExists(int id)
        {
            return _context.Facilities.Any(e => e.FacilityID == id);
        }
    }
}
