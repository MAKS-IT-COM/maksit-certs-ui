#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MaksIT.CertsUI.Engine/MaksIT.CertsUI.Engine.csproj", "MaksIT.CertsUI.Engine/"]
COPY ["MaksIT.CertsUI/MaksIT.CertsUI.csproj", "MaksIT.CertsUI/"]
RUN dotnet restore "./MaksIT.CertsUI/MaksIT.CertsUI.csproj"
COPY . .
WORKDIR "/src/MaksIT.CertsUI"
RUN dotnet build "./MaksIT.CertsUI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MaksIT.CertsUI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
USER root
RUN apt-get update \
  && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
  && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .
USER app
ENTRYPOINT ["dotnet", "MaksIT.CertsUI.dll"]