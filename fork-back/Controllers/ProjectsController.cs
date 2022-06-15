using fork_back.DataContext;
using fork_back.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;

namespace fork_back.Controllers
{
    public class ProjectsController : BaseController
    {
        DatabaseContext DataContext { get; init; }
        ILogger<ProjectsController> Logger { get; init; }

        public ProjectsController(DatabaseContext dbContext, ILogger<ProjectsController> logger)
        {
            DataContext = dbContext;
            Logger = logger;
        }

        [HttpGet("List")]
        public async Task<IEnumerable<Project>> GetListAsync([Range(1, MaxPageCount)] int? limit,
                                                           [Range(0, int.MaxValue)] int? offset)
        {
            var res = await DataContext.Projects.OrderBy(p => p.Id)
                                                .Skip(offset ?? 0)
                                                .Take(limit ?? MaxPageCount)
                                                .ToListAsync();

            // avoid cycle references in JSON result
            res.ForEach(p => p.Epics?.ForEach(e => e.Project = default));
            res.ForEach(p => p.Epics?.ForEach(e => e.Tickets = default));

            return res;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Project>> GetProjectAsync([Range(1, int.MaxValue)] int id,
                                                                 bool includeEpics = false)
        {
            var query = DataContext.Projects.AsQueryable();

            if (includeEpics)
            {
                query = query.Include(a => a.Epics);
            }

            var res = await query.FirstOrDefaultAsync(p => p.Id == id);

            if (res == default)
            {
                return NotFound();
            }

            // avoid cycle references in JSON result
            res.Epics?.ForEach(e => e.Project = default);
            res.Epics?.ForEach(e => e.Tickets = default);

            return res;
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteProjectAsync([Range(1, int.MaxValue)] int id)
        {
            var res = await DataContext.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (res == default)
            {
                return NotFound();
            }

            DataContext.Projects.Remove(res);
            await DataContext.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<Project>> CreateProjectAsync(Project project)
        {
            var invalidId = project.Id != 0;
            if (invalidId)
            {
                ModelState.AddModelError(nameof(Project.Id), "Id should be empty.");
                return ValidationProblem();
            }

            var isUrlBusy = await DataContext.Projects.AnyAsync(p => p.Url == project.Url);
            if (isUrlBusy)
            {
                ModelState.AddModelError(nameof(Project.Url), "Url is already used.");
                return ValidationProblem();
            }

            await DataContext.Projects.AddAsync(project);
            await DataContext.SaveChangesAsync();

            return project;
        }

        [HttpPut]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<Project>> EditProjectAsync(Project project)
        {
            var hasTickets = project.Epics?.Any() ?? false;
            if (hasTickets)
            {
                ModelState.AddModelError(nameof(Project.Epics), "Use other API to update epics list.");
                return ValidationProblem();
            }

            var isUrlBusy = await DataContext.Projects.AnyAsync(p => p.Url == project.Url &&
                                                                       p.Id != project.Id);
            if (isUrlBusy)
            {
                ModelState.AddModelError(nameof(Project.Url), "Url is already used by other project.");
                return ValidationProblem();
            }

            var res = await DataContext.Projects.FirstOrDefaultAsync(p => p.Id == project.Id);
            if (res == default)
            {
                return NotFound();
            }

            res.Name = project.Name;
            res.Description = project.Description;
            res.Url = project.Url;
            //res.Epics = project.Epics;       // We do not update epics list here

            await DataContext.SaveChangesAsync();

            return res;
        }
    }
}
