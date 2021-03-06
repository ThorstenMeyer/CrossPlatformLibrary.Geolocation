﻿using System;
using System.Device.Location;
using System.Threading;
using System.Threading.Tasks;

using CrossPlatformLibrary.Geolocation.Exceptions;

using Guards;

using Tracing;

namespace CrossPlatformLibrary.Geolocation
{
    public class LocationService : ILocationService
    {
        private readonly ITracer tracer;
        private GeoCoordinateWatcher watcher;
        private bool isEnabled;
        private double desiredAccuracy = 100;

        /// <inheritdoc/>
        public event EventHandler<PositionErrorEventArgs> PositionError;

        /// <inheritdoc/>
        public event EventHandler<PositionEventArgs> PositionChanged;

        public LocationService(ITracer tracer)
        {
            Guard.ArgumentNotNull(() => tracer);

            this.tracer = tracer;
        }

        /// <inheritdoc/>
        public bool IsGeolocationAvailable
        {
            get     
            {
                return true;
            }
        }

        /// <inheritdoc/>
        public bool IsGeolocationEnabled
        {
            get
            {
                if (this.watcher != null)
                {
                    this.isEnabled = (this.watcher.Permission == GeoPositionPermission.Granted && this.watcher.Status != GeoPositionStatus.Disabled);
                }
                else
                {
                    this.isEnabled = GetEnabled();
                }

                return this.isEnabled;
            }
        }

        /// <inheritdoc/>
        public double DesiredAccuracy
        {
            get
            {
                return this.desiredAccuracy;
            }
            set
            {
                this.desiredAccuracy = value;
            }
        }

        /// <inheritdoc/>
        public bool SupportsHeading
        {
            get
            {
                return true;
            }
        }

        /// <inheritdoc/>
        public bool IsListening
        {
            get
            {
                return (this.watcher != null);
            }
        }

        /// <inheritdoc/>
        public Task<Position> GetPositionAsync(int timeoutMilliseconds = Timeout.Infinite, CancellationToken? cancelToken = null, bool includeHeading = false)
        {
            this.tracer.Debug("GetPositionAsync with timeoutMilliseconds={0}, includeHeading={1}", timeoutMilliseconds, includeHeading);

            if (!cancelToken.HasValue)
            {
                cancelToken = CancellationToken.None;
            }

            if (timeoutMilliseconds <= 0 && timeoutMilliseconds != Timeout.Infinite)
            {
                throw new ArgumentOutOfRangeException("timeoutMilliseconds", "timeout must be greater than or equal to 0");
            }

            var singlePositionListenerTask = new SinglePositionListener(this.DesiredAccuracy, timeoutMilliseconds, cancelToken.Value).Task;
            singlePositionListenerTask.ContinueWith((callback) =>
                {
                    if (callback.Status == TaskStatus.RanToCompletion)
                    {
                        this.RaisePositionChangedEvent(callback.Result);
                    }
                });
            return singlePositionListenerTask;
        }

        /// <inheritdoc/>
        public void StartListening(int minTime, double minDistance, bool includeHeading = false)
        {
            if (minTime < 0)
            {
                throw new ArgumentOutOfRangeException("minTime");
            }
            if (minDistance < 0)
            {
                throw new ArgumentOutOfRangeException("minDistance");
            }
            if (this.IsListening)
            {
                throw new InvalidOperationException("This LocationService is already listening");
            }

            this.watcher = new GeoCoordinateWatcher(GetAccuracy(this.DesiredAccuracy));
            this.watcher.MovementThreshold = minDistance;
            this.watcher.PositionChanged += this.WatcherOnPositionChanged;
            this.watcher.StatusChanged += this.WatcherOnStatusChanged;
            this.watcher.Start();
        }

        /// <inheritdoc/>
        public void StopListening()
        {
            if (this.watcher == null)
            {
                return;
            }

            this.watcher.PositionChanged -= this.WatcherOnPositionChanged;
            this.watcher.StatusChanged -= this.WatcherOnStatusChanged;
            this.watcher.Stop();
            this.watcher.Dispose();
            this.watcher = null;
        }

        private static bool GetEnabled()
        {
            var w = new GeoCoordinateWatcher();
            try
            {
                w.Start(true);
                bool enabled = (w.Permission == GeoPositionPermission.Granted && w.Status != GeoPositionStatus.Disabled);
                w.Stop();

                return enabled;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                w.Dispose();
            }
        }

        private void WatcherOnStatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            this.isEnabled = (this.watcher.Permission == GeoPositionPermission.Granted && this.watcher.Status != GeoPositionStatus.Disabled);

            GeolocationException geolocationException;
            switch (e.Status)
            {
                case GeoPositionStatus.Disabled:
                    geolocationException = new GeolocationUnauthorizedException();
                    break;

                case GeoPositionStatus.NoData:
                    geolocationException = new GeolocationPositionUnavailableException();
                    break;

                default:
                    return;
            }

            this.StopListening();

            var perror = this.PositionError;
            if (perror != null)
            {
                perror(this, new PositionErrorEventArgs(geolocationException));
            }
        }

        private void WatcherOnPositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            Position position = GetPosition(e.Position);
            if (position != null)
            {
                this.RaisePositionChangedEvent(position);
            }
        }

        private void RaisePositionChangedEvent(Position position)
        {
            var positionChangedHandler = this.PositionChanged;
            if (positionChangedHandler != null)
            {
                positionChangedHandler(this, new PositionEventArgs(position));
            }
        }

        internal static GeoPositionAccuracy GetAccuracy(double desiredAccuracy)
        {
            if (desiredAccuracy < 100)
            {
                return GeoPositionAccuracy.High;
            }

            return GeoPositionAccuracy.Default;
        }

        internal static Position GetPosition(GeoPosition<GeoCoordinate> position)
        {
            var location = position.Location;
            if (location.IsUnknown)
            {
                return Position.Unknown;
            }

            var p = new Position();
            p.Accuracy = location.HorizontalAccuracy;
            p.Longitude = location.Longitude;
            p.Latitude = location.Latitude;

            if (!Double.IsNaN(location.VerticalAccuracy) && !Double.IsNaN(location.Altitude))
            {
                p.AltitudeAccuracy = location.VerticalAccuracy;
                p.Altitude = location.Altitude;
            }

            if (!Double.IsNaN(location.Course))
            {
                p.Heading = location.Course;
            }

            if (!Double.IsNaN(location.Speed))
            {
                p.Speed = location.Speed;
            }

            p.Timestamp = position.Timestamp.ToUniversalTime();

            return p;
        }
    }
}