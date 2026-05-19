FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY AiAgents.MathTutor.sln ./
COPY AiAgents.Core/AiAgents.Core.csproj AiAgents.Core/
COPY AiAgents.MathTutorAgent/AiAgents.MathTutorAgent.csproj AiAgents.MathTutorAgent/
COPY AiAgents.MathTutorAgent.Web/AiAgents.MathTutorAgent.Web.csproj AiAgents.MathTutorAgent.Web/

RUN dotnet restore AiAgents.MathTutor.sln

COPY . .

RUN dotnet publish AiAgents.MathTutorAgent.Web/AiAgents.MathTutorAgent.Web.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

RUN mkdir -p /app/logs

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "AiAgents.MathTutorAgent.Web.dll"]
