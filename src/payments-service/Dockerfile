FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/payments-service/. ./payments-service
COPY src/shared/contracts ./shared/contracts
RUN dotnet restore ./payments-service/payments-service.csproj
RUN dotnet publish ./payments-service/payments-service.csproj -c Release -o /app/publish --no-restore
RUN ls -la /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN rm -f Properties/launchSettings.json
ENTRYPOINT ["dotnet", "payments-service.dll"] 