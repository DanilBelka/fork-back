using fork_back.DataContext;
using fork_back.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;

namespace fork_back.Controllers
{
    public class AccountsController : BaseController
    {
        DatabaseContext DataContext { get; init; }
        ILogger<AccountsController> Logger { get; init; }

        public AccountsController(DatabaseContext dbContext, ILogger<AccountsController> logger)
        {
            DataContext = dbContext;
            Logger = logger;
        }

        [HttpGet("List")]
        public async Task<IEnumerable<Account>> GetListAsync([Range(1, MaxPageCount)] int? limit,
                                                             [Range(0, int.MaxValue)] int? offset)
        {
            var res = await DataContext.Accounts.Include(a => a.Tickets)
                                                .Skip(offset ?? 0)
                                                .Take(limit ?? MaxPageCount)
                                                .ToListAsync();

            // avoid cycle references in JSON result
            res.ForEach(a => a.Tickets?.ForEach(t => t.Accounts = null));

            return res;
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Account>> GetAccountAsync([Range(1, int.MaxValue)] int id)
        {
            var res = await DataContext.Accounts.Include(a => a.Tickets)
                                                .FirstOrDefaultAsync(a => a.Id == id);

            if (res == default)
            {
                return NotFound();
            }

            // avoid cycle references in JSON result
            res.Tickets?.ForEach(t => t.Accounts = null);

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

            return account;
        }
    }
}
