FROM debian:bookworm-slim@sha256:734a72b3d6142a71993b4bc580ca01d8ca687f77a2c422d96a961d3376b179bc

# renovate: suite=bookworm depName=bash
ENV BASH_VERSION="5.2.15-2+b13"
# renovate: suite=bookworm depName=ca-certificates
ENV CA_CERTIFICATES_VERSION="20250419~deb12u2"
# renovate: suite=bookworm depName=clang
ENV CLANG_VERSION="1:14.0-55.7~deb12u1"
# renovate: suite=bookworm depName=cmake
ENV CMAKE_VERSION="3.25.1-1"
# renovate: suite=bookworm depName=curl
ENV CURL_VERSION="7.88.1-10+deb12u15"
# renovate: suite=bookworm depName=git
ENV GIT_VERSION="1:2.39.5-0+deb12u3"
# renovate: suite=bookworm depName=libgssapi-krb5-2
ENV LIBGSSAPI_KRB5_2_VERSION="1.20.1-2+deb12u5"
# renovate: suite=bookworm depName=libicu72
ENV LIBICU72_VERSION="72.1-3+deb12u1"
# renovate: suite=bookworm depName=libssl3
ENV LIBSSL3_VERSION="3.0.22-1~deb12u2"
# renovate: suite=bookworm depName=libstdc++6
ENV LIBSTDCXX6_VERSION="12.2.0-14+deb12u1"
# renovate: suite=bookworm depName=make
ENV MAKE_VERSION="4.3-4.1"
# renovate: suite=bookworm depName=zlib1g
ENV ZLIB1G_VERSION="1:1.2.13.dfsg-1"
# Update the GitHub CLI version and architecture-specific checksum together.
ENV GITHUB_CLI_VERSION="2.101.0"
ENV GITHUB_CLI_ARCH="arm64"
ENV GITHUB_CLI_SHA256="b57e8063f18862647c9d22727c32e9da1b963f8bf9db648fe123a6975695640f"

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        bash="${BASH_VERSION}" \
        ca-certificates="${CA_CERTIFICATES_VERSION}" \
        clang="${CLANG_VERSION}" \
        cmake="${CMAKE_VERSION}" \
        curl="${CURL_VERSION}" \
        git="${GIT_VERSION}" \
        libgssapi-krb5-2="${LIBGSSAPI_KRB5_2_VERSION}" \
        libicu72="${LIBICU72_VERSION}" \
        libssl3="${LIBSSL3_VERSION}" \
        libstdc++6="${LIBSTDCXX6_VERSION}" \
        make="${MAKE_VERSION}" \
        zlib1g="${ZLIB1G_VERSION}" && \
    rm -rf /var/lib/apt/lists/*

RUN curl -sSfL "https://github.com/cli/cli/releases/download/v${GITHUB_CLI_VERSION}/gh_${GITHUB_CLI_VERSION}_linux_${GITHUB_CLI_ARCH}.tar.gz" --output /tmp/gh.tar.gz \
    && echo "${GITHUB_CLI_SHA256}  /tmp/gh.tar.gz" | sha256sum -c \
    && tar -xzf /tmp/gh.tar.gz -C /tmp \
    && mv "/tmp/gh_${GITHUB_CLI_VERSION}_linux_${GITHUB_CLI_ARCH}/bin/gh" /usr/local/bin/gh \
    && rm -rf /tmp/gh.tar.gz "/tmp/gh_${GITHUB_CLI_VERSION}_linux_${GITHUB_CLI_ARCH}"

COPY ./scripts/dotnet-install.sh ./dotnet-install.sh

RUN chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh -v 11.0.100 --install-dir /usr/share/dotnet --no-path \
    && ./dotnet-install.sh -v 10.0.404 --install-dir /usr/share/dotnet --no-path --skip-non-versioned-files \
    && rm dotnet-install.sh

ENV DOTNET_ROOT=/usr/share/dotnet
ENV PATH="$DOTNET_ROOT:$PATH"

WORKDIR /project
