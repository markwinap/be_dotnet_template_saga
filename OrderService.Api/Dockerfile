# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY OrderService.slnx .
COPY OrderService.Api/OrderService.Api.csproj OrderService.Api/
COPY OrderService.Application/OrderService.Application.csproj OrderService.Application/
COPY OrderService.Domain/OrderService.Domain.csproj OrderService.Domain/
COPY OrderService.Infrastructure/OrderService.Infrastructure.csproj OrderService.Infrastructure/
RUN dotnet restore OrderService.Api/OrderService.Api.csproj
COPY . .
RUN dotnet publish OrderService.Api/OrderService.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .
USER $APP_UID
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "OrderService.Api.dll"]
