FROM mcr.microsoft.com/dotnet/sdk:8.0 AS tool-env

RUN dotnet tool install --tool-path /tools dotnet-trace \
    && dotnet tool install --tool-path /tools dotnet-counters \
    && dotnet tool install --tool-path /tools dotnet-dump \
    && dotnet tool install --tool-path /tools dotnet-gcdump

FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /tools
COPY --from=tool-env /tools .

LABEL com.bitwarden.product="bitwarden"

USER app
ENV ASPNETCORE_URLS=http://+:5000
WORKDIR /app
EXPOSE 5000

COPY obj/build-output/publish .

ENTRYPOINT ["dotnet", "/app/Billing.dll"]
