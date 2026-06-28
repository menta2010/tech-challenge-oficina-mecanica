# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.sln ./
COPY src/Oficina.Domain/*.csproj src/Oficina.Domain/
COPY src/Oficina.Application/*.csproj src/Oficina.Application/
COPY src/Oficina.Infrastructure/*.csproj src/Oficina.Infrastructure/
COPY src/Oficina.API/*.csproj src/Oficina.API/
COPY tests/Oficina.UnitTests/*.csproj tests/Oficina.UnitTests/
COPY tests/Oficina.IntegrationTests/*.csproj tests/Oficina.IntegrationTests/
RUN dotnet restore src/Oficina.API/Oficina.API.csproj

COPY . .
RUN dotnet publish src/Oficina.API/Oficina.API.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- runtime (chiseled: imagem minima, sem shell/SO completo) ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS final
WORKDIR /app
COPY --from=build /app/publish .

# Usuario nao-root explicito (hardening - resolve o hotspot do SonarQube)
USER $APP_UID

ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Oficina.API.dll"]
