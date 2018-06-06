use MinistryPlatform
go

-- gets a list of contacts that were created after a given date (presumably, new registrations)
create or alter procedure dbo.api_crds_get_newly_registered_mp_contacts_for_hubspot
    @LastSuccessfulSyncDate datetime
as

    select              MinistryPlatformContactId,
                        Email, -- switched to dp_Users.User_Name based on dbo.crds_service_update_email_nightly
                        Firstname,
                        Lastname,
                        Community,
                        Marital_Status,
                        Gender,
                        Phone,         -- HS internal id (lower case)
                        MobilePhone,   -- HS internal id (lower case)
                        Zip,           -- HS internal id (lower case)
                        Number_of_Infants,
                        Number_of_1_Year_Olds,
                        Number_of_2_Year_Olds,
                        Number_of_3_Year_Olds,
                        Number_of_4_Year_Olds,
                        Number_of_5_Year_Olds,
                        Number_of_Kindergartners,
                        Number_of_1st_Graders,
                        Number_of_2nd_Graders,
                        Number_of_3rd_Graders,
                        Number_of_4th_Graders,
                        Number_of_5th_Graders,
                        Number_of_6th_Graders,
                        Number_of_7th_Graders,
                        Number_of_8th_Graders,
                        Number_of_9th_Graders,
                        Number_of_10th_Graders,
                        Number_of_11th_Graders,
                        Number_of_12th_Graders,
                        Number_of_Graduated_Seniors

    from                dbo.vw_crds_hubspot_users
    where               ParticipantStartDate > @LastSuccessfulSyncDate;