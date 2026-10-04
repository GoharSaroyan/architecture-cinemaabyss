FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

WORKDIR /app

# Copy project file
COPY *.csproj ./

# Download all dependencies
RUN dotnet restore

# Copy the source code (only sources, without local bin/ and obj/)
COPY Program.cs appsettings.json ./
COPY Configuration/ ./Configuration/
COPY Endpoints/ ./Endpoints/
COPY Kafka/ ./Kafka/
COPY Models/ ./Models/

# Build the application
RUN dotnet publish -c Release --no-restore -o /app/out

# Debian-based image: the Kafka client (librdkafka) native library targets glibc
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Copy the build output from builder
COPY --from=builder /app/out .

# Expose the application port
EXPOSE 8082

# Command to run the application
CMD ["dotnet", "events-service.dll"]
