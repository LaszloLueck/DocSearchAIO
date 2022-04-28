FROM laszlo/containerruntimeglobal-build:latest AS build-env
WORKDIR /app
COPY ./DocSearchAIO ./

RUN dotnet --version
RUN dotnet restore
RUN dotnet clean

RUN dotnet build --no-restore
RUN dotnet publish --no-restore -c Release -o out

FROM laszlo/containerruntimeglobal-runtime:latest
WORKDIR /app
COPY --from=build-env /app/out .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

RUN dotnet --list-runtimes
ENTRYPOINT ["dotnet", "DocSearchAIO.dll"]
