# STAGE 1: Build React Production Bundle
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files để cache dependencies
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

# Copy source code frontend
COPY frontend/ ./

# Build bundle production (tsc & vite build)
ENV VITE_API_BASE_URL=/api/v1
RUN npm run build

# STAGE 2: Nginx Alpine Serving Static Assets
FROM nginx:1.27-alpine AS final

# Xóa trang mặc định của Nginx
RUN rm -rf /usr/share/nginx/html/*

# Copy build artifacts từ Stage 1
COPY --from=build /app/dist /usr/share/nginx/html

# Copy cấu hình Nginx SPA
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
