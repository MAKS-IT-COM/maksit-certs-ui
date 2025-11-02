FROM node:24

# Ambiente di sviluppo
ENV NODE_ENV=development

# Lavora come utente normale
USER node
WORKDIR /app

EXPOSE 5173

# Comando di avvio
CMD ["sh", "-c", "npm install && npm run dev"]
