using System.Fabric;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.ServiceFabric;
using Microsoft.ServiceFabric.Services.Remoting.V2;
using Microsoft.ServiceFabric.Services.Remoting.V2.Runtime;

namespace Eshopworld.Telemetry.ServiceFabric
{
    /// <summary>
    /// Proxy dispatcher which can initialize request telemetry
    /// </summary>
    internal class TelemetryContextInitializingDispatcher : IServiceRemotingMessageHandler
    {
        private readonly IServiceRemotingMessageHandler _handler;
        private readonly ServiceContext _context;

        public TelemetryContextInitializingDispatcher(IServiceRemotingMessageHandler handler, ServiceContext context)
        {
            _handler = handler;
            _context = context;
        }

        public IServiceRemotingMessageBodyFactory GetRemotingMessageBodyFactory() => _handler.GetRemotingMessageBodyFactory();

        public void HandleOneWayMessage(IServiceRemotingRequestMessage requestMessage)
        {
            FabricTelemetryInitializerExtension.SetServiceCallContext(_context);
            _handler.HandleOneWayMessage(requestMessage);
        }

        public Task<IServiceRemotingResponseMessage> HandleRequestResponseAsync(IServiceRemotingRequestContext requestContext, IServiceRemotingRequestMessage requestMessage)
        {
            FabricTelemetryInitializerExtension.SetServiceCallContext(_context);
            return _handler.HandleRequestResponseAsync(requestContext, requestMessage);
        }
    }
}
