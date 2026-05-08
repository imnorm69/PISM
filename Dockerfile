# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files for layer-cached restore
COPY PISM.slnx .
COPY PISM.Core/PISM.Core.csproj PISM.Core/
COPY PISM.Data/PISM.Data.csproj PISM.Data/
COPY PISM.Web/PISM.Web.csproj PISM.Web/
COPY PISM.Worker/PISM.Worker.csproj PISM.Worker/
RUN dotnet restore

# Copy remaining source and publish
COPY . .
RUN dotnet publish PISM.Web/PISM.Web.csproj -c Release -o /app/publish --no-restore

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PISM.Web.dll"]
