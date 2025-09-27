FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

RUN apt-get update && apt-get install nano

WORKDIR /src

COPY ["SodbotAPI/SodbotAPI.csproj", "SodbotAPI/"]
RUN dotnet restore "SodbotAPI/SodbotAPI.csproj"

COPY . .
WORKDIR "/src/SodbotAPI"
RUN dotnet publish "SodbotAPI.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "SodbotAPI.dll"]