using System;
using System.Collections.Generic;
using System.Fabric;
using Eshopworld.Telemetry.ServiceFabric;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.Runtime;

namespace Eshopworld.Telemetry
{
    /// <summary>
    /// Initializes telemetry and sets fabric TCP transport as the default remoting provider for the services.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class FabricTransportServiceRemotingProviderWithTelemetryAttribute : FabricTransportServiceRemotingProviderAttribute
    {
        public override Dictionary<string, Func<ServiceContext, IService, IServiceRemotingListener>> CreateServiceRemotingListeners()
        {
            if (RemotingListenerVersion != RemotingListenerVersion.V2)
                throw new InvalidOperationException($"The RemotingListenerVersion property must be set to V2 (the default value). No other values are supported.");

            return new Dictionary<string, Func<ServiceContext, IService, IServiceRemotingListener>>
            {
                { Constants.ListenerNameV2, CreateListener },
            };
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The lifecycle of the ServiceRemotingMessageDispatcher depends on the remoting infrastructure and it cannot be disposed manually.")]
        private IServiceRemotingListener CreateListener(ServiceContext serviceContext, IService service)
        {
            // Create a standard remoting dispatcher with a proxy which initializes the request telemetry
            var dispatcher = new TelemetryContextInitializingDispatcher(new ServiceRemotingMessageDispatcher(serviceContext, service, new DataContractRemotingMessageFactory()), serviceContext);
            return new FabricTransportServiceRemotingListener(serviceContext, dispatcher);
        }
    }
}
