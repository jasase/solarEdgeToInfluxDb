#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build
WORKDIR /src
COPY ["SolarEdgeToInfluxDb/SolarEdgeToInfluxDb.csproj", "SolarEdgeToInfluxDb/"]
RUN dotnet restore "SolarEdgeToInfluxDb/SolarEdgeToInfluxDb.csproj"
COPY . .
WORKDIR "/src/SolarEdgeToInfluxDb"
RUN dotnet build "SolarEdgeToInfluxDb.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SolarEdgeToInfluxDb.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SolarEdgeToInfluxDb.dll"]