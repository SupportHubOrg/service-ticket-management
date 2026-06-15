#Stage 1: Base runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
#.NET 8 defaults to port 8080 for non-root users
EXPOSE 8080
WORKDIR /app

#Stage 2: Build environment
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

#Restore dependencies as a distinct layer to leverage Docker cache
COPY ["TicketManagement.csproj", "."]
RUN dotnet restore "./TicketManagement.csproj"

#Copy the remaining source code and build
COPY . .
RUN dotnet build "./TicketManagement.csproj" -c $BUILD_CONFIGURATION -o /app/build

#Stage 3: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TicketManagement.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

#Stage 4: Final output image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

#The appsettings.json defines Serilog writing to "Logs/log-.txt". 
#.NET 8 runs as a restricted non-root user ($APP_UID) by default.
#We must create this directory and assign ownership to prevent runtime access denial.
USER root
RUN mkdir -p /app/Logs && chown -R $APP_UID:$APP_UID /app/Logs

#Switch back to the restricted user for security
USER $APP_UID

ENTRYPOINT ["dotnet", "TicketManagement.dll"]