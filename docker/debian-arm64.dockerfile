FROM mcr.microsoft.com/dotnet/sdk:9.0.308-bookworm-slim@sha256:584cdb686f8ff7d2ffd636581b9bba8c9de38df1e751e9275438cddecdfbbcd2
# There is no official base image for .NET SDK 10+ on Debian, so install .NET10 via dotnet-install


RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

# Install older sdks using the install script as there are no arm64 SDK packages.
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "9d310fe9c949b9635f27c2d59c4c742101ebad52a16db5265ecc0fdb29ec7f45  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 10.0.100 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.416 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
