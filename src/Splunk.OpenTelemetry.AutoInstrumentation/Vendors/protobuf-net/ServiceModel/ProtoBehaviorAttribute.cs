//------------------------------------------------------------------------------
// <auto-generated />
// Vendored from https://github.com/protobuf-net/protobuf-net/archive/refs/tags/2.4.8.zip
//------------------------------------------------------------------------------
#if FEAT_SERVICEMODEL && PLAT_XMLSERIALIZER
using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Splunk.OpenTelemetry.AutoInstrumentation.Vendors.ProtoBuf.ServiceModel
{
    /// <summary>
    /// Uses protocol buffer serialization on the specified operation; note that this
    /// must be enabled on both the client and server.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal sealed class ProtoBehaviorAttribute : Attribute, IOperationBehavior
    {
        void IOperationBehavior.AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
        { }

        void IOperationBehavior.ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
            IOperationBehavior innerBehavior = new ProtoOperationBehavior(operationDescription);
            innerBehavior.ApplyClientBehavior(operationDescription, clientOperation);
        }

        void IOperationBehavior.ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            IOperationBehavior innerBehavior = new ProtoOperationBehavior(operationDescription);
            innerBehavior.ApplyDispatchBehavior(operationDescription, dispatchOperation);
        }

        void IOperationBehavior.Validate(OperationDescription operationDescription)
        { }
    }
}
#endif