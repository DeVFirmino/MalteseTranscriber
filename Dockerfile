# Stage 1: Build .NET backend
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY MalteseTranscriber.sln ./
COPY src/MalteseTranscriber.Core/*.csproj src/MalteseTranscriber.Core/
COPY src/MalteseTranscriber.Infrastructure/*.csproj src/MalteseTranscriber.Infrastructure/
COPY src/MalteseTranscriber.API/*.csproj src/MalteseTranscriber.API/
RUN dotnet restore
COPY src/ src/
RUN dotnet publish src/MalteseTranscriber.API -c Release -o /app/publish

# Stage 2: Build React frontend
FROM node:20-alpine AS frontend
WORKDIR /frontend
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=frontend /frontend/dist wwwroot/
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "MalteseTranscriber.API.dll"]
