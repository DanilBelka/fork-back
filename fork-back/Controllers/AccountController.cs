using fork_back.DataContext;
using fork_back.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;

namespace fork_back.Controllers
{
    public class AccountController : BaseController
    {
        DatabaseContext DataContext { get; init; }
        ILogger<AccountController> Logger { get; init; }

        public AccountController(DatabaseContext dbContext, ILogger<AccountController> logger)
        {
            DataContext = dbContext;
            Logger = logger;
        }

        [HttpGet("List")]
        public async Task<IEnumerable<Account>> GetListAsync([Range(1, MaxPageCount)] int? limit,
                                                             [Range(0, int.MaxValue)] int? offset)
        {
            var res = await DataContext.Accounts.OrderBy(a => a.Id)
                                                .Skip(offset ?? 0)
                                                .Take(limit ?? MaxPageCount)
                                                .ToListAsync();

            // avoid cycle references in JSON result
            res.ForEach(a => a.Tickets?.ForEach(t => t.Accounts = default));
            res.ForEach(a => a.Tickets?.ForEach(t => t.Epic = default));

            return res;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Account>> GetAccountAsync([Range(1, int.MaxValue)] int id,
                                                                 bool includeTickets = false)
        {
            var query = DataContext.Accounts.AsQueryable();

            if (includeTickets)
            {
                query = query.Include(a => a.Tickets);
            }

            var res = await query.FirstOrDefaultAsync(a => a.Id == id);

            if (res == default)
            {
                return NotFound();
            }

            // avoid cycle references in JSON result
            res.Tickets?.ForEach(t => t.Accounts = default);
            res.Tickets?.ForEach(t => t.Epic = default);

            return res;
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteAccountAsync([Range(1, int.MaxValue)] int id)
        {
            var res = await DataContext.Accounts.FirstOrDefaultAsync(a => a.Id == id);
            if (res == default)
            {
                return NotFound();
            }

            DataContext.Accounts.Remove(res);
            await DataContext.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<Account>> CreateAccountAsync(Account account)
        {
            var invalidId = account.Id != 0;
            if (invalidId)
            {
                ModelState.AddModelError(nameof(Account.Id), "Id should be empty.");
                return ValidationProblem();
            }

            var isLoginBusy = await DataContext.Accounts.AnyAsync(a => a.Login == account.Login);
            if (isLoginBusy)
            {
                ModelState.AddModelError(nameof(Account.Login), "Login is already used.");
                return ValidationProblem();
            }

            await DataContext.Accounts.AddAsync(account);
            await DataContext.SaveChangesAsync();

            return account;
        }

        [HttpPut]
        [Consumes(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<Account>> EditAccountAsync(Account account)
        {
            var hasTickets = account.Tickets?.Any() ?? false;
            if (hasTickets)
            {
                ModelState.AddModelError(nameof(Account.Tickets), "Use other API to update ticket list.");
                return ValidationProblem();
            }

            var isLoginBusy = await DataContext.Accounts.AnyAsync(a => a.Login == account.Login &&
                                                                       a.Id != account.Id);
            if (isLoginBusy)
            {
                ModelState.AddModelError(nameof(Account.Login), "Login is already used by other account.");
                return ValidationProblem();
            }

            var res = await DataContext.Accounts.FirstOrDefaultAsync(a => a.Id == account.Id);
            if (res == default)
            {
                return NotFound();
            }

            res.FirstName = account.FirstName;
            res.LastName = account.LastName;
            //res.Tickets = account.Tickets;       // We do not update tickets list here

            await DataContext.SaveChangesAsync();

            return res;
        }
    }
}
