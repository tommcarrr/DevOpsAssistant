FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy project files first to leverage Docker layer caching
COPY src/DevOpsAssistant/DevOpsAssistant/DevOpsAssistant.csproj ./DevOpsAssistant/
COPY src/DevOpsAssistant/PromptGenerator/PromptGenerator.csproj ./PromptGenerator/
RUN dotnet restore DevOpsAssistant/DevOpsAssistant.csproj

# copy the remaining source and publish the app
COPY src/DevOpsAssistant/ .
RUN dotnet publish DevOpsAssistant/DevOpsAssistant.csproj -c Release -o /app/publish --no-restore

FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 5678
CMD ["nginx", "-g", "daemon off;"]
