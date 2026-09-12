# 1. AŞAMA: Angular'ı derle (Node imajı)
FROM node:22 AS build
WORKDIR /app

# Önce bağımlılıkları kur (katman önbelleği için)
COPY package*.json ./
RUN npm install

# Kaynağı kopyala ve production build al → dist/user-ui/browser
COPY . ./
RUN npm run build

# 2. AŞAMA: Nginx ile statik dosyaları servis et
FROM nginx:alpine AS runtime
COPY --from=build /app/dist/user-ui/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
