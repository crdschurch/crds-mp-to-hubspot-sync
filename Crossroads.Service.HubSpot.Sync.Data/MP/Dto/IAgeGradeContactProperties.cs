namespace Crossroads.Service.HubSpot.Sync.Data.MP.Dto
{
    /// <summary>
    /// <see cref="ICoreContactProperties"/> PROPERTY NAMES MUST MATCH THE COLUMNS NAMES IN
    /// dbo.api_crds_get_newly_registered_mp_contacts_for_hubspot and
    /// dbo.api_crds_get_mp_contact_updates_for_hubspot. When this is true, the mapping from this
    /// instance to a HubSpot-bound DTO will happen automagically (thanks to a smidge of reflection).
    /// </summary>
    public interface IAgeGradeContactProperties
    {
        int Number_of_Infants { get; set; }

        int Number_of_1_Year_Olds { get; set; }

        int Number_of_2_Year_Olds { get; set; }

        int Number_of_3_Year_Olds { get; set; }

        int Number_of_4_Year_Olds { get; set; }

        int Number_of_5_Year_Olds { get; set; }

        int Number_of_Kindergartners { get; set; }

        int Number_of_1st_Graders { get; set; }

        int Number_of_2nd_Graders { get; set; }

        int Number_of_3rd_Graders { get; set; }

        int Number_of_4th_Graders { get; set; }

        int Number_of_5th_Graders { get; set; }

        int Number_of_6th_Graders { get; set; }

        int Number_of_7th_Graders { get; set; }

        int Number_of_8th_Graders { get; set; }

        int Number_of_9th_Graders { get; set; }

        int Number_of_10th_Graders { get; set; }

        int Number_of_11th_Graders { get; set; }

        int Number_of_12th_Graders { get; set; }

        int Number_of_Graduated_Seniors { get; set; }
    }
}