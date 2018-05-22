use MinistryPlatform
go

-- drop procedure dbo.crds_get_child_age_and_grade_counts;

create or alter function dbo.crds_get_child_age_and_grade_counts()  
returns table
as
return

    select          Number_of_Infants,
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
                    Number_of_Graduated_Seniors,
                    HouseholdId,
                    [Hash],
                    CreatedUtc,
                    LastModifiedUtc

    --              Attach kiddo info to any contact we want in HubSpot (could even be the minor themselves, accommodates HOH kiddo)
    from            dbo.cr_ChildAgeAndGradeCountsByHousehold;