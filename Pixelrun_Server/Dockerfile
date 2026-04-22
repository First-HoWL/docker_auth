FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 443

COPY *.slnx .
COPY Pixelrun_Server/*.csproj ./Pixelrun_Server/
RUN dotnet restore Pixelrun_Server/Pixelrun_Server.csproj

COPY Pixelrun_Server/. ./Pixelrun_Server/
WORKDIR /source/Pixelrun_Server
RUN dotnet publish -c release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app ./

ENTRYPOINT ["dotnet", "Pixelrun_Server.dll"]