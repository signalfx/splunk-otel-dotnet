FROM mcr.microsoft.com/dotnet/sdk:9.0.316-bookworm-slim@sha256:ee0d20fbc3dfc60ae2d9d9115a9b750c18576a47f24073119dd09d93e072cb89
# There is no official base image for .NET SDK 10+ on Debian, so install .NET10 via dotnet-install


# renovate: suite=bookworm depName=cmake
ENV CMAKE_VERSION="3.25.1-1"
# renovate: suite=bookworm depName=clang
ENV CLANG_VERSION="1:14.0-55.7~deb12u1"
# renovate: suite=bookworm depName=make
ENV MAKE_VERSION="4.3-4.1"

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        cmake="${CMAKE_VERSION}" \
        clang="${CLANG_VERSION}" \
        make="${MAKE_VERSION}" && \
    rm -rf /var/lib/apt/lists/*

# Install older sdks using the install script as there are no arm64 SDK packages.
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "082f7685e156738a1b2e2ed8381a621870d4ce8e8c59278034556f05c186eb2e  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 10.0.302 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.423 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
