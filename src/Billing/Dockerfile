FROM mcr.microsoft.com/dotnet/aspnet:8.0

LABEL com.bitwarden.product="bitwarden"

USER app
ENV ASPNETCORE_URLS=http://+:5000
WORKDIR /app
EXPOSE 5000

COPY obj/build-output/publish .

ENTRYPOINT ["dotnet", "/app/Billing.dll"]
