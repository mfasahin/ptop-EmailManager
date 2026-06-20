# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["EmailManager.Domain/EmailManager.Domain.csproj", "EmailManager.Domain/"]
COPY ["EmailManager.Application/EmailManager.Application.csproj", "EmailManager.Application/"]
COPY ["EmailManager.Infrastructure/EmailManager.Infrastructure.csproj", "EmailManager.Infrastructure/"]
COPY ["EmailManager.Web/EmailManager.Web.csproj", "EmailManager.Web/"]

RUN dotnet restore "EmailManager.Web/EmailManager.Web.csproj"

# Copy all source files and compile
COPY . .
WORKDIR "/src/EmailManager.Web"
RUN dotnet build "EmailManager.Web.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "EmailManager.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directory for SQLite persistent database
RUN mkdir -p /app/data && chown -R 1000:1000 /app/data

# ASP.NET Core environment and listening port configuration
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/emailmanager.db"
EXPOSE 8080

ENTRYPOINT ["dotnet", "EmailManager.Web.dll"]
