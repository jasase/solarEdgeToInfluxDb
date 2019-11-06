#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build
WORKDIR /src
#COPY ["SolarEdgeToInfluxDb/SolarEdgeToInfluxDb.csproj", "SolarEdgeToInfluxDb/"]
COPY . .
RUN dotnet restore "SolarEdgeToInfluxDb/SolarEdgeToInfluxDb.csproj"
WORKDIR "/src/SolarEdgeToInfluxDb"
RUN dotnet build "SolarEdgeToInfluxDb.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SolarEdgeToInfluxDb.csproj" -c Release -o /app/publish

FROM base AS final
RUN mkdir /logs
RUN mkdir /configuration
VOLUME /logs /configuration
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SolarEdgeToInfluxDb.dll"]