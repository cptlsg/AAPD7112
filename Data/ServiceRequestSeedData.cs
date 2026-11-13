using System;
using System.Collections.Generic;
using MunicipalServicesApp.Models;

namespace MunicipalServicesApp.Data
{
    /// <summary>
    /// Sample service requests and the Tshwane road network the Service Request
    /// Status page starts with. Dates are relative to today, so the data always
    /// looks current. Reference numbers are not written here: the manager issues
    /// them in date order, the same way the Report Issues page does.
    /// </summary>
    public static class ServiceRequestSeedData
    {
        /// <summary>
        /// The suburb the field crews leave from.
        /// </summary>
        public const string Depot = "Pretoria CBD";

        /// <summary>
        /// Roads are not straight lines, so straight-line distances are scaled up
        /// by this factor to estimate driving distance.
        /// </summary>
        public const double RoadDistanceFactor = 1.3;

        /// <summary>
        /// One sample request. Key is only used to link dependencies below.
        /// </summary>
        public sealed class SeedRequest
        {
            public SeedRequest(int key, int daysAgo, int hour, int minute, string title, string category,
                string suburb, string street, string description, RequestPriority priority,
                RequestStatus status, params int[] dependsOn)
            {
                Key = key;
                DaysAgo = daysAgo;
                Hour = hour;
                Minute = minute;
                Title = title;
                Category = category;
                Suburb = suburb;
                Street = street;
                Description = description;
                Priority = priority;
                Status = status;
                DependsOn = dependsOn ?? Array.Empty<int>();
            }

            public int Key { get; }
            public int DaysAgo { get; }
            public int Hour { get; }
            public int Minute { get; }
            public string Title { get; }
            public string Category { get; }
            public string Suburb { get; }
            public string Street { get; }
            public string Description { get; }
            public RequestPriority Priority { get; }
            public RequestStatus Status { get; }

            /// <summary>
            /// Keys of requests that must be resolved before this one can be worked on.
            /// </summary>
            public int[] DependsOn { get; }
        }

        public static List<SuburbLocation> CreateSuburbs()
        {
            return new List<SuburbLocation>
            {
                new SuburbLocation("Pretoria CBD", -25.7463, 28.1880),
                new SuburbLocation("Arcadia", -25.7445, 28.2075),
                new SuburbLocation("Sunnyside", -25.7560, 28.2040),
                new SuburbLocation("Hatfield", -25.7490, 28.2370),
                new SuburbLocation("Brooklyn", -25.7700, 28.2370),
                new SuburbLocation("Menlo Park", -25.7680, 28.2590),
                new SuburbLocation("Lynnwood", -25.7640, 28.2860),
                new SuburbLocation("Waterkloof", -25.7920, 28.2440),
                new SuburbLocation("Silverton", -25.7320, 28.2930),
                new SuburbLocation("Gezina", -25.7190, 28.2080),
                new SuburbLocation("Pretoria West", -25.7520, 28.1480),
                new SuburbLocation("Centurion", -25.8600, 28.1890),
                new SuburbLocation("Mamelodi", -25.7110, 28.3950),
                new SuburbLocation("Atteridgeville", -25.7720, 28.0720)
            };
        }

        /// <summary>
        /// Pairs of suburbs joined directly by a main road. Distances are worked
        /// out from the suburbs' coordinates.
        /// </summary>
        public static List<KeyValuePair<string, string>> CreateRoads()
        {
            return new List<KeyValuePair<string, string>>
            {
                Road("Pretoria CBD", "Arcadia"),
                Road("Pretoria CBD", "Sunnyside"),
                Road("Pretoria CBD", "Pretoria West"),
                Road("Pretoria CBD", "Gezina"),
                Road("Pretoria CBD", "Centurion"),
                Road("Arcadia", "Sunnyside"),
                Road("Arcadia", "Hatfield"),
                Road("Arcadia", "Gezina"),
                Road("Sunnyside", "Brooklyn"),
                Road("Sunnyside", "Hatfield"),
                Road("Hatfield", "Brooklyn"),
                Road("Hatfield", "Menlo Park"),
                Road("Hatfield", "Silverton"),
                Road("Brooklyn", "Menlo Park"),
                Road("Brooklyn", "Waterkloof"),
                Road("Menlo Park", "Lynnwood"),
                Road("Menlo Park", "Waterkloof"),
                Road("Lynnwood", "Silverton"),
                Road("Lynnwood", "Mamelodi"),
                Road("Silverton", "Mamelodi"),
                Road("Silverton", "Gezina"),
                Road("Pretoria West", "Atteridgeville"),
                Road("Atteridgeville", "Centurion"),
                Road("Centurion", "Waterkloof")
            };
        }

        private static KeyValuePair<string, string> Road(string a, string b)
        {
            return new KeyValuePair<string, string>(a, b);
        }

        public static List<SeedRequest> CreateRequests()
        {
            return new List<SeedRequest>
            {
                // Older requests, mostly finished.
                new SeedRequest(1, 400, 9, 15, "Leaking water meter", ServiceDepartments.WaterAndSewerage,
                    "Arcadia", "Park Street",
                    "Water is leaking from the meter box on the pavement.",
                    RequestPriority.Medium, RequestStatus.Closed),
                new SeedRequest(2, 380, 7, 40, "Pothole outside primary school", ServiceDepartments.RoadsAndPotholes,
                    "Sunnyside", "Esselen Street",
                    "Deep pothole at the school drop-off zone. Cars swerve into the oncoming lane to avoid it.",
                    RequestPriority.High, RequestStatus.Closed),
                new SeedRequest(3, 360, 18, 5, "Streetlights out along Lynnwood Road", ServiceDepartments.StreetLighting,
                    "Lynnwood", "Lynnwood Road",
                    "About six streetlights in a row have been off for a week.",
                    RequestPriority.Medium, RequestStatus.Closed),
                new SeedRequest(4, 340, 11, 30, "Refuse not collected for two weeks", ServiceDepartments.RefuseRemoval,
                    "Mamelodi", "Tsamaya Avenue",
                    "Bins on the whole street have not been emptied for two collection days.",
                    RequestPriority.Medium, RequestStatus.Closed),
                new SeedRequest(5, 325, 14, 0, "Overgrown grass in community park", ServiceDepartments.ParksAndPublicSpaces,
                    "Waterkloof", "Albert Street",
                    "Grass in the park is knee-high and hiding litter.",
                    RequestPriority.Low, RequestStatus.Closed),
                new SeedRequest(6, 200, 6, 50, "Sewer manhole overflowing", ServiceDepartments.Sanitation,
                    "Gezina", "Frederika Street",
                    "Sewage is overflowing from a manhole into the street.",
                    RequestPriority.Critical, RequestStatus.Closed),
                new SeedRequest(7, 150, 10, 10, "Illegal dumping next to the taxi rank", ServiceDepartments.RefuseRemoval,
                    "Atteridgeville", "Komane Street",
                    "Building rubble and household waste dumped beside the taxi rank.",
                    RequestPriority.Medium, RequestStatus.Resolved),
                new SeedRequest(8, 120, 16, 45, "Prepaid meter not accepting tokens", ServiceDepartments.Electricity,
                    "Pretoria West", "Mitchell Street",
                    "The meter rejects every token with an error code.",
                    RequestPriority.Medium, RequestStatus.Closed),
                new SeedRequest(9, 95, 8, 20, "Stormwater drain cover missing", ServiceDepartments.RoadsAndPotholes,
                    "Brooklyn", "Bronkhorst Street",
                    "An open stormwater drain on the pavement is a hazard for pedestrians.",
                    RequestPriority.High, RequestStatus.Closed),
                new SeedRequest(10, 60, 13, 35, "Query about water meter reading", ServiceDepartments.Other,
                    "Hatfield", "Hilda Street",
                    "The latest bill shows a meter reading far higher than the meter itself.",
                    RequestPriority.Low, RequestStatus.Resolved),

                // Church Street, Arcadia: the road cannot be repaired until the pipe
                // underneath is fixed, and the markings wait for the road.
                new SeedRequest(11, 45, 5, 55, "Burst water main", ServiceDepartments.WaterAndSewerage,
                    "Arcadia", "Church Street",
                    "Water is gushing from the road near the corner of Church and Beckett Street.",
                    RequestPriority.Critical, RequestStatus.InProgress),
                new SeedRequest(12, 44, 9, 0, "Road surface collapsed after water leak", ServiceDepartments.RoadsAndPotholes,
                    "Arcadia", "Church Street",
                    "The tar has caved in where the water main burst. One lane is closed.",
                    RequestPriority.High, RequestStatus.Acknowledged, 11),
                new SeedRequest(13, 40, 15, 20, "Road markings missing after repairs", ServiceDepartments.RoadsAndPotholes,
                    "Arcadia", "Church Street",
                    "Lane markings and the pedestrian crossing will need repainting once the road is fixed.",
                    RequestPriority.Low, RequestStatus.Submitted, 12),

                // Silverton substation: lights and signals wait for the power to be restored.
                new SeedRequest(14, 30, 19, 10, "Substation fault leaves area without power", ServiceDepartments.Electricity,
                    "Silverton", "Moreleta Street",
                    "No power in several streets since a loud bang at the substation.",
                    RequestPriority.Critical, RequestStatus.InProgress),
                new SeedRequest(15, 29, 20, 30, "Streetlights out on Moreleta Street", ServiceDepartments.StreetLighting,
                    "Silverton", "Moreleta Street",
                    "The whole street is dark at night.",
                    RequestPriority.High, RequestStatus.Assigned, 14),
                new SeedRequest(16, 28, 7, 15, "Traffic signals dark at Moreleta Street intersection", ServiceDepartments.Electricity,
                    "Silverton", "Moreleta Street",
                    "Traffic lights are off and the intersection is congested at peak times.",
                    RequestPriority.High, RequestStatus.Submitted, 14),

                // Sunnyside: the sewer has been fixed, so the playground clean-up can go ahead.
                new SeedRequest(17, 25, 8, 45, "Blocked sewer line", ServiceDepartments.Sanitation,
                    "Sunnyside", "Rissik Street",
                    "Toilets in the block of flats are backing up.",
                    RequestPriority.Critical, RequestStatus.Resolved),
                new SeedRequest(18, 24, 12, 0, "Playground contaminated after sewer overflow", ServiceDepartments.ParksAndPublicSpaces,
                    "Sunnyside", "Rissik Street",
                    "The play area next to the flats needs cleaning and the sand replaced.",
                    RequestPriority.Medium, RequestStatus.InProgress, 17),

                // Other current requests.
                new SeedRequest(19, 20, 6, 30, "No water supply for three days", ServiceDepartments.WaterAndSewerage,
                    "Mamelodi", "Serene Street",
                    "Taps have been dry for three days. Elderly residents need water.",
                    RequestPriority.Critical, RequestStatus.Assigned),
                new SeedRequest(20, 18, 17, 40, "Large pothole damaging vehicles", ServiceDepartments.RoadsAndPotholes,
                    "Centurion", "Jean Avenue",
                    "Several cars have had burst tyres at this pothole.",
                    RequestPriority.High, RequestStatus.InProgress),
                new SeedRequest(21, 15, 21, 5, "Exposed electrical cables at street pole", ServiceDepartments.Electricity,
                    "Hatfield", "Burnett Street",
                    "The cover of the junction box is missing and cables are exposed near a bus stop.",
                    RequestPriority.Critical, RequestStatus.Acknowledged),
                new SeedRequest(22, 12, 10, 25, "Broken swings and bench in park", ServiceDepartments.ParksAndPublicSpaces,
                    "Menlo Park", "13th Street",
                    "Two swings have snapped chains and a bench is broken.",
                    RequestPriority.Low, RequestStatus.Submitted),
                new SeedRequest(23, 10, 9, 50, "Refuse collection missed this week", ServiceDepartments.RefuseRemoval,
                    "Pretoria West", "Mitchell Street",
                    "The truck did not come on the usual collection day.",
                    RequestPriority.Medium, RequestStatus.Acknowledged),
                new SeedRequest(24, 8, 14, 15, "Fire hydrant leaking", ServiceDepartments.WaterAndSewerage,
                    "Brooklyn", "Fehrsen Street",
                    "A steady stream of water is running from the hydrant into the gutter.",
                    RequestPriority.High, RequestStatus.Submitted),
                new SeedRequest(25, 6, 19, 45, "Streetlight flickering at night", ServiceDepartments.StreetLighting,
                    "Waterkloof", "Lawley Street",
                    "One streetlight keeps switching on and off.",
                    RequestPriority.Low, RequestStatus.Submitted),
                new SeedRequest(26, 5, 11, 5, "Illegal dumping on open stand", ServiceDepartments.RefuseRemoval,
                    "Atteridgeville", "Phudufudu Street",
                    "Garden waste and old furniture are being dumped on the empty stand.",
                    RequestPriority.Medium, RequestStatus.Submitted),
                new SeedRequest(27, 4, 7, 35, "Sinkhole forming on road verge", ServiceDepartments.RoadsAndPotholes,
                    "Centurion", "Lenchen Avenue",
                    "A hole about a metre wide has opened next to the road and is getting bigger.",
                    RequestPriority.Critical, RequestStatus.Acknowledged),
                new SeedRequest(28, 3, 16, 0, "Low water pressure in the mornings", ServiceDepartments.WaterAndSewerage,
                    "Lynnwood", "Lynnwood Road",
                    "Water pressure drops to a trickle between 6 and 9 every morning.",
                    RequestPriority.Medium, RequestStatus.Submitted),
                new SeedRequest(29, 2, 8, 10, "Stop sign knocked down", ServiceDepartments.RoadsAndPotholes,
                    "Gezina", "Frederika Street",
                    "The stop sign at the four-way stop is lying on the pavement.",
                    RequestPriority.High, RequestStatus.Submitted),
                new SeedRequest(30, 1, 13, 30, "Noise from municipal pump station", ServiceDepartments.Other,
                    "Menlo Park", "Atterbury Road",
                    "The pump station has been making a loud grinding noise day and night.",
                    RequestPriority.Low, RequestStatus.Submitted),
                new SeedRequest(31, 0, 7, 0, "Sewer smell near the clinic", ServiceDepartments.Sanitation,
                    "Gezina", "Adcock Street",
                    "A strong sewer smell near the clinic entrance.",
                    RequestPriority.Medium, RequestStatus.Submitted)
            };
        }
    }
}
