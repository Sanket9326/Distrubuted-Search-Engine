# syntax=docker/dockerfile:1

FROM node:22-alpine AS build
WORKDIR /src

COPY src/Services/WebUI/package.json src/Services/WebUI/package-lock.json ./
RUN npm ci

COPY src/Services/WebUI/ ./
RUN npm run build -- --configuration production

FROM nginx:alpine AS runtime
COPY --from=build /src/dist/web-ui/browser /usr/share/nginx/html
COPY src/Services/WebUI/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
