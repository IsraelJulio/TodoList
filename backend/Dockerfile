FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/TodoList.API/TodoList.API.csproj", "src/TodoList.API/"]
COPY ["src/TodoList.Application/TodoList.Application.csproj", "src/TodoList.Application/"]
COPY ["src/TodoList.Domain/TodoList.Domain.csproj", "src/TodoList.Domain/"]
COPY ["src/TodoList.Infrastructure/TodoList.Infrastructure.csproj", "src/TodoList.Infrastructure/"]
RUN dotnet restore "src/TodoList.API/TodoList.API.csproj"
COPY . .
WORKDIR "/src/src/TodoList.API"
RUN dotnet publish "TodoList.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TodoList.API.dll"]
