FROM mcr.microsoft.com/dotnet/sdk:9.0.318-bookworm-slim@sha256:01fabc4758d1d74e39eda700c8463dae6241a61481f973683692ddcb59a5eeb7
# There is no official base image for .NET SDK 10+ on Debian, so install .NET10 via dotnet-install


# renovate: suite=bookworm depName=cmake
ENV CMAKE_VERSION="3.25.1-1"
# renovate: suite=bookworm depName=clang
ENV CLANG_VERSION="1:14.0-55.7~deb12u1"
# renovate: suite=bookworm depName=make
ENV MAKE_VERSION="4.3-4.1"
# Update the GitHub CLI version and architecture-specific checksum together.
ENV GITHUB_CLI_VERSION="2.101.0"
ENV GITHUB_CLI_ARCH="arm64"
ENV GITHUB_CLI_SHA256="b57e8063f18862647c9d22727c32e9da1b963f8bf9db648fe123a6975695640f"

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        cmake="${CMAKE_VERSION}" \
        clang="${CLANG_VERSION}" \
        make="${MAKE_VERSION}" && \
    rm -rf /var/lib/apt/lists/*

RUN curl -sSfL "https://github.com/cli/cli/releases/download/v${GITHUB_CLI_VERSION}/gh_${GITHUB_CLI_VERSION}_linux_${GITHUB_CLI_ARCH}.tar.gz" --output /tmp/gh.tar.gz \
    && echo "${GITHUB_CLI_SHA256}  /tmp/gh.tar.gz" | sha256sum -c \
    && tar -xzf /tmp/gh.tar.gz -C /tmp \
    && mv "/tmp/gh_${GITHUB_CLI_VERSION}_linux_${GITHUB_CLI_ARCH}/bin/gh" /usr/local/bin/gh \
    && rm -rf /tmp/gh.tar.gz "/tmp/gh_${GITHUB_CLI_VERSION}_linux_${GITHUB_CLI_ARCH}"

# Install older sdks using the install script as there are no arm64 SDK packages.
RUN curl -sSL https://dot.net/v1/dotnet-install.sh --output dotnet-install.sh \
    && echo "SHA256: $(sha256sum dotnet-install.sh)" \
    && echo "082f7685e156738a1b2e2ed8381a621870d4ce8e8c59278034556f05c186eb2e  dotnet-install.sh" | sha256sum -c \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 10.0.302 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 8.0.423 --install-dir /usr/share/dotnet --no-path \
    && rm dotnet-install.sh

WORKDIR /project
