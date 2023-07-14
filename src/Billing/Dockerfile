FROM mcr.microsoft.com/dotnet/aspnet:7.0

LABEL com.bitwarden.product="bitwarden"

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        curl=7.74.0-1.3+deb11u7 \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS http://+:5000
WORKDIR /app
EXPOSE 5000
COPY prepare-env.sh /
RUN chmod +x /prepare-env.sh
RUN /prepare-env.sh

# Needs healthcheck /alive endpoint in the app
# HEALTHCHECK CMD curl -f http://localhost:5000/alive || exit 1

COPY obj/build-output/publish .

USER bitwarden

ENTRYPOINT ["dotnet", "/app/Billing.dll"]
