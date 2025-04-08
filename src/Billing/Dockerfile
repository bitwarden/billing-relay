FROM mcr.microsoft.com/dotnet/aspnet:9.0.3-azurelinux3.0-distroless

LABEL com.bitwarden.product="bitwarden"
COPY --from=ghcr.io/alexaka1/distroless-dotnet-healthchecks:1 / /healthchecks
HEALTHCHECK CMD ["/healthchecks/Distroless.HealthChecks", "--uri", "http://localhost:5000/alive"]

USER app
ENV ASPNETCORE_URLS http://+:5000
WORKDIR /app
EXPOSE 5000

COPY obj/build-output/publish .

ENTRYPOINT ["dotnet", "/app/Billing.dll"]
