FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS builder

WORKDIR /app

# Copy project file
COPY *.csproj ./

# Download all dependencies
RUN dotnet restore

# Copy the source code (only sources, without local bin/ and obj/)
COPY Program.cs ./

# Build the application
RUN dotnet publish -c Release --no-restore -o /app/out

# Use a smaller image for the final application
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

WORKDIR /app

# Copy the build output from builder
COPY --from=builder /app/out .

# Expose the application port
EXPOSE 8081

# Command to run the application
CMD ["dotnet", "movies-service.dll"]
