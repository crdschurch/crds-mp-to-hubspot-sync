using Crossroads.Service.HubSpot.Sync.ApplicationServices.Logging;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Services;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

namespace Crossroads.Service.HubSpot.Sync.App.Controllers
{
    [Route("[controller]")]
    public class SyncController : Controller
    {
        private readonly ISyncNewMpRegistrationsToHubSpot _syncService;
        private readonly IJobRepository _jobRepository;
        private readonly ILogger<SyncController> _logger;

        public SyncController(ISyncNewMpRegistrationsToHubSpot syncService, IJobRepository jobRepository, ILogger<SyncController> logger)
        {
            _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [Route("hello")]
        public IActionResult HelloWorld()
        {
            return Ok("hello world");
        }

        [HttpGet]
        [Route("new/run")]
        public IActionResult SyncNewMpRegistrationsToHubSpot()
        {
            using (_logger.BeginScope(AppEvent.Web.SyncNewMpToHubSpot))
            {
                try
                {
                    var activity = _syncService.Execute();
                    if (activity.JobProcessingState == JobProcessingState.Processing)
                        return Content("Job is already processing. Please wait and try again later.");

                    return RedirectToAction("ViewResult", "Sync", new {activityid = activity.Id});
                }
                catch (Exception exc)
                {
                    _logger.LogError(AppEvent.Web.SyncNewMpToHubSpot, exc, "An exception occurred while executing the .");
                    throw;
                }
            }
        }

        [HttpGet]
        [Route("viewresult/{activityid}")]
        public IActionResult ViewResult(string activityId)
        {
            using (_logger.BeginScope(AppEvent.Web.ViewActivityResult))
            {
                try
                {
                    return Json(_jobRepository.GetActivity(activityId));
                }
                catch (Exception exc)
                {
                    _logger.LogError(AppEvent.Web.ViewActivityResult, exc, "An exception occurred while fetching a sync activity.", exc);
                    throw;
                }
            }
        }
    }
}
