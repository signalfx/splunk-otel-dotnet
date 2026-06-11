FROM mcr.microsoft.com/dotnet/sdk:9.0.315-bookworm-slim@sha256:06da3f01ec505b4f365497e1952c7ffcb748e536ac60da463ae6766814cfd9c6
# There is no official base image for .NET SDK 10+ on Debian, so install .NET10 via dotnet-install


RUN apt-get update && \
    apt-get install -y \
        cmake \
        clang \
        make

# Install older sdks using the install script as there are no arm64 SDK packages.
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "082f7685e156738a1b2e2ed8381a621870d4ce8e8c59278034556f05c186eb2e  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 10.0.203 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.420 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
