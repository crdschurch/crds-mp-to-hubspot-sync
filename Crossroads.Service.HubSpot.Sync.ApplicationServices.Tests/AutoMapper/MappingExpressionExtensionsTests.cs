using System.Collections.Generic;
using Crossroads.Service.HubSpot.Sync.ApplicationServices.AutoMapper;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using Crossroads.Service.HubSpot.Sync.Data.MP.Dto;
using FluentAssertions;
using System.Linq;
using Xunit;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Test.AutoMapper
{
    public class MappingExpressionExtensionsTests
    {
        private readonly NewlyRegisteredMpContactDto _newContact = new NewlyRegisteredMpContactDto
        {
            Community = "Florence",
            Email = "luke@dotdash.net",
            Firstname = "Luke",
            Lastname = "Mods",
            Gender = "a dood",
            Marital_Status = "Murried",
            MinistryPlatformContactId = "123456789",
            MobilePhone = "555-555-5555",
            Number_of_Infants = 1,
            Number_of_1_Year_Olds = 2,
            Number_of_2_Year_Olds = 3,
            Number_of_3_Year_Olds = 4,
            Number_of_4_Year_Olds = 5,
            Number_of_5_Year_Olds = 6,
            Number_of_Kindergartners = 7,
            Number_of_1st_Graders = 8,
            Number_of_2nd_Graders = 9,
            Number_of_3rd_Graders = 10,
            Number_of_4th_Graders = 11,
            Number_of_5th_Graders = 12,
            Number_of_6th_Graders = 13,
            Number_of_7th_Graders = 14,
            Number_of_8th_Graders = 15,
            Number_of_9th_Graders = 16,
            Number_of_10th_Graders = 17,
            Number_of_11th_Graders = 18,
            Number_of_12th_Graders = 19,
            Number_of_Graduated_Seniors = 20
        };

        private readonly CoreUpdateMpContactDto _updatedContact = new CoreUpdateMpContactDto
        {
            PropertyName = "email",
            PreviousValue = "old@email.com",
            NewValue = "new@email.com",
            Community = "Oakley",
            Email = "new@email.com",
            Firstname = "Jason",
            Lastname = "Brahms",
            Gender = "a fella",
            Marital_Status = "Casado",
            MinistryPlatformContactId = "9876543210",
            MobilePhone = "555-555-7777",
            Number_of_Infants = 1,
            Number_of_1_Year_Olds = 2,
            Number_of_2_Year_Olds = 3,
            Number_of_3_Year_Olds = 4,
            Number_of_4_Year_Olds = 5,
            Number_of_5_Year_Olds = 6,
            Number_of_Kindergartners = 7,
            Number_of_1st_Graders = 8,
            Number_of_2nd_Graders = 9,
            Number_of_3rd_Graders = 10,
            Number_of_4th_Graders = 11,
            Number_of_5th_Graders = 12,
            Number_of_6th_Graders = 13,
            Number_of_7th_Graders = 14,
            Number_of_8th_Graders = 15,
            Number_of_9th_Graders = 16,
            Number_of_10th_Graders = 17,
            Number_of_11th_Graders = 18,
            Number_of_12th_Graders = 19,
            Number_of_Graduated_Seniors = 20
        };

        private AgeAndGradeGroupCountsForMpContactDto _ageGradeCounts = new AgeAndGradeGroupCountsForMpContactDto
        {
            Community = "Mason",
            Email = "j@p.io",
            Firstname = "Jake",
            Lastname = "Patron",
            Gender = "a lad",
            Marital_Status = "Single",
            MinistryPlatformContactId = "7070707",
            MobilePhone = "111-555-5555",
            Number_of_Infants = 1,
            Number_of_1_Year_Olds = 2,
            Number_of_2_Year_Olds = 3,
            Number_of_3_Year_Olds = 4,
            Number_of_4_Year_Olds = 5,
            Number_of_5_Year_Olds = 6,
            Number_of_Kindergartners = 7,
            Number_of_1st_Graders = 8,
            Number_of_2nd_Graders = 9,
            Number_of_3rd_Graders = 10,
            Number_of_4th_Graders = 11,
            Number_of_5th_Graders = 12,
            Number_of_6th_Graders = 13,
            Number_of_7th_Graders = 14,
            Number_of_8th_Graders = 15,
            Number_of_9th_Graders = 16,
            Number_of_10th_Graders = 17,
            Number_of_11th_Graders = 18,
            Number_of_12th_Graders = 19,
            Number_of_Graduated_Seniors = 20
        };

        [Fact]
        public void should_yield_new_registration_mp_dto_into_hubspot_properties_collection()
        {
            var result = MappingExpressionExtensions.ReflectToContactProperties(_newContact);
            result.First(item => item.Name == "community").Value.Should().Be("Florence");
            result.First(item => item.Name == "email").Value.Should().Be("luke@dotdash.net");
            result.First(item => item.Name == "firstname").Value.Should().Be("Luke");
            result.First(item => item.Name == "lastname").Value.Should().Be("Mods");
            result.First(item => item.Name == "gender").Value.Should().Be("a dood");
            result.First(item => item.Name == "marital_status").Value.Should().Be("Murried");
            result.First(item => item.Name == "ministryplatformcontactid").Value.Should().Be("123456789");
            result.First(item => item.Name == "mobilephone").Value.Should().Be("555-555-5555");
            result.First(item => item.Name == "number_of_infants").Value.Should().Be("1");
            result.First(item => item.Name == "number_of_1_year_olds").Value.Should().Be("2");
            result.First(item => item.Name == "number_of_2_year_olds").Value.Should().Be("3");
            result.First(item => item.Name == "number_of_3_year_olds").Value.Should().Be("4");
            result.First(item => item.Name == "number_of_4_year_olds").Value.Should().Be("5");
            result.First(item => item.Name == "number_of_5_year_olds").Value.Should().Be("6");
            result.First(item => item.Name == "number_of_kindergartners").Value.Should().Be("7");
            result.First(item => item.Name == "number_of_1st_graders").Value.Should().Be("8");
            result.First(item => item.Name == "number_of_2nd_graders").Value.Should().Be("9");
            result.First(item => item.Name == "number_of_3rd_graders").Value.Should().Be("10");
            result.First(item => item.Name == "number_of_4th_graders").Value.Should().Be("11");
            result.First(item => item.Name == "number_of_5th_graders").Value.Should().Be("12");
            result.First(item => item.Name == "number_of_6th_graders").Value.Should().Be("13");
            result.First(item => item.Name == "number_of_7th_graders").Value.Should().Be("14");
            result.First(item => item.Name == "number_of_8th_graders").Value.Should().Be("15");
            result.First(item => item.Name == "number_of_9th_graders").Value.Should().Be("16");
            result.First(item => item.Name == "number_of_10th_graders").Value.Should().Be("17");
            result.First(item => item.Name == "number_of_11th_graders").Value.Should().Be("18");
            result.First(item => item.Name == "number_of_12th_graders").Value.Should().Be("19");
            result.First(item => item.Name == "number_of_graduated_seniors").Value.Should().Be("20");
            result.First(item => item.Name == "source").Value.Should().Be("MP_Registration");
        }

        [Fact]
        public void should_yield_core_update_mp_dto_into_hubspot_properties_collection()
        {
            var result = MappingExpressionExtensions.ReflectToContactProperties(_updatedContact);
            result.First(item => item.Name == "community").Value.Should().Be("Oakley");
            result.First(item => item.Name == "email").Value.Should().Be("new@email.com");
            result.First(item => item.Name == "firstname").Value.Should().Be("Jason");
            result.First(item => item.Name == "lastname").Value.Should().Be("Brahms");
            result.First(item => item.Name == "gender").Value.Should().Be("a fella");
            result.First(item => item.Name == "marital_status").Value.Should().Be("Casado");
            result.First(item => item.Name == "ministryplatformcontactid").Value.Should().Be("9876543210");
            result.First(item => item.Name == "mobilephone").Value.Should().Be("555-555-7777");
            result.First(item => item.Name == "number_of_infants").Value.Should().Be("1");
            result.First(item => item.Name == "number_of_1_year_olds").Value.Should().Be("2");
            result.First(item => item.Name == "number_of_2_year_olds").Value.Should().Be("3");
            result.First(item => item.Name == "number_of_3_year_olds").Value.Should().Be("4");
            result.First(item => item.Name == "number_of_4_year_olds").Value.Should().Be("5");
            result.First(item => item.Name == "number_of_5_year_olds").Value.Should().Be("6");
            result.First(item => item.Name == "number_of_kindergartners").Value.Should().Be("7");
            result.First(item => item.Name == "number_of_1st_graders").Value.Should().Be("8");
            result.First(item => item.Name == "number_of_2nd_graders").Value.Should().Be("9");
            result.First(item => item.Name == "number_of_3rd_graders").Value.Should().Be("10");
            result.First(item => item.Name == "number_of_4th_graders").Value.Should().Be("11");
            result.First(item => item.Name == "number_of_5th_graders").Value.Should().Be("12");
            result.First(item => item.Name == "number_of_6th_graders").Value.Should().Be("13");
            result.First(item => item.Name == "number_of_7th_graders").Value.Should().Be("14");
            result.First(item => item.Name == "number_of_8th_graders").Value.Should().Be("15");
            result.First(item => item.Name == "number_of_9th_graders").Value.Should().Be("16");
            result.First(item => item.Name == "number_of_10th_graders").Value.Should().Be("17");
            result.First(item => item.Name == "number_of_11th_graders").Value.Should().Be("18");
            result.First(item => item.Name == "number_of_12th_graders").Value.Should().Be("19");
            result.First(item => item.Name == "number_of_graduated_seniors").Value.Should().Be("20");
            result.First(item => item.Name == "source").Value.Should().Be("MP_Sync_General_Update");
        }

        [Fact]
        public void should_yield_age_grade_mp_dto_into_hubspot_properties_collection()
        {
            var result = MappingExpressionExtensions.ReflectToContactProperties(_ageGradeCounts);
            result.First(item => item.Name == "community").Value.Should().Be("Mason");
            result.First(item => item.Name == "email").Value.Should().Be("j@p.io");
            result.First(item => item.Name == "firstname").Value.Should().Be("Jake");
            result.First(item => item.Name == "lastname").Value.Should().Be("Patron");
            result.First(item => item.Name == "gender").Value.Should().Be("a lad");
            result.First(item => item.Name == "marital_status").Value.Should().Be("Single");
            result.First(item => item.Name == "ministryplatformcontactid").Value.Should().Be("7070707");
            result.First(item => item.Name == "mobilephone").Value.Should().Be("111-555-5555");
            result.First(item => item.Name == "number_of_infants").Value.Should().Be("1");
            result.First(item => item.Name == "number_of_1_year_olds").Value.Should().Be("2");
            result.First(item => item.Name == "number_of_2_year_olds").Value.Should().Be("3");
            result.First(item => item.Name == "number_of_3_year_olds").Value.Should().Be("4");
            result.First(item => item.Name == "number_of_4_year_olds").Value.Should().Be("5");
            result.First(item => item.Name == "number_of_5_year_olds").Value.Should().Be("6");
            result.First(item => item.Name == "number_of_kindergartners").Value.Should().Be("7");
            result.First(item => item.Name == "number_of_1st_graders").Value.Should().Be("8");
            result.First(item => item.Name == "number_of_2nd_graders").Value.Should().Be("9");
            result.First(item => item.Name == "number_of_3rd_graders").Value.Should().Be("10");
            result.First(item => item.Name == "number_of_4th_graders").Value.Should().Be("11");
            result.First(item => item.Name == "number_of_5th_graders").Value.Should().Be("12");
            result.First(item => item.Name == "number_of_6th_graders").Value.Should().Be("13");
            result.First(item => item.Name == "number_of_7th_graders").Value.Should().Be("14");
            result.First(item => item.Name == "number_of_8th_graders").Value.Should().Be("15");
            result.First(item => item.Name == "number_of_9th_graders").Value.Should().Be("16");
            result.First(item => item.Name == "number_of_10th_graders").Value.Should().Be("17");
            result.First(item => item.Name == "number_of_11th_graders").Value.Should().Be("18");
            result.First(item => item.Name == "number_of_12th_graders").Value.Should().Be("19");
            result.First(item => item.Name == "number_of_graduated_seniors").Value.Should().Be("20");
            result.First(item => item.Name == "source").Value.Should().Be("MP_Sync_Kids_Club_&_Student_Ministry_Update");
        }

        [Fact]
        public void should_append_environment_and_lifecycle_stage()
        {
            var result = new SerialHubSpotContact();
            MappingExpressionExtensions.AddTangentialAttributesToHubSpotProperties(result, "dev");

            result.Properties.First(item => item.Name == "environment").Value.Should().Be("dev");
            result.Properties.First(item => item.Name == "lifecyclestage").Value.Should().Be("customer");
        }

        [Fact]
        public void should_append_environment_and_lifecycle_stage_when_they_dont_already_exist()
        {
            var result = new SerialHubSpotContact
            {
                Properties = new List<HubSpotContactProperty>
                {
                    new HubSpotContactProperty
                    {
                        Name = "environment",
                        Value = "PRODUCTION"
                    },
                    new HubSpotContactProperty
                    {
                        Name = "lifecyclestage",
                        Value = "boss!"
                    }
                }
            };
            MappingExpressionExtensions.AddTangentialAttributesToHubSpotProperties(result, "dev");

            result.Properties.First(item => item.Name == "environment").Value.Should().Be("PRODUCTION");
            result.Properties.First(item => item.Name == "lifecyclestage").Value.Should().Be("boss!");
        }
    }
}