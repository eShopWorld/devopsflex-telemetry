using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.ServiceFabric.Services.Remoting.V2;

namespace Eshopworld.Telemetry.ServiceFabric
{
    [DataContract(Name = "msgBody", Namespace = Constants.ServiceCommunicationNamespace)]
    internal class MyServiceRemotingRequestMessageBody : IServiceRemotingRequestMessageBody
    {
        [DataMember(Name = "parameters")]
        private readonly Dictionary<string, object> _parameters;

        public MyServiceRemotingRequestMessageBody(int parameterInfos)
        {
            _parameters = new Dictionary<string, object>(parameterInfos);
        }

        public void SetParameter(int position, string parameName, object parameter)
        {
            _parameters[parameName] = parameter;
        }

        public object GetParameter(int position, string parameName, Type paramType)
        {
            return _parameters[parameName];
        }
    }
}
