using System;
using System.Runtime.Serialization;
using Microsoft.ServiceFabric.Services.Remoting.V2;

namespace Eshopworld.Telemetry.ServiceFabric
{
    [DataContract(Name = "msgResponse", Namespace = Constants.ServiceCommunicationNamespace)]
    internal class MyServiceRemotingResponseMessageBody : IServiceRemotingResponseMessageBody
    {
        [DataMember(Name = "response")]
        private object _response;

        public void Set(object response)
        {
            _response = response;
        }

        public object Get(Type paramType)
        {
            return _response;
        }
    }
}
