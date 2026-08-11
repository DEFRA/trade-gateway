# Base dotnet image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Add curl to template.
# CDP PLATFORM HEALTHCHECK REQUIREMENT
RUN apt update && \
    apt upgrade -y && \
    apt install curl -y && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Build stage image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

WORKDIR "/src"

COPY .config/dotnet-tools.json .config/dotnet-tools.json
COPY .csharpierrc .csharpierrc
COPY .csharpierignore .csharpierignore

RUN dotnet tool restore

COPY src/Api/Api.csproj src/Api/Api.csproj
COPY src/Api.Client/Api.Client.csproj src/Api.Client/Api.Client.csproj
COPY src/TracesNT/TracesNT.csproj src/TracesNT/TracesNT.csproj
COPY src/Api.Contract/Api.Contract.csproj src/Api.Contract/Api.Contract.csproj
COPY tests/Api.Tests/Api.Tests.csproj tests/Api.Tests/Api.Tests.csproj
COPY tests/TracesNT.Tests/TracesNT.Tests.csproj tests/TracesNT.Tests/TracesNT.Tests.csproj
COPY tests/Api.Client.Tests/Api.Client.Tests.csproj tests/Api.Client.Tests/Api.Client.Tests.csproj

COPY TradeGateway.sln TradeGateway.sln
COPY Directory.Build.props Directory.Build.props

COPY NuGet.config NuGet.config
ARG DEFRA_NUGET_PAT

RUN dotnet restore

COPY src/Api src/Api
COPY src/Api.Client src/Api.Client
COPY src/Api.Contract src/Api.Contract
COPY src/TracesNT src/TracesNT
COPY tests/Api.Tests tests/Api.Tests
COPY tests/TracesNT.Tests tests/TracesNT.Tests
COPY tests/Api.Client.Tests tests/Api.Client.Tests

RUN dotnet test --no-restore --filter "Category!=IntegrationTests"

FROM build AS publish
RUN dotnet publish src/Api -c Release -o /app/publish /p:UseAppHost=false

ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Final production image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8085
ENTRYPOINT ["dotnet", "Api.dll"]
