namespace Crossroads.Service.HubSpot.Sync.Data.MongoDb.JobProcessing.Enum
{
    public enum OperationName
    {
        /// <summary>
        /// Stored procedure responsible for calculating and persisting the number of children
        /// in each age/grade Kid's Club and Student Ministry category for a given household.
        /// This process accounts for both primary and "Other" households.
        /// </summary>
        AgeGradeDataCalculationInMp = 1,

        /// <summary>
        /// Sync step responsible for coralling newly registered users and creating them in
        /// HubSpot.
        /// </summary>
        NewContactRegistrationSync = 2,

        /// <summary>
        /// Sync step responsible for finding updates in Ministry Platform to core contact
        /// attributes and transferring those changes to HubSpot.
        /// </summary>
        CoreContactAttributeUpdateSync = 3,

        /// <summary>
        /// Transfers calculated age/grade data to HubSpot alongside Ministry Platform contacts
		/// that qualify to have contact records in HubSpot.
        /// </summary>
        AgeGradeDataSync = 4
    }
}