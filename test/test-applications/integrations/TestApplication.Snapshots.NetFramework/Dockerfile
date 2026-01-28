# escape=`

FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2022 AS base
ARG configuration=Debug
WORKDIR /opentelemetry
COPY bin/tracer.zip .
COPY bin/Splunk.OTel.DotNet.psm1 .
ENV SPLUNK_OTEL_DOTNET_AUTO_INSTALL_DIR=C:\opentelemetry
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]
RUN Import-Module .\Splunk.OTel.DotNet.psm1 -Verbose; `
    Install-OpenTelemetryCore -LocalPath .\tracer.zip; `
    Register-OpenTelemetryForIIS; `
    Import-Module WebAdministration; `
    Set-ItemProperty -Path 'IIS:\AppPools\DefaultAppPool' -Name 'enable32BitAppOnWin64' -Value $false;
ENV OTEL_DOTNET_AUTO_LOG_DIRECTORY=C:\inetpub\wwwroot\logs `
    OTEL_LOG_LEVEL=debug
WORKDIR /inetpub/wwwroot
COPY bin/${configuration}/app.publish .
