﻿using System;
using System.Device.Location;
using System.Threading;
using System.Threading.Tasks;

namespace CrossPlatformLibrary.Geolocation
{
    internal class SinglePositionListener
    {
        internal SinglePositionListener(double accuracy, int timeout, CancellationToken cancelToken)
        {
            cancelToken.Register(this.HandleTimeout, true);
            this.cancelToken = cancelToken;
            this.desiredAccuracy = accuracy;
            this.start = DateTime.Now;
            this.timeout = timeout;

            System.Threading.Tasks.Task.Factory.StartNew(
                () =>
                    {
                        this.watcher = new GeoCoordinateWatcher(Geolocator.GetAccuracy(accuracy));
                        this.watcher.PositionChanged += this.WatcherOnPositionChanged;
                        this.watcher.StatusChanged += this.WatcherOnStatusChanged;

                        this.watcher.Start();
                    });

            if (timeout != Timeout.Infinite)
            {
                this.timer = new Timer(this.HandleTimeout, null, timeout, Timeout.Infinite);
            }

            this.Task.ContinueWith(this.Cleanup);
        }

        public Task<Position> Task
        {
            get
            {
                return this.tcs.Task;
            }
        }

        private GeoPosition<GeoCoordinate> bestPosition;
        private GeoCoordinateWatcher watcher;
        private readonly double desiredAccuracy;
        private readonly DateTimeOffset start;
        private readonly Timer timer;
        private readonly int timeout;
        private readonly TaskCompletionSource<Position> tcs = new TaskCompletionSource<Position>();
        private readonly CancellationToken cancelToken;

        private void Cleanup(Task task)
        {
            this.watcher.PositionChanged -= this.WatcherOnPositionChanged;
            this.watcher.StatusChanged -= this.WatcherOnStatusChanged;

            this.watcher.Stop();
            this.watcher.Dispose();

            if (this.timer != null)
            {
                this.timer.Dispose();
            }
        }

        private void HandleTimeout(object state)
        {
            if (state != null && (bool)state)
            {
                this.tcs.TrySetCanceled();
            }

            if (this.bestPosition != null)
            {
                this.tcs.TrySetResult(Geolocator.GetPosition(this.bestPosition));
            }
            else
            {
                this.tcs.TrySetCanceled();
            }
        }

        private void WatcherOnStatusChanged(object sender, GeoPositionStatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case GeoPositionStatus.NoData:
                    this.tcs.TrySetException(new GeolocationException(GeolocationError.PositionUnavailable));
                    break;

                case GeoPositionStatus.Disabled:
                    this.tcs.TrySetException(new GeolocationException(GeolocationError.Unauthorized));
                    break;
            }
        }

        private void WatcherOnPositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            if (e.Position.Location.IsUnknown)
            {
                return;
            }

            bool isRecent = (e.Position.Timestamp - this.start).TotalMilliseconds < this.timeout || (this.timeout == Timeout.Infinite && this.cancelToken == CancellationToken.None);

            if (e.Position.Location.HorizontalAccuracy <= this.desiredAccuracy && isRecent)
            {
                this.tcs.TrySetResult(Geolocator.GetPosition(e.Position));
            }

            if (this.bestPosition == null || e.Position.Location.HorizontalAccuracy < this.bestPosition.Location.HorizontalAccuracy)
            {
                this.bestPosition = e.Position;
            }
        }
    }
}