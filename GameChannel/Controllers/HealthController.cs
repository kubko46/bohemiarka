// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using WingsEmu.Game.Managers;

namespace GameChannel.Controllers
{
    [HealthCheckApiKey]
    [Route("/health")]
    [ApiController]
    public class HealthController : Controller
    {
        private readonly IServerManager _serverManager;

        public HealthController(IServerManager serverManager) => _serverManager = serverManager;


        [HttpGet("liveness")]
        public IActionResult GetLivenessState()
        {
            switch (_serverManager.State)
            {
                case GameServerState.ERROR:
                    return StatusCode(500, "Server is in error state");
                case GameServerState.STARTING:
                    return StatusCode(500, "Server is starting");
                case GameServerState.RUNNING:
                case GameServerState.IDLE:
                    return Ok($"{_serverManager.State.ToString()}");
                case GameServerState.STOPPING:
                    return Ok(); // not sure about that
                    break;
            }

            return Ok();
        }

        [HttpGet("readiness")]
        public IActionResult GetReadinessState()
        {
            switch (_serverManager.State)
            {
                case GameServerState.ERROR:
                    return StatusCode(500, "Server is in error state");
                case GameServerState.STARTING:
                    return StatusCode(500, "Server is starting");
                case GameServerState.RUNNING:
                case GameServerState.IDLE:
                    return Ok($"{_serverManager.State.ToString()}");
                case GameServerState.STOPPING:
                    return Ok(); // not sure about that
            }

            return Ok();
        }


        [AttributeUsage(AttributeTargets.Class)]
        public class HealthCheckApiKey : Attribute, IAsyncActionFilter
        {
            private const string APIKEYNAME = "HEALTHCHECK_API_KEY";
            private static string HEALTHCHECK_API_KEY = Environment.GetEnvironmentVariable(APIKEYNAME) ?? "123456789";

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                if (!context.HttpContext.Request.Headers.TryGetValue(APIKEYNAME, out StringValues extractedApiKey))
                {
                    context.Result = new ContentResult
                    {
                        StatusCode = 401,
                        Content = "Api Key was not provided"
                    };
                    return;
                }

                if (!extractedApiKey.Equals(extractedApiKey))
                {
                    context.Result = new ContentResult
                    {
                        StatusCode = 401,
                        Content = "Api Key is not valid"
                    };
                    return;
                }

                await next();
            }
        }
    }
}