#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ConsensusAlgorithm.WebAPI/ConsensusAlgorithm.WebAPI.csproj", "ConsensusAlgorithm.WebAPI/"]
RUN dotnet restore "ConsensusAlgorithm.WebAPI/ConsensusAlgorithm.WebAPI.csproj"
COPY . .
WORKDIR "/src/ConsensusAlgorithm.WebAPI"
RUN dotnet build "ConsensusAlgorithm.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ConsensusAlgorithm.WebAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsensusAlgorithm.WebAPI.dll"]