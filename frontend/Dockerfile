# Build stage
FROM node:24-alpine AS build
WORKDIR /app
COPY . .
RUN npm ci
RUN npm run build

# Deploy stage
FROM nginx:alpine AS deploy
COPY --from=build /app/dist/spa /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]