﻿using System.Globalization;
using CrossPlatformLibrary.Dispatching;
using CrossPlatformLibrary.Geolocation;
using CrossPlatformLibrary.IoC;
using Tracing;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace GeolocationSample.UWP
{
    /// <summary>
    ///     Example code for the usage of ILocationService in an UWP app.
    ///     Original UWP source code:
    ///     https://github.com/Microsoft/Windows-universal-samples/tree/master/Samples/Geolocation/cs/GeolocationCS
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly ILocationService locationService;
        private readonly IDispatcherService dispatcherService;
        private readonly ITracer tracer;
   
        public MainPage()
        {
            this.InitializeComponent();

            this.locationService = SimpleIoc.Default.GetInstance<ILocationService>();
            this.dispatcherService = SimpleIoc.Default.GetInstance<IDispatcherService>();
            this.tracer = Tracer.Create(this);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.StartTrackingButton.IsEnabled = true;
            this.StopTrackingButton.IsEnabled = false;
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (this.StopTrackingButton.IsEnabled)
            {
                this.locationService.PositionChanged -= this.OnPositionChanged;
                this.locationService.PositionError -= this.OnPositionError;
            }

            base.OnNavigatingFrom(e);
        }

        /// <summary>
        ///     This is the click handler for the 'StartTracking' button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartTracking(object sender, RoutedEventArgs e)
        {
            // You should set MovementThreshold for distance-based tracking
            // or ReportInterval for periodic-based tracking before adding event
            // handlers. If none is set, a ReportInterval of 1 second is used
            // as a default and a position will be returned every 1 second.
            //
            // Value of 2000 milliseconds (2 seconds) 
            // isn't a requirement, it is just an example.

            // Subscribe to PositionChanged event to get updated tracking positions
            this.locationService.PositionChanged += this.OnPositionChanged;
            this.locationService.PositionError += this.OnPositionError;
            this.locationService.StartListening(2000, 2000);

            this.tracer.Info("Waiting for update...");
            this.LocationDisabledMessage.Visibility = Visibility.Collapsed;
            this.StartTrackingButton.IsEnabled = false;
            this.StopTrackingButton.IsEnabled = true;
        }

        private void OnPositionError(object sender, PositionErrorEventArgs e)
        {
            // template to load for showing Toast Notification
            var xmlToastTemplate = "<toast launch=\"app-defined-string\">" + "<visual>" + "<binding template =\"ToastGeneric\">" + "<text>OnPositionError</text>" + "<text>"
                                   + "A position error has occurred: " + e.GeolocationException.Message + "</text>" + "</binding>" + "</visual>" + "</toast>";

            // load the template as XML document
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlToastTemplate);

            var toastNotification = new ToastNotification(xmlDocument);
            var notification = ToastNotificationManager.CreateToastNotifier();
            notification.Show(toastNotification);
        }

        /// <summary>
        ///     This is the click handler for the 'StopTracking' button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopTracking(object sender, RoutedEventArgs e)
        {
            this.locationService.PositionChanged -= this.OnPositionChanged;
            this.locationService.PositionError -= this.OnPositionError;
            this.locationService.StopListening();

            this.StartTrackingButton.IsEnabled = true;
            this.StopTrackingButton.IsEnabled = false;
        }

        /// <summary>
        ///     Event handler for PositionChanged events. It is raised when
        ///     a location is available for the tracking session specified.
        /// </summary>
        /// <param name="sender">Geolocator instance</param>
        /// <param name="e">Position data</param>
        private void OnPositionChanged(object sender, PositionEventArgs e)
        {
            this.dispatcherService.CheckBeginInvokeOnUI(
                () =>
                    {
                        this.tracer.Info("Location updated.");
                        this.UpdateLocationData(e.Position);
                    });
        }

        /// <summary>
        ///     Updates the user interface with the Geoposition data provided
        /// </summary>
        /// <param name="position">Geoposition to display its details</param>
        private void UpdateLocationData(Position position)
        {
            if (position == null)
            {
                this.Latitude.Text = "No data";
                this.Longitude.Text = "No data";
                this.Accuracy.Text = "No data";
            }
            else
            {
                this.Latitude.Text = position.Latitude.ToString(CultureInfo.CurrentCulture);
                this.Longitude.Text = position.Longitude.ToString(CultureInfo.CurrentCulture);
                this.Accuracy.Text = position.Accuracy.ToString(CultureInfo.CurrentCulture);
            }
        }
    }
}