using Crossroads.Service.HubSpot.Sync.ApplicationServices.Configuration;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Logging;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.Services;
using DalSoft.Hosting.BackgroundQueue;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Dto;
using Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Enum;

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
                    var clickHereToViewProgress = $@"Click <a target=""blank"" href=""{Url.Action("ViewActivityState")}"">here</a> to view progress.";
                    var activityProgress = _configurationService.GetCurrentActivityProgress();
                    if (activityProgress.ActivityState == ActivityState.Processing)
                        return Content($"The HubSpot sync job is already processing. {clickHereToViewProgress}", "text/html");

                    _backgroundQueue.Enqueue(async cancellationToken => await _syncService.Sync());
                    return Content($"HubSpot sync job is now processing. {clickHereToViewProgress}", "text/html");
                }
                catch (Exception exc)
                {
                    _logger.LogError(AppEvent.Web.SyncMpContactsToHubSpot, exc, "An exception occurred while syncing MP contacts to HubSpot.");
                    throw;
                }
            }
        }

        [HttpGet]
        [Route("state")]
        public IActionResult ViewActivityState()
        {
            using (_logger.BeginScope(AppEvent.Web.ViewJobProcessingState))
            {
                try
                {
                    var progress = _configurationService.GetCurrentActivityProgress();
                    return Content(progress.HtmlPrint(), "text/html");
                }
                catch (Exception exc)
                {
                    _logger.LogError(AppEvent.Web.ViewJobProcessingState, exc, "An exception occurred viewing the sync processing state.", exc);
                    throw;
                }
            }
        }

        [HttpPost]
        [Route("state")]
        public IActionResult ResetActivityState()
        {
            using (_logger.BeginScope(AppEvent.Web.ResetJobProcessingState))
            {
                try
                {
                    _jobRepository.PersistActivityProgress(new ActivityProgress {ActivityState = ActivityState.Idle});
                    return Content($"Activity state has been reset to 'Idle'.", "text/html");
                }
                catch (Exception exc)
                {
                    _logger.LogError(AppEvent.Web.ResetJobProcessingState, exc, "An exception occurred resetting the job processing state to Idle.", exc);
                    throw;
                }
            }
        }

        [HttpGet]
        [Route("dates")]
        public IActionResult ViewLastSyncDates()
        {
            using (_logger.BeginScope(AppEvent.Web.ViewLastSuccessfulSyncDates))
            {
                try
                {
                    var dates = _configurationService.GetLastSuccessfulOperationDates();
                    return Content(
                        $@"<strong>Last successful operation dates</strong> (listed by order of execution)<br/>
                        Age/Grade process date: {dates.AgeAndGradeProcessDate.ToLocalTime()}<br />
                        Registration: {dates.RegistrationSyncDate.ToLocalTime()}<br/>
                        Core update: {dates.CoreUpdateSyncDate.ToLocalTime()}<br />
                        Age/Grade sync date: {dates.AgeAndGradeSyncDate.ToLocalTime()}",
                        "text/html");
                }
                catch (Exception exc)
                {
                    _logger.LogError(AppEvent.Web.ViewLastSuccessfulSyncDates, exc, "An exception occurred viewing the sync processing dates.", exc);
                    throw;
                }
            }
        }

        [HttpGet]
        [Route("activity/{activityid}")]
        public IActionResult ViewActivity(string activityId)
        {
            using (_logger.BeginScope(AppEvent.Web.ViewSyncActivity))
            {
                try
                {
                    return Content(_jobRepository.GetActivity(activityId), "application/json");
                }
                catch (Exception exc)
                {
                    _logger.LogError(AppEvent.Web.ViewSyncActivity, exc, "An exception occurred while fetching a sync activity.", exc);
                    throw;
                }
            }
        }

        [HttpGet]
        [Route("viewall")] // maybe create a view later
        public IActionResult ViewActivities(int limit = 144)
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
        [Route("viewlatest")]
        public IActionResult ViewLatestActivity()
        {
            using (_logger.BeginScope(AppEvent.Web.ViewMostRecentSyncActivity))
            {
                try
                {
                    if (_configurationService.GetCurrentActivityProgress().ActivityState == ActivityState.Processing)
                        return Content("Job is currently processing. Refresh later to see the latest sync results.");

                    return Content(_jobRepository.GetMostRecentActivity(), "application/json");
                }
                catch (Exception exc)
                {
                    _logger.LogError(AppEvent.Web.ViewMostRecentSyncActivity, exc, "An exception occurred while fetching most recent sync activity.", exc);
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
    }
}
