using System;

namespace MunicipalServicesApp.Models
{
    /// <summary>
    /// A suburb on the road network used to plan crew routes. Coordinates are
    /// approximate and are used to draw the map and work out road distances.
    /// </summary>
    public class SuburbLocation
    {
        private const double EarthRadiusKm = 6371.0;

        public SuburbLocation(string name, double latitude, double longitude)
        {
            Name = name;
            Latitude = latitude;
            Longitude = longitude;
        }

        public string Name { get; }

        public double Latitude { get; }

        public double Longitude { get; }

        /// <summary>
        /// Straight-line distance in kilometres (haversine formula).
        /// </summary>
        public double DistanceTo(SuburbLocation other)
        {
            double lat1 = ToRadians(Latitude);
            double lat2 = ToRadians(other.Latitude);
            double deltaLat = lat2 - lat1;
            double deltaLon = ToRadians(other.Longitude - Longitude);

            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                       Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

            return 2 * EarthRadiusKm * Math.Asin(Math.Sqrt(a));
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
