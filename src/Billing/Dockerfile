FROM mcr.microsoft.com/dotnet/aspnet:8.0

LABEL com.bitwarden.product="bitwarden"

RUN apt-get update -y \
    && apt-get install -y --no-install-recommends \
        curl=7.74.0* \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS http://+:5000
WORKDIR /app
EXPOSE 5000

COPY prepare-env.sh /
RUN chmod +x /prepare-env.sh && \
    /prepare-env.sh

# Enable healthcheck after implementing it in the service code
# HEALTHCHECK CMD curl -f http://localhost:5000/alive || exit 1

COPY obj/build-output/publish .

USER bitwarden

ENTRYPOINT ["dotnet", "/app/Billing.dll"]
