# To build one auto-instrumentation image for dotnet, please:
#  - Download your dotnet auto-instrumentation artifacts to the `/autoinstrumentation` directory. This is required as when instrumenting the pod,
#    one init container will be created to copy the files to your app's container.
#  - Grant the necessary access to the files in the `/autoinstrumentation` directory.
#  - Following environment variables are injected to the application container to enable the .NET auto-instrumentation.
#    CORECLR_ENABLE_PROFILING=1
#    CORECLR_PROFILER={918728DD-259F-4A6A-AC2B-B85E1B658318}
#    CORECLR_PROFILER_PATH=%InstallationLocation%/linux/OpenTelemetry.AutoInstrumentation.Native.so # for glibc based images
#    CORECLR_PROFILER_PATH=%InstallationLocation%/linux-musl/OpenTelemetry.AutoInstrumentation.Native.so # for musl based images
#    DOTNET_ADDITIONAL_DEPS=%InstallationLocation%/AdditionalDeps
#    DOTNET_SHARED_STORE=%InstallationLocation%/store
#    DOTNET_STARTUP_HOOKS=%InstallationLocation%/net/OpenTelemetry.AutoInstrumentation.StartupHook.dll
#    OTEL_DOTNET_AUTO_HOME=%InstallationLocation%
#    OTEL_DOTNET_AUTO_PLUGINS=Splunk.OpenTelemetry.AutoInstrumentation.Plugin, Splunk.OpenTelemetry.AutoInstrumentation
#  - For auto-instrumentation by container injection, the Linux command cp is
#    used and must be available in the image.
#  - This image is built for linux/amd64 and linux/arm64. The .NET Runtime Identifier (RID) specific
#    artifact for the target platform is downloaded and exposed under the architecture-agnostic
#    `linux`/`linux-musl` directories, with `linux-x64`/`linux-musl-x64` kept as symlinks for
#    backward compatibility with the CORECLR_PROFILER_PATH values used before multi-arch support
#    was added.

FROM busybox:1.38.0@sha256:a46257e06c82ad85242d0d38077d0a52d9896389ca60a1a59d3463db586e5fa4 AS downloader

ARG RELEASE_VER
ARG ARCHIVE_ARCH

WORKDIR /autoinstrumentation

ADD https://github.com/signalfx/splunk-otel-dotnet/releases/download/$RELEASE_VER/splunk-opentelemetry-dotnet-linux-glibc-$ARCHIVE_ARCH.zip .
ADD https://github.com/signalfx/splunk-otel-dotnet/releases/download/$RELEASE_VER/splunk-opentelemetry-dotnet-linux-musl-$ARCHIVE_ARCH.zip .

RUN unzip splunk-opentelemetry-dotnet-linux-glibc-$ARCHIVE_ARCH.zip &&\
    unzip splunk-opentelemetry-dotnet-linux-musl-$ARCHIVE_ARCH.zip "linux-musl-$ARCHIVE_ARCH/*" -d . &&\
    rm splunk-opentelemetry-dotnet-linux-glibc-$ARCHIVE_ARCH.zip splunk-opentelemetry-dotnet-linux-musl-$ARCHIVE_ARCH.zip &&\
    mv "linux-$ARCHIVE_ARCH" linux &&\
    mv "linux-musl-$ARCHIVE_ARCH" linux-musl &&\
    ln -s linux "linux-$ARCHIVE_ARCH" &&\
    ln -s linux-musl "linux-musl-$ARCHIVE_ARCH" &&\
    chmod -R go+r .

FROM busybox:1.38.0@sha256:a46257e06c82ad85242d0d38077d0a52d9896389ca60a1a59d3463db586e5fa4

LABEL org.opencontainers.image.source="https://github.com/signalfx/splunk-otel-dotnet"
LABEL org.opencontainers.image.description="Splunk Distribution of OpenTelemetry .NET"

COPY --from=downloader /autoinstrumentation /autoinstrumentation
