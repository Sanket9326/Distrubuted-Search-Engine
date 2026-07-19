# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/BuildingBlocks/SharedKernel/SharedKernel.csproj src/BuildingBlocks/SharedKernel/
COPY src/BuildingBlocks/Contracts/Contracts.csproj src/BuildingBlocks/Contracts/
COPY src/BuildingBlocks/Infrastructure/Infrastructure.csproj src/BuildingBlocks/Infrastructure/
COPY src/BuildingBlocks/Common/Common.csproj src/BuildingBlocks/Common/
COPY src/Services/ReliabilityService/ReliabilityService.csproj src/Services/ReliabilityService/
RUN dotnet restore src/Services/ReliabilityService/ReliabilityService.csproj

COPY src/BuildingBlocks/ src/BuildingBlocks/
COPY src/Services/ReliabilityService/ src/Services/ReliabilityService/
RUN dotnet publish src/Services/ReliabilityService/ReliabilityService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ReliabilityService.dll"]
