#Stage 1: Build and Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

#Restore dependencies
COPY ["src/TicketManagement/TicketManagement.csproj", "src/TicketManagement/"]
RUN dotnet restore "src/TicketManagement/TicketManagement.csproj"

#Copy the remaining source code and build
COPY . .
WORKDIR "/src/src/TicketManagement"
RUN dotnet publish "TicketManagement.csproj" -c Release -o /app/publish /p:UseAppHost=false

#Stage 2: Final Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

#Standard port for .NET applications running in containers
EXPOSE 8080 
ENTRYPOINT ["dotnet", "TicketManagement.dll"]