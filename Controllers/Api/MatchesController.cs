using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportsManagementMVC.Data;
using SportsManagementMVC.Models;

namespace SportsManagementMVC.Controllers.Api
{
    [ApiController]
    [Route("api/matches")]
    [Authorize(
        AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
        Roles = "Admin,Coach,Player"
    )]
    public class MatchesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public MatchesController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetMatches()
        {
            var matches = await _db.Matches
                .AsNoTracking()
                .OrderByDescending(match => match.Date)
                .ThenBy(match => match.Time)
                .ThenBy(match => match.Id)
                .Select(match => new
                {
                    id = match.Id,
                    teamA = match.TeamA,
                    teamB = match.TeamB,
                    date = match.Date,
                    time = match.Time,
                    venue = match.Venue,
                    tournament = match.Tournament,
                    status = match.Status.ToString(),
                    scoreA = match.ScoreA,
                    scoreB = match.ScoreB
                })
                .ToListAsync();

            return Ok(matches);
        }

        [HttpGet("{id:int:min(1)}")]
        public async Task<IActionResult> GetMatch(int id)
        {
            var match = await _db.Matches
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id);

            if (match == null)
            {
                return NotFound(new
                {
                    message = "The match could not be found."
                });
            }

            return Ok(new
            {
                id = match.Id,
                teamA = match.TeamA,
                teamB = match.TeamB,
                date = match.Date,
                time = match.Time,
                venue = match.Venue,
                tournament = match.Tournament,
                status = match.Status.ToString(),
                scoreA = match.ScoreA,
                scoreB = match.ScoreB
            });
        }

        [HttpPut("{id:int:min(1)}/result")]
        [Authorize(Roles = "Admin,Coach")]
        public async Task<IActionResult> UpdateResult(
            int id,
            UpdateMatchResultRequest request)
        {
            var match = await _db.Matches
                .FirstOrDefaultAsync(item => item.Id == id);

            if (match == null)
            {
                return NotFound(new
                {
                    message = "The match could not be found."
                });
            }

            if (request.ScoreA < 0 || request.ScoreB < 0)
            {
                return BadRequest(new
                {
                    message = "Match scores cannot be less than zero."
                });
            }

            if (!Enum.TryParse<MatchStatus>(
                request.Status,
                true,
                out var matchStatus))
            {
                return BadRequest(new
                {
                    message = "Invalid match status."
                });
            }

            match.ScoreA = request.ScoreA;
            match.ScoreB = request.ScoreB;
            match.Status = matchStatus;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                id = match.Id,
                teamA = match.TeamA,
                teamB = match.TeamB,
                date = match.Date,
                time = match.Time,
                venue = match.Venue,
                tournament = match.Tournament,
                status = match.Status.ToString(),
                scoreA = match.ScoreA,
                scoreB = match.ScoreB
            });
        }
    }

    public class UpdateMatchResultRequest
    {
        public int ScoreA { get; set; }

        public int ScoreB { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}