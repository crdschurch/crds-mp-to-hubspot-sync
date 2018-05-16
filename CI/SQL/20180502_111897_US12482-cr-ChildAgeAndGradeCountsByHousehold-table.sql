use MinistryPlatform
go

-- drop table dbo.cr_ChildAgeAndGradeCountsByHousehold
if not exists (select 1 from INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA = 'dbo' and TABLE_NAME = 'cr_ChildAgeAndGradeCountsByHousehold')
begin

    create table dbo.cr_ChildAgeAndGradeCountsByHousehold
    (
	    Hash binary(20) not null,
        CreatedUtc datetime not null,
	    LastModifiedUtc datetime not null,
	    HouseholdId int primary key not null,
	    Number_of_Infants int not null,
	    Number_of_1_Year_Olds int not null,
	    Number_of_2_Year_Olds int not null,
	    Number_of_3_Year_Olds int not null,
	    Number_of_4_Year_Olds int not null,
	    Number_of_5_Year_Olds int not null,
	    Number_of_Kindergartners int not null,
	    Number_of_1st_Graders int not null,
	    Number_of_2nd_Graders int not null,
	    Number_of_3rd_Graders int not null,
	    Number_of_4th_Graders int not null,
	    Number_of_5th_Graders int not null,
	    Number_of_6th_Graders int not null,
	    Number_of_7th_Graders int not null,
	    Number_of_8th_Graders int not null,
	    Number_of_9th_Graders int not null,
	    Number_of_10th_Graders int not null,
	    Number_of_11th_Graders int not null,
	    Number_of_12th_Graders int not null,
	    Number_of_Graduated_Seniors int not null
    )

    create nonclustered index IDX_cr_ChildAgeAndGradeCountsByHousehold_Hash on dbo.cr_ChildAgeAndGradeCountsByHousehold (Hash);

    create nonclustered index IDX_cr_ChildAgeAndGradeCountsByHousehold_CreatedUtc on dbo.cr_ChildAgeAndGradeCountsByHousehold (CreatedUtc);

    create nonclustered index IDX_cr_ChildAgeAndGradeCountsByHousehold_LastModifiedUtc on dbo.cr_ChildAgeAndGradeCountsByHousehold (LastModifiedUtc);

end