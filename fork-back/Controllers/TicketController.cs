using fork_back.DataContext;
using fork_back.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.Mime;

namespace fork_back.Controllers
{
    public class TicketController : BaseController
    {
        DatabaseContext DataContext { get; init; }
        ILogger<TicketController> Logger { get; init; }

        public TicketController(DatabaseContext dbContext, ILogger<TicketController> logger)
        {
            DataContext = dbContext;
            Logger = logger;
        }

        [HttpGet("List")]
        public async Task<IEnumerable<Ticket>> GetListAsync([Range(1, int.MaxValue)] int? epicId,
                                                            [Range(1, MaxPageCount)] int? limit,
                                                            [Range(0, int.MaxValue)] int? offset)
        {
            var query = DataContext.Tickets.AsQueryable();
            if (epicId.HasValue)
            {
                query = query.Where(t => t.EpicId == epicId);
            }

            query = query.OrderBy(e => e.Id)
                         .Skip(offset ?? 0)
                         .Take(limit ?? MaxPageCount);

            var res = await query.ToListAsync();

            // avoid cycle references in JSON result
            res.ForEach(t => { if (t.Epic != default) { t.Epic.Tickets = default; } });
            res.ForEach(t => t.Accounts?.ForEach(a => a.Tickets = default));

            return res;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Ticket>> GetTicketAsync([Range(1, int.MaxValue)] int id,
                                                               bool includeAccounts = false,
                                                               bool includeEpic = false,
                                                               bool thenIncludeEpicProject = false)
        {
            var query = DataContext.Tickets.AsQueryable();

            if (includeAccounts)
            {
                query = query.Include(t => t.Accounts);
            }

            if (includeEpic)
            {
                var epicQuery = query.Include(t => t.Epic);

                query = thenIncludeEpicProject ? epicQuery.ThenInclude(e => e!.Project) :
                                                 epicQuery;
            }

            var res = await query.Where(t => t.Id == id)
                                 .FirstOrDefaultAsync();

            if (res == default)
            {
                return NotFound();
            }

            // avoid cycle references in JSON result
            {
                if (res.Epic != default)
                {
                    res.Epic.Tickets = default;

                    if (res.Epic.Project != default)
                    {
                        res.Epic.Project.Epics = default;
                    }
                }

                res.Accounts?.ForEach(a => a.Tickets = default);
            }

            return res;
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteTicketAsync([Range(1, int.MaxValue)] int id)
        {
            var res = await DataContext.Tickets.FirstOrDefaultAsync(p => p.Id == id);
            if (res == default)
            {
                return NotFound();
            }

            DataContext.Tickets.Remove(res);
            await DataContext.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<Ticket>> CreateTicketAsync(Ticket ticket)
        {
            var invalidId = ticket.Id != 0;
            if (invalidId)
            {
                ModelState.AddModelError(nameof(Ticket.Id), "Id should be empty.");
                return ValidationProblem();
            }

            var hasAccounts = ticket.Accounts?.Any() ?? false;
            if (hasAccounts)
            {
                ModelState.AddModelError(nameof(Ticket.Accounts), "No accounts allowed here. Use other API to assign ticket for account.");
                return ValidationProblem();
            }

            var hasEpic = ticket.Epic != default;
            if (hasEpic)
            {
                ModelState.AddModelError(nameof(Ticket.Epic), "Use only the EpicId here.");
                return ValidationProblem();
            }

            var hasParentEpic = await DataContext.Epics.AnyAsync(e => e.Id == ticket.EpicId);
            if (!hasParentEpic)
            {
                ModelState.AddModelError(nameof(Ticket.EpicId), "No epic was found.");
                return ValidationProblem();
            }
            
            ticket.DateCreated = DateTime.UtcNow;
            ticket.DateOpened = default;
            ticket.DateResolved = default;
            ticket.DateVerified = default;

            await DataContext.Tickets.AddAsync(ticket);
            await DataContext.SaveChangesAsync();

            return ticket;
        }

        [HttpPut]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<Ticket>> EditTicketAsync(Ticket ticket)
        {
            var hasTickets = ticket.Accounts?.Any() ?? false;
            if (hasTickets)
            {
                ModelState.AddModelError(nameof(Ticket.Accounts), "Use other API to update account list.");
                return ValidationProblem();
            }

            var hasEpic = ticket.Epic != default;
            if (hasEpic)
            {
                ModelState.AddModelError(nameof(Ticket.Epic), "Use only the EpicId here.");
                return ValidationProblem();
            }

            var res = await DataContext.Tickets.FirstOrDefaultAsync(t => t.Id == ticket.Id);
            if (res == default)
            {
                return NotFound();
            }

            var hasParentEpic = await DataContext.Epics.AnyAsync(e => e.Id == ticket.EpicId);
            if (!hasParentEpic)
            {
                ModelState.AddModelError(nameof(Ticket.EpicId), "No epic was found.");
                return ValidationProblem();
            }

            // Update model
            {
                res.EpicId = ticket.EpicId;
                res.Title = ticket.Title;
                res.Description = ticket.Description;
                //res.Accounts = account.Accounts;       // We do not update tickets list here

                UpdateState(res, ticket.State);
            }

            await DataContext.SaveChangesAsync();

            return res;
        }

        void UpdateState(Ticket t, TicketState state)
        {
            if (t.State != state)
            {
                switch (state)
                {
                    case TicketState.Triage:
                        t.DateOpened = default;
                        t.DateResolved = default;
                        t.DateVerified = default;
                        break;

                    case TicketState.Open:
                        t.DateOpened = DateTime.UtcNow;
                        t.DateResolved = default;
                        t.DateVerified = default;
                        break;

                    case TicketState.InProgress:
                        if (t.DateOpened == default)
                        {
                            t.DateOpened = DateTime.UtcNow;
                        }
                        t.DateResolved = default;
                        t.DateVerified = default;
                        break;

                    case TicketState.Resolved:
                        if (t.DateOpened == default)
                        {
                            t.DateOpened = DateTime.UtcNow;
                        }
                        t.DateResolved = DateTime.UtcNow;
                        t.DateVerified = default;
                        break;

                    case TicketState.Verified:
                        if (t.DateOpened == default)
                        {
                            t.DateOpened = DateTime.UtcNow;
                        }

                        if (t.DateResolved == default)
                        {
                            t.DateResolved = DateTime.UtcNow;
                        }
                        t.DateVerified = DateTime.UtcNow;
                        break;

                    default:
                        Debug.Assert(false, "Unknown state");
                        break;
                }

                t.State = state;
            }
        }
    }
}
