FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /src

COPY src/Benchy/Benchy.csproj .
RUN dotnet restore Benchy.csproj
COPY src/Benchy/ /Benchy/

WORKDIR /src

RUN dotnet publish --no-restore -c Release -o /app/publish Benchy.csproj

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime

WORKDIR /app

COPY --from=build /app/publish .
COPY src/Benchy/entrypoint.sh .

EXPOSE 5000

RUN chmod +x ./entrypoint.sh

CMD /bin/bash ./entrypoint.sh
