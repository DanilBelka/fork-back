using fork_back.DataContext;
using fork_back.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;

namespace fork_back.Controllers
{
    public class EpicController : BaseController
    {
        DatabaseContext DataContext { get; init; }
        ILogger<EpicController> Logger { get; init; }

        public EpicController(DatabaseContext dbContext, ILogger<EpicController> logger)
        {
            DataContext = dbContext;
            Logger = logger;
        }

        [HttpGet("List")]
        public async Task<IEnumerable<Epic>> GetListAsync([Range(1, int.MaxValue)] int? projectId,
                                                          [Range(1, MaxPageCount)] int? limit,
                                                          [Range(0, int.MaxValue)] int? offset)
        {
            var res = default(List<Epic>);

            if (projectId.HasValue)
            {
                // request project epics
                res = await DataContext.Epics.Where(e => e.ProjectId == projectId.Value)
                                             .OrderBy(e => e.Id)
                                             .Skip(offset ?? 0)
                                             .Take(limit ?? MaxPageCount)
                                             .ToListAsync();
            }
            else
            {
                // request all epics
                res = await DataContext.Epics.OrderBy(e => e.Id)
                                             .Skip(offset ?? 0)
                                             .Take(limit ?? MaxPageCount)
                                             .ToListAsync();
            }

            // avoid cycle references in JSON result
            res.ForEach(e => e.Tickets?.ForEach(t => t.Epic = null));

            return res;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Epic>> GetEpicAsync([Range(1, int.MaxValue)] int id,
                                                           bool includeTickets = false,
                                                           bool includeProject = false)
        {
            var query = DataContext.Epics.AsQueryable();

            if (includeTickets)
            {
                query = query.Include(e => e.Tickets);
            }

            if (includeProject)
            {
                query = query.Include(e => e.Project);
            }

            var res = await query.FirstOrDefaultAsync(e => e.Id == id);

            if (res == default)
            {
                return NotFound();
            }

            // avoid cycle references in JSON result
            {
                if (res.Project != default)
                {
                    res.Project.Epics = null;
                }

                res.Tickets?.ForEach(t => t.Epic = null);
            }

            return res;
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteEpicAsync([Range(1, int.MaxValue)] int id)
        {
            var res = await DataContext.Epics.FirstOrDefaultAsync(p => p.Id == id);
            if (res == default)
            {
                return NotFound();
            }

            DataContext.Epics.Remove(res);
            await DataContext.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<Epic>> CreateEpicAsync(Epic epic)
        {
            var invalidId = epic.Id != 0;
            if (invalidId)
            {
                ModelState.AddModelError(nameof(Epic.Id), "Id should be empty.");
                return ValidationProblem();
            }

            var hasParentProject = await DataContext.Projects.AnyAsync(p => p.Id == epic.ProjectId);
            if (!hasParentProject)
            {
                ModelState.AddModelError(nameof(Epic.ProjectId), "No project was found.");
                return ValidationProblem();
            }

            await DataContext.Epics.AddAsync(epic);
            await DataContext.SaveChangesAsync();

            return epic;
        }

        [HttpPut]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<Epic>> EditEpicAsync(Epic epic)
        {
            var hasTickets = epic.Tickets?.Any() ?? false;
            if (hasTickets)
            {
                ModelState.AddModelError(nameof(Account.Tickets), "Use other API to update ticket list.");
                return ValidationProblem();
            }

            var res = await DataContext.Epics.FirstOrDefaultAsync(e => e.Id == epic.Id);
            if (res == default)
            {
                return NotFound();
            }

            var hasParentProject = await DataContext.Projects.AnyAsync(p => p.Id == epic.ProjectId);
            if (!hasParentProject)
            {
                ModelState.AddModelError(nameof(Epic.ProjectId), "No project was found.");
                return ValidationProblem();
            }

            res.ProjectId = epic.ProjectId;
            res.Title = epic.Title;
            res.Description = epic.Description;
            //res.Tickets = account.Tickets;       // We do not update tickets list here

            await DataContext.SaveChangesAsync();

            return res;
        }
    }
}
