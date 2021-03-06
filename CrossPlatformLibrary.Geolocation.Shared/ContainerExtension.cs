﻿
using CrossPlatformLibrary.IoC;

namespace CrossPlatformLibrary.Geolocation
{
    public class ContainerExtension : IContainerExtension
    {
        public void Initialize(ISimpleIoc container)
        {
            container.Register<ILocationService, LocationService>();
            container.Register<ILocationServiceSettings, LocationServiceSettings>();
        }
    }
}
