
namespace Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Response
{

    public class BadRequest
    {
        public string status { get; set; }
        public string message { get; set; }
        public string correlationId { get; set; }
        public string[] invalidEmails { get; set; }
        public Failuremessage[] failureMessages { get; set; }
        public string requestId { get; set; }
    }

    public class Failuremessage
    {
        public int index { get; set; }
        public Error error { get; set; }
    }

    public class Error
    {
        public string status { get; set; }
        public string message { get; set; }
    }

}
