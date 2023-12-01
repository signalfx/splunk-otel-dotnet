# To build one auto-instrumentation image for dotnet, please:
#  - Download your dotnet auto-instrumentation artefacts to `/autoinstrumentation` directory. This is required as when instrumenting the pod,
#    one init container will be created to copy the files to your app's container.
#  - Grant the necessary access to the files in the `/autoinstrumentation` directory.
#  - Following environment variables are injected to the application container to enable the auto-instrumentation.
#    CORECLR_ENABLE_PROFILING=1
#    CORECLR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}
#    CORECLR_PROFILER_PATH=%InstallationLocation%/linux-x64/OpenTelemetry.AutoInstrumentation.Native.so # for glibc based images
#    CORECLR_PROFILER_PATH=%InstallationLocation%/linux-musl-x64/OpenTelemetry.AutoInstrumentation.Native.so # for musl based images
#    DOTNET_ADDITIONAL_DEPS=%InstallationLocation%/AdditionalDeps
#    DOTNET_SHARED_STORE=%InstallationLocation%/store
#    DOTNET_STARTUP_HOOKS=%InstallationLocation%/net/OpenTelemetry.AutoInstrumentation.StartupHook.dll 
#    OTEL_DOTNET_AUTO_HOME=%InstallationLocation%
#    OTEL_DOTNET_AUTO_PLUGINS=Splunk.OpenTelemetry.AutoInstrumentation.Plugin, Splunk.OpenTelemetry.AutoInstrumentation
#  - For auto-instrumentation by container injection, the Linux command cp is
#    used and must be availabe in the image.
FROM busybox

LABEL org.opencontainers.image.source="https://github.com/signalfx/splunk-otel-dotnet"
LABEL org.opencontainers.image.description="Splunk Distribution of OpenTelemetry .NET"

ARG RELEASE_VER

WORKDIR /autoinstrumentation

ADD https://github.com/signalfx/splunk-otel-dotnet/releases/download/$RELEASE_VER/splunk-opentelemetry-dotnet-linux-glibc.zip .
ADD https://github.com/signalfx/splunk-otel-dotnet/releases/download/$RELEASE_VER/splunk-opentelemetry-dotnet-linux-musl.zip .

RUN unzip splunk-opentelemetry-dotnet-linux-glibc.zip &&\
    unzip splunk-opentelemetry-dotnet-linux-musl.zip "linux-musl-x64/*" -d . &&\
    rm splunk-opentelemetry-dotnet-linux-glibc.zip splunk-opentelemetry-dotnet-linux-musl.zip &&\
    chmod -R go+r .
