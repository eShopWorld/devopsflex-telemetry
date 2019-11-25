using System;
using System.Collections.Generic;
using Eshopworld.Telemetry.ServiceFabric;
using Microsoft.ServiceFabric.Actors.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Actors.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Actors.Remoting.V2.Runtime;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;

namespace Eshopworld.Telemetry
{
    /// <summary>
    /// Initializes telemetry and sets fabric TCP transport as the default remoting provider for the actors.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class FabricTransportActorRemotingProviderWithTelemetryAttribute : FabricTransportActorRemotingProviderAttribute
    {
        public override Dictionary<string, Func<ActorService, IServiceRemotingListener>> CreateServiceRemotingListeners()
        {
            if (RemotingListenerVersion != Microsoft.ServiceFabric.Services.Remoting.RemotingListenerVersion.V2)
                throw new InvalidOperationException($"The RemotingListenerVersion property must be set to V2 (the default value). No other values are supported.");

            return new Dictionary<string, Func<ActorService, IServiceRemotingListener>>
            {
                { Constants.ListenerNameV2, CreateListner },
            };
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The lifecycle of the ActorServiceRemotingDispatcher depends on the remoting infrastructure and it cannot be disposed manually.")]
        private IServiceRemotingListener CreateListner(ActorService actorService)
        {
            // Create a standard remoting dispatcher with a proxy which initializes the request telemetry
            var dispatcher = new TelemetryContextInitializingDispatcher(new ActorServiceRemotingDispatcher(actorService, new DataContractRemotingMessageFactory()), actorService.Context);
            return new FabricTransportActorServiceRemotingListener(actorService, dispatcher);
        }
    }
}
