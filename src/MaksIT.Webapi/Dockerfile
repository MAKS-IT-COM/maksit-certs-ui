#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MaksIT.Models/MaksIT.Models.csproj", "MaksIT.Models/"]
COPY ["LetsEncrypt/LetsEncrypt.csproj", "LetsEncrypt/"]
COPY ["MaksIT.Webapi/MaksIT.Webapi.csproj", "MaksIT.Webapi/"]
RUN dotnet restore "./MaksIT.Webapi/MaksIT.Webapi.csproj"
COPY . .
WORKDIR "/src/MaksIT.Webapi"
RUN dotnet build "./MaksIT.Webapi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MaksIT.Webapi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MaksIT.Webapi.dll"]