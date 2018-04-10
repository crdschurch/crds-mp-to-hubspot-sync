using Crossroads.Service.HubSpot.Sync.ApplicationServices.Logging;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Services;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing.Enum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration;
using Crossroads.Service.HubSpot.Sync.Data.LiteDb.JobProcessing;
using DalSoft.Hosting.BackgroundQueue;

namespace Crossroads.Service.HubSpot.Sync.App.Controllers
{
    [Route("")]
    public class SyncController : Controller
    {
        private readonly BackgroundQueue _backgroundQueue;
        private readonly ISyncMpContactsToHubSpotService _syncService;
        private readonly IJobRepository _jobRepository;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<SyncController> _logger;

        public SyncController(
            BackgroundQueue backgroundQueue,
            ISyncMpContactsToHubSpotService syncService,
            IJobRepository jobRepository,
            IConfigurationService configurationService,
            ILogger<SyncController> logger)
        {
            _backgroundQueue = backgroundQueue ?? throw new ArgumentNullException(nameof(backgroundQueue));
            _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [Route("hello")]
        public IActionResult HelloWorld()
        {
            return Ok("hello world");
        }

        [HttpPost]
        [Route("execute")]
        public IActionResult SyncMpContactsToHubSpot()
        {
            using (_logger.BeginScope(AppEvent.Web.SyncMpContactsToHubSpot))
            {
                try
                {
                    _backgroundQueue.Enqueue(async cancellationToken => await _syncService.Sync());

                    return Ok();
                }
                catch (Exception exc)
                {
                    _logger.LogError(AppEvent.Web.SyncMpContactsToHubSpot, exc, "An exception occurred while syncing MP contacts to HubSpot.");
                    throw;
                }
            }
        }

        [HttpPost]
        [Route("state/reset")]
        public IActionResult ResetJobProcessingState()
        {
            using (_logger.BeginScope(AppEvent.Web.ResetJobProcessingState))
            {
                try
                {
                    _jobRepository.SetSyncJobProcessingState(SyncProcessingState.Idle);

                    return Ok();
                }
                catch (Exception exc)
                {
                    _logger.LogError(AppEvent.Web.ResetJobProcessingState, exc, "An exception occurred resetting the job processing state to Idle.", exc);
                    throw;
                }
            }
        }

        [HttpGet]
        [Route("hubspotapirequestcount")]
        public IActionResult ViewHubSpotApiRequestCount()
        {
            using (_logger.BeginScope(AppEvent.Web.ViewHubSpotApiRequestCount))
            {
                try
                {
                    return Json(_jobRepository.GetHubSpotApiDailyRequestCount());
                }
                catch (Exception exc)
                {
                    _logger.LogError(AppEvent.Web.ViewHubSpotApiRequestCount, exc, "An exception occurred viewing the sync processing state.", exc);
                    throw;
                }
            }
        }

        [HttpGet]
        [Route("dates/view")]
        public IActionResult ViewLastSyncDates()
        {
            using (_logger.BeginScope(AppEvent.Web.ViewLastSuccessfulSyncDates))
            {
                try
                {
                    var dates = _configurationService.GetLastSuccessfulSyncDates();
                    return Content($"Last successful sync dates<br/>Create: {dates.CreateSyncDate.ToLocalTime()}<br/>Update: {dates.UpdateSyncDate.ToLocalTime()}", "text/html");
                }
                catch (Exception exc)
                {
                    _logger.LogError(AppEvent.Web.ViewLastSuccessfulSyncDates, exc, "An exception occurred viewing the sync processing state.", exc);
                    throw;
                }
            }
        }

        [HttpGet]
        [Route("state/view")]
        public IActionResult ViewJobProcessingState()
        {
            using (_logger.BeginScope(AppEvent.Web.ViewJobProcessingState))
            {
                try
                {
                    return Content($"Current state: {_configurationService.GetCurrentJobProcessingState()}");
                }
                catch (Exception exc)
                {
                    _logger.LogError(AppEvent.Web.ViewJobProcessingState, exc, "An exception occurred viewing the sync processing state.", exc);
                    throw;
                }
            }
        }

        [HttpGet]
        [Route("activity/view/{activityid}")]
        public IActionResult ViewActivity(string activityId)
        {
            using (_logger.BeginScope(AppEvent.Web.ViewSyncActivity))
            {
                try
                {
                    return Json(_jobRepository.GetActivity(activityId));
                }
                catch (Exception exc)
                {
                    _logger.LogError(AppEvent.Web.ViewSyncActivity, exc, "An exception occurred while fetching a sync activity.", exc);
                    throw;
                }
            }
        }

        [HttpGet]
        [Route("activity/viewall")] // maybe create a view later
        public IActionResult ViewActivities(int limit = 20)
        {
            using (_logger.BeginScope(AppEvent.Web.ViewAllSyncActivities))
            {
                try
                {
                    var activitiesMarkedUp = _jobRepository.GetActivityIds(limit)
                        .ToDictionary(activityId => DateTime.Parse(activityId.Split('_')[1]), activityId => Url.Action("ViewActivity", new { activityId }))
                        .Select(kvp => $"<a target=\"_blank\" href=\"{kvp.Value}\">{kvp.Key.ToLocalTime()}</a>");
                    return Content(string.Join("<br/>", activitiesMarkedUp), "text/html" );
                }
                catch (Exception exc)
                {
                    _logger.LogError(AppEvent.Web.ViewAllSyncActivities, exc, "An exception occurred while fetching a sync activity.", exc);
                    throw;
                }
            }
        }

        [HttpGet]
        [Route("activity/viewlatest")]
        public IActionResult ViewLatestActivity()
        {
            using (_logger.BeginScope(AppEvent.Web.ViewMostRecentSyncActivity))
            {
                try
                {
                    if (_configurationService.GetCurrentJobProcessingState() == SyncProcessingState.Processing)
                        return Content("Job is currently processing. Refresh later to see the latest sync results.");

                    return Json(_jobRepository.GetMostRecentActivity());
                }
                catch (Exception exc)
                {
                    _logger.LogError(AppEvent.Web.ViewMostRecentSyncActivity, exc, "An exception occurred while fetching most recent sync activity.", exc);
                    throw;
                }
            }
        }
    }
}
