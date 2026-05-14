FROM node:22-alpine AS build
WORKDIR /app
COPY src/ui/package.json src/ui/package-lock.json ./
RUN npm ci
COPY src/ui/ ./
# Default empty: production bundle uses relative /api and /hubs (nginx proxies to rest-api).
ARG VITE_API_BASE_URL=
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY src/ui/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
