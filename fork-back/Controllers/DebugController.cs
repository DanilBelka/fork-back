using fork_back.DataContext;
using fork_back.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;

namespace fork_back.Controllers
{
    public class DebugController : BaseController
    {
        DatabaseContext DataContext { get; init; }

        ILogger<DebugController> Logger { get; init; }

        IWebHostEnvironment Environment { get; init; }

        public DebugController(DatabaseContext dbContext, ILogger<DebugController> logger, IWebHostEnvironment environment)
        {
            DataContext = dbContext;
            Logger = logger;
            Environment = environment;
        }

        [HttpPost("RecreateDatabase")]
        public ActionResult RecreateDatabase()
        {
            if (!Environment.IsDevelopment())
            {
                return BadRequest();
            }

            DataContext.RecreateDatabase();
            return Ok();
        }

        [HttpPost("FillDatabase")]
        public async Task<ActionResult> FillDatabaseAsync()
        {
            if (!Environment.IsDevelopment())
            {
                return BadRequest();
            }

            var accounts = new List<Account>()
            {
                new Account() { Login = "margaret.kyselgova@gmail.com", Role = AccountRole.Developer, FirstName = "PumaMargo", Seсurity = AccountSeсurity.Build("margo")},
                new Account() { Login = "danil.belikov.y.gd@gmail.com", Role = AccountRole.Manager, FirstName = "DanilBelka", Seсurity = AccountSeсurity.Build("belka")},
                new Account() { Login = "kyselgov@gmail.com", Role = AccountRole.Administrator, FirstName = "Supervisor", Seсurity = AccountSeсurity.Build("test")},
            };

            var projects = new List<Project>()
            {
                new Project() { Name = "Fork-back", Description = "Backend of the Fork project", Url = "https://DanilBelka@github.com/DanilBelka/fork-back.git" },
                new Project() { Name = "Fork-front", Description = "Frontend of the Fork project", Url = "https://DanilBelka@github.com/DanilBelka/fork-back.git" },
            };

            var epics = new List<Epic>()
            {
                new Epic() { Project = projects.First(), Title = "Create DataModel" },
                new Epic() { Project = projects.First(), Title = "Create WebAPI" },
            };

            var tickets = new List<Ticket>()
            {
                new Ticket() { Epic = epics.First(), Title = "Create Account Model", State = TicketState.Resolved, DateResolved = DateTime.UtcNow, Accounts = accounts },
                new Ticket() { Epic = epics.First(), Title = "Create Project Model", State = TicketState.Resolved, DateResolved = DateTime.UtcNow, Accounts = accounts },
                new Ticket() { Epic = epics.First(), Title = "Create Epic Model", State = TicketState.Resolved, DateResolved = DateTime.UtcNow, Accounts = accounts },
                new Ticket() { Epic = epics.First(), Title = "Create Ticket Model", State = TicketState.Resolved, DateResolved = DateTime.UtcNow, Accounts = accounts },
                new Ticket() { Epic = epics.First(), Title = "Create EF Base Context", State = TicketState.Resolved, DateResolved = DateTime.UtcNow, Accounts = accounts },
                new Ticket() { Epic = epics.First(), Title = "Create EF MySQL Context", State = TicketState.Resolved, DateResolved = DateTime.UtcNow, Accounts = accounts },

                new Ticket() { Epic = epics.Skip(1).First(), Title = "Create Base Controller", State = TicketState.Verified, DateVerified = DateTime.UtcNow, Accounts = accounts },
                new Ticket() { Epic = epics.Skip(1).First(), Title = "Create Accounts Controller", State = TicketState.Verified, DateVerified = DateTime.UtcNow, Accounts = accounts },
                new Ticket() { Epic = epics.Skip(1).First(), Title = "Create Projects Controller", State = TicketState.Resolved, DateResolved = DateTime.UtcNow, Accounts = accounts },
                new Ticket() { Epic = epics.Skip(1).First(), Title = "Create Epics Controller", State = TicketState.Open, Accounts = accounts },
                new Ticket() { Epic = epics.Skip(1).First(), Title = "Create Tickets Controller", State = TicketState.Triage, Accounts = accounts },
                new Ticket() { Epic = epics.Skip(1).First(), Title = "Create Debug Controller", State = TicketState.InProgress, Accounts = accounts },
            };

            await DataContext.Accounts.AddRangeAsync(accounts);
            await DataContext.Projects.AddRangeAsync(projects);
            await DataContext.Epics.AddRangeAsync(epics);
            await DataContext.Tickets.AddRangeAsync(tickets);
            await DataContext.SaveChangesAsync();

            return Ok();
        }
    }
}
