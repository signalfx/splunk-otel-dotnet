# To build one auto-instrumentation image for dotnet, please:
#  - Download your dotnet auto-instrumentation artifacts to `/autoinstrumentation` 
#    directory. This is required as when instrumenting the pod, one init 
#    container will be created to copy the files to your app's container.
#  - Symlink '/autoinstrumentation/splunk-opentelemetry-dotnet-linux-glibc' to `/autoinstrumentation/opentelemetry-dotnet-instrumentation-linux-glibc`
#    to support compatability with the https://github.com/open-telemetry/opentelemetry-operator 
#    project.
#  - Grant the necessary access to the files in the `/autoinstrumentation` directory.
#  - Following environment variables are injected to the application container to enable the auto-instrumentation.
#    CORECLR_ENABLE_PROFILING=1
#    CORECLR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}
#    CORECLR_PROFILER_PATH=%InstallationLocation%/linux-x64/OpenTelemetry.AutoInstrumentation.Native.so
#    DOTNET_ADDITIONAL_DEPS=%InstallationLocation%/AdditionalDeps
#    DOTNET_SHARED_STORE=%InstallationLocation%/store
#    DOTNET_STARTUP_HOOKS=%InstallationLocation%/net/OpenTelemetry.AutoInstrumentation.StartupHook.dll 
#    OTEL_DOTNET_AUTO_HOME=%InstallationLocation%
#  - For auto-instrumentation by container injection, the Linux command cp is
#    used and must be availabe in the image.
FROM busybox

ARG RELEASE_VER
ENV RELEASE_VER=$RELEASE_VER

WORKDIR /autoinstrumentation

ADD https://github.com/signalfx/splunk-otel-dotnet/releases/download/${RELEASE_VER}/splunk-opentelemetry-dotnet-linux-glibc.zip .
RUN unzip ./splunk-opentelemetry-dotnet-linux-glibc.zip
RUN ln -s ./splunk-opentelemetry-dotnet-linux-glibc ./opentelemetry-dotnet-instrumentation-linux-glibc

RUN chmod -R 644 .
