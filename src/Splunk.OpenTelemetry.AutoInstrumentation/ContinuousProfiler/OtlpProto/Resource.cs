// <auto-generated>
//   This file was generated by a tool; you should avoid making direct changes.
//   Consider using 'partial classes' to extend these types
//   Input: resource.proto
// </auto-generated>

// Vendored namespaces adjusted: global::ProtoBuf. changed to global::Splunk.OpenTelemetry.AutoInstrumentation.Vendors.ProtoBuf.

#region Designer generated code
#pragma warning disable CS0612, CS0618, CS1591, CS3021, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
namespace Splunk.OpenTelemetry.AutoInstrumentation.Proto.Resource.V1
{

    [global::Splunk.OpenTelemetry.AutoInstrumentation.Vendors.ProtoBuf.ProtoContract()]
    public partial class Resource : global::Splunk.OpenTelemetry.AutoInstrumentation.Vendors.ProtoBuf.IExtensible
    {
        private global::Splunk.OpenTelemetry.AutoInstrumentation.Vendors.ProtoBuf.IExtension __pbn__extensionData;
        global::Splunk.OpenTelemetry.AutoInstrumentation.Vendors.ProtoBuf.IExtension global::Splunk.OpenTelemetry.AutoInstrumentation.Vendors.ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
            => global::Splunk.OpenTelemetry.AutoInstrumentation.Vendors.ProtoBuf.Extensible.GetExtensionObject(ref __pbn__extensionData, createIfMissing);

        [global::Splunk.OpenTelemetry.AutoInstrumentation.Vendors.ProtoBuf.ProtoMember(1, Name = @"attributes")]
        public global::System.Collections.Generic.List<global::Splunk.OpenTelemetry.AutoInstrumentation.Proto.Common.V1.KeyValue> Attributes { get; } = new global::System.Collections.Generic.List<global::Splunk.OpenTelemetry.AutoInstrumentation.Proto.Common.V1.KeyValue>();

        [global::Splunk.OpenTelemetry.AutoInstrumentation.Vendors.ProtoBuf.ProtoMember(2, Name = @"dropped_attributes_count")]
        public uint DroppedAttributesCount { get; set; }

    }

}

#pragma warning restore CS0612, CS0618, CS1591, CS3021, IDE0079, IDE1006, RCS1036, RCS1057, RCS1085, RCS1192
#endregion