FROM node:20.14.0-alpine

# Ambiente di sviluppo
ENV NODE_ENV=development
ENV LETSENCRYPT_SERVER=http://localhost:8080
ENV CHOKIDAR_USEPOLLING=true
ENV WATCHPACK_POLLING=true

# Lavora come utente normale
USER node
WORKDIR /app

EXPOSE 3000

# Comando di avvio
CMD ["sh", "-c", "npm install && npm run dev"]
