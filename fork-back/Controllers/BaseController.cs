using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace fork_back.Controllers
{
    [ApiController]
    [Produces(MediaTypeNames.Application.Json)]
    [Route("api/[controller]")]
    public class BaseController : ControllerBase
    {
    }
}
