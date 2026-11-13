using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.Models
{
    /// <summary>
    /// The municipal departments, the issue categories each one handles (the same
    /// categories residents choose on the Report Issues page) and the field team
    /// that does the work. This is the shape of the department tree on the
    /// Service Request Status page.
    /// </summary>
    public static class ServiceDepartments
    {
        public const string MunicipalityName = "City of Tshwane";

        // Departments
        public const string WaterAndSanitation = "Water and Sanitation";
        public const string EnergyAndElectricity = "Energy and Electricity";
        public const string RoadsAndTransport = "Roads and Transport";
        public const string EnvironmentAndWaste = "Environment and Waste";
        public const string GeneralServices = "General Services";

        // Categories (match ReportIssuesForm)
        public const string WaterAndSewerage = "Water and Sewerage";
        public const string Sanitation = "Sanitation";
        public const string Electricity = "Electricity";
        public const string StreetLighting = "Street Lighting";
        public const string RoadsAndPotholes = "Roads and Potholes";
        public const string RefuseRemoval = "Refuse Removal";
        public const string ParksAndPublicSpaces = "Parks and Public Spaces";
        public const string Other = "Other";

        private sealed class CategoryInfo
        {
            public CategoryInfo(string department, string team, RequestPriority defaultPriority)
            {
                Department = department;
                Team = team;
                DefaultPriority = defaultPriority;
            }

            public string Department { get; }
            public string Team { get; }
            public RequestPriority DefaultPriority { get; }
        }

        private static readonly Dictionary<string, CategoryInfo> Categories =
            new Dictionary<string, CategoryInfo>(StringComparer.OrdinalIgnoreCase)
            {
                { WaterAndSewerage, new CategoryInfo(WaterAndSanitation, "Water Response Team", RequestPriority.High) },
                { Sanitation, new CategoryInfo(WaterAndSanitation, "Sanitation Crew", RequestPriority.High) },
                { Electricity, new CategoryInfo(EnergyAndElectricity, "Electrical Maintenance Team", RequestPriority.High) },
                { StreetLighting, new CategoryInfo(EnergyAndElectricity, "Public Lighting Team", RequestPriority.Medium) },
                { RoadsAndPotholes, new CategoryInfo(RoadsAndTransport, "Roads Maintenance Crew", RequestPriority.Medium) },
                { RefuseRemoval, new CategoryInfo(EnvironmentAndWaste, "Waste Collection Unit", RequestPriority.Medium) },
                { ParksAndPublicSpaces, new CategoryInfo(EnvironmentAndWaste, "Parks and Horticulture Team", RequestPriority.Low) },
                { Other, new CategoryInfo(GeneralServices, "Customer Care Office", RequestPriority.Low) }
            };

        // Words in a resident's description that make a report an emergency.
        private static readonly string[] EmergencyWords =
        {
            "burst", "flood", "sewage", "sparking", "live wire", "exposed cable", "no water",
            "no power", "fire", "collapsed", "sinkhole", "danger"
        };

        /// <summary>
        /// Departments in display order, each with the categories it handles.
        /// </summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string[]>> Structure =
            new List<KeyValuePair<string, string[]>>
            {
                new KeyValuePair<string, string[]>(WaterAndSanitation, new[] { WaterAndSewerage, Sanitation }),
                new KeyValuePair<string, string[]>(EnergyAndElectricity, new[] { Electricity, StreetLighting }),
                new KeyValuePair<string, string[]>(RoadsAndTransport, new[] { RoadsAndPotholes }),
                new KeyValuePair<string, string[]>(EnvironmentAndWaste, new[] { RefuseRemoval, ParksAndPublicSpaces }),
                new KeyValuePair<string, string[]>(GeneralServices, new[] { Other })
            };

        public static IEnumerable<string> Departments
        {
            get
            {
                foreach (KeyValuePair<string, string[]> department in Structure)
                {
                    yield return department.Key;
                }
            }
        }

        public static string GetDepartment(string category)
        {
            return Categories.TryGetValue(category ?? string.Empty, out CategoryInfo? info)
                ? info.Department
                : GeneralServices;
        }

        public static string GetTeam(string category)
        {
            return Categories.TryGetValue(category ?? string.Empty, out CategoryInfo? info)
                ? info.Team
                : "Customer Care Office";
        }

        /// <summary>
        /// The category's usual priority, raised to Critical if the description
        /// mentions an emergency such as a burst pipe or exposed cables.
        /// </summary>
        public static RequestPriority AssessPriority(string category, string description)
        {
            RequestPriority priority = Categories.TryGetValue(category ?? string.Empty, out CategoryInfo? info)
                ? info.DefaultPriority
                : RequestPriority.Low;

            string text = (description ?? string.Empty).ToLowerInvariant();
            foreach (string word in EmergencyWords)
            {
                if (text.Contains(word))
                {
                    return RequestPriority.Critical;
                }
            }

            return priority;
        }
    }
}
