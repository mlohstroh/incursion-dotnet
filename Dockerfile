FROM microsoft/dotnet:2.2-sdk
WORKDIR /app

ENV REDIS_URL_PORT="localhost:6379"

# Copy everything to the work dir
COPY . .
RUN dotnet restore

RUN dotnet publish -o exe
WORKDIR /app/exe

CMD ["dotnet", "jabber.dll"]