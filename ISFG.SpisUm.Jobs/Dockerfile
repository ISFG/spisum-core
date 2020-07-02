#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /jobs
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["ISFG.SpisUm.Jobs/ISFG.SpisUm.Jobs.csproj", "ISFG.SpisUm.Jobs/"]
RUN dotnet restore "ISFG.SpisUm.Jobs/ISFG.SpisUm.Jobs.csproj"
COPY . .
WORKDIR "/src/ISFG.SpisUm.Jobs"
RUN dotnet build "ISFG.SpisUm.Jobs.csproj" -c Release -o /jobs/build

FROM build AS publish
RUN dotnet publish "ISFG.SpisUm.Jobs.csproj" -c Release -o /jobs/publish

FROM base AS final
WORKDIR /jobs
COPY --from=publish /jobs/publish .
ENTRYPOINT ["dotnet", "ISFG.SpisUm.Jobs.dll"]