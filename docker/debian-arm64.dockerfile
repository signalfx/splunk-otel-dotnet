FROM mcr.microsoft.com/dotnet/sdk:9.0.313-bookworm-slim@sha256:f9ddb8a31ae90f4b38d18355d82f03d76dcdd2a57d7235a2ffdf008fab11a862
# There is no official base image for .NET SDK 10+ on Debian, so install .NET10 via dotnet-install


RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

# Install older sdks using the install script as there are no arm64 SDK packages.
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "102a6849303713f15462bb28eb10593bf874bbeec17122e0522f10a3b57ce442  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 10.0.203 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.420 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
