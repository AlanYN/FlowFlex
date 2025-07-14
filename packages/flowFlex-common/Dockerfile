FROM node:20-alpine AS build

ARG Registry
RUN [ ! -n "$Registry" ] || npm config set registry ${Registry}
RUN npm config get registry
ENV NODE_OPTIONS=--max-old-space-size=16384

WORKDIR /app
COPY . ./

RUN npm install -g pnpm \
  && pnpm install

ARG BUILD_CMD=build:development
RUN pnpm $BUILD_CMD

FROM nginx:latest AS runtime
COPY --from=build /app/dist /usr/share/nginx/html/dist
COPY --from=build /app/nginx.conf /etc/nginx/nginx.conf 
