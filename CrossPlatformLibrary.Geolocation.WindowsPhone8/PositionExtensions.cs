﻿using System.Device.Location;

using Guards;

namespace CrossPlatformLibrary.Geolocation.WinRT
{
    public static class PositionExtensions
    {
        public static GeoCoordinate ToGeoCoordinate(this Position position)
        {
            Guard.ArgumentNotNull(position, "position");

            return new GeoCoordinate
                       {
                           Altitude = position.Altitude,
                           Course = position.Heading,
                           HorizontalAccuracy = position.Accuracy,
                           VerticalAccuracy = position.Accuracy,
                           Latitude = position.Latitude,
                           Longitude = position.Longitude,
                           Speed = position.Speed
                       };
        }

        public static Position ToPosition(this GeoCoordinate geoCoordinate)
        {
            Guard.ArgumentNotNull(geoCoordinate, "geoCoordinate");

            return new Position
                       {
                           Altitude = geoCoordinate.Altitude,
                           Heading = geoCoordinate.Course,
                           Accuracy = (geoCoordinate.HorizontalAccuracy / 2) + (geoCoordinate.VerticalAccuracy / 2),
                           Latitude = geoCoordinate.Latitude,
                           Longitude = geoCoordinate.Longitude,
                           Speed = geoCoordinate.Speed
                       };
        }
    }
}