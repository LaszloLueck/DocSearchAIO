FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app
COPY ./DocSearchAIO ./

RUN dotnet --version

RUN dotnet clean
RUN dotnet nuget locals all --clear
RUN dotnet restore

RUN dotnet build --configuration Release
RUN dotnet publish -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
RUN dotnet --list-runtimes
ENTRYPOINT ["dotnet", "DocSearchAIO.dll"]
