using AutoMapper;
using Crossroads.Service.HubSpot.Sync.Data.HubSpot.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.AutoMapper
{
    public static class MappingExpressionExtensions
    {
        /// <summary>
        /// Encapsulates the logic for transforming Ministry Platform contact attributes to HubSpot properties.
        /// Also adds the "environment" and "lifecyclestage" properties to the <see cref="IHubSpotContact.Properties"/>
        /// collection.
        /// </summary>
        /// <typeparam name="TMpContact">Ministry Platform contact type to transform.</typeparam>
        /// <typeparam name="THubSpotContact">HubSpot type to which we will transform the MP contact attributes.</typeparam>
        /// <param name="expression">Mapping expression to be manipulated.</param>
        /// <param name="sourceEmailSelector">The means by which we source our email address to pass into the HubSpot contact instance.</param>
        /// <param name="environment">Environment in which the app is being executed.</param>
        public static void MapMpContactToHubSpotContact<TMpContact, THubSpotContact>(
            this IMappingExpression<TMpContact, THubSpotContact> expression,
            Func<TMpContact, string> sourceEmailSelector,
            string environment)
            where THubSpotContact : IHubSpotContact
        {
            expression
                .ForMember(hubSpotContact => hubSpotContact.Email, memberOptions => memberOptions.MapFrom(dto => sourceEmailSelector(dto)))
                .ForMember(hubSpotContact => hubSpotContact.Properties, memberOptions => memberOptions.MapFrom(mpContact => ReflectToContactProperties(mpContact)))
                .AfterMap((mpContact, hubSpotContact) => AddTangentialAttributesToHubSpotProperties(hubSpotContact, environment));
        }

        /// <summary>
        /// Reflects over the specified argument to convert its defined object properties to an ISet collection of
        /// type <see cref="HubSpotContactProperty"/>. Goal is to sacrifice a mite of readability (hard decision to make)
        /// for the sake of simplifying future updates; should we need to add any new properties to the contact,
        /// this will account for any additions where mapping to HubSpot models is concerned. This operates under the
        /// assumption that all properties expressed in the type's definition have corrollaries in HubSpot.
        /// </summary>
        public static ISet<HubSpotContactProperty> ReflectToContactProperties<T>(T mpContactDto)
        {
            return new HashSet<HubSpotContactProperty>(mpContactDto.GetType()
                .GetInterfaces()
                .SelectMany(i => i.GetProperties().Select(propertyInfo => BuildContactProperty(propertyInfo, mpContactDto))));
        }

        private static HubSpotContactProperty BuildContactProperty(PropertyInfo property, object obj)
        {
            return new HubSpotContactProperty
            {
                Name = property.Name.ToLowerInvariant(),
                Value = property.GetValue(obj)?.ToString() ?? string.Empty
            };
        }

        /// <summary>
        /// Ensures the Environment and lifecycle stage are included in the payload to be sent to HubSpot alongside each contact.
        /// </summary>
        /// <param name="hubSpotContact">Contact to which we will assign the integration attributes.</param>
        /// <param name="environmentName">Name of the environment in which the application is executing.</param>
        public static void AddTangentialAttributesToHubSpotProperties(IHubSpotContact hubSpotContact, string environmentName)
        {
            // preserve existing properties (the HashSet will keep the data clean/unique)
            hubSpotContact.Properties = new HashSet<HubSpotContactProperty>(hubSpotContact.Properties ?? Enumerable.Empty<HubSpotContactProperty>())
            {
                // captures reference metadata to pass along when updating HubSpot contact data
                new HubSpotContactProperty
                {
                    Name = "environment",
                    Value = environmentName
                },
                new HubSpotContactProperty
                {
                    Name = "lifecyclestage",
                    Value = "customer"
                }
            }.ToList();
        }
    }
}