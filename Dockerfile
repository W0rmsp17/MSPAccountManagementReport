FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY MSPAccountManagementReport/MSPAccountManagementReport.csproj MSPAccountManagementReport/
RUN dotnet restore MSPAccountManagementReport/MSPAccountManagementReport.csproj

COPY . .
RUN dotnet publish MSPAccountManagementReport/MSPAccountManagementReport.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MSPAccountManagementReport.dll"]
