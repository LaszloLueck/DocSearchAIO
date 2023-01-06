FROM registry.gretzki.ddns.net:5080/containerruntimeglobal/containerruntimeglobal_build-full_x64:22.04_7.0.1 AS build-env
WORKDIR /app
COPY ./DocSearchAIO ./

RUN dotnet --info \
&& dotnet restore \
&& dotnet clean \
&& dotnet build --no-restore \
&& dotnet publish --no-restore -c Release -o out

FROM registry.gretzki.ddns.net:5080/containerruntimeglobal/containerruntimeglobal_runtime-full_x64:22.04_7.0.1
WORKDIR /app
COPY --from=build-env /app/out .
EXPOSE 5000
EXPOSE 5002
ENV ASPNETCORE_URLS=http://+:5000;https://+:5002
ENTRYPOINT ["dotnet", "DocSearchAIO.dll"]
