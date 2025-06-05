FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/DevOpsAssistant/DevOpsAssistant.sln ./
COPY src/DevOpsAssistant ./DevOpsAssistant
RUN dotnet publish DevOpsAssistant/DevOpsAssistant.csproj -c Release -o /app/publish

FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 5678
CMD ["nginx", "-g", "daemon off;"]
