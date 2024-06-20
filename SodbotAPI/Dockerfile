FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app

COPY ["MyWebApiProject/MyWebApiProject.csproj", "MyWebApiProject/"]
RUN dotnet restore "MyWebApiProject/MyWebApiProject.csproj"

COPY . .
WORKDIR "/src/MyWebApiProject"
RUN dotnet publish "MyWebApiProject.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5278

ENTRYPOINT ["dotnet", "MyWebApiProject.dll"]