#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Homepage/Homepage.csproj", "External/QuickSC/QuickSC.Core/QuickSC.Core.csproj", "External/QuickSC/QuickSC.Infra/QuickSC.Infra.csproj", "External/QuickSC/QuickSC.Syntax/QuickSC.Syntax.csproj", "."]
RUN dotnet restore "Homepage/Homepage.csproj"
COPY [".", "."]
WORKDIR "/src/Homepage"
RUN dotnet publish "Homepage.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Homepage.dll"]