using System;
using System.Threading;
using System.Threading.Tasks;

namespace CrossPlatformLibrary.Geolocation
{
    public interface ILocationService
    {
        event EventHandler<PositionErrorEventArgs> PositionError;

        event EventHandler<PositionEventArgs> PositionChanged;

        double DesiredAccuracy { get; set; }

        bool IsListening { get; }

        bool SupportsHeading { get; }

        bool IsGeolocationAvailable { get; }

        bool IsGeolocationEnabled { get; }

        Task<Position> GetPositionAsync(int timeout = Timeout.Infinite, CancellationToken? token = null, bool includeHeading = false);

        void StartListening(int minTime, double minDistance, bool includeHeading = false);

        void StopListening();
    }
}