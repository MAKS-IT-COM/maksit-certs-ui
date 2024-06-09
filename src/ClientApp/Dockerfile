# Use the specific Node.js version 20.14.0 image as the base image for building the app
FROM node:20.14.0-alpine AS build

# Set the working directory inside the container
WORKDIR /app

# Copy the package.json and package-lock.json files to the working directory
COPY ClientApp/package*.json ./

# Install the project dependencies
RUN npm install

# Copy the rest of the application code to the working directory
COPY ClientApp .

# Build the Next.js application for production
RUN npm run build

# Use the same Node.js image for running the app
FROM node:20.14.0-alpine AS production

# Set the working directory inside the container
WORKDIR /app

# Copy the package.json and package-lock.json files to the working directory
COPY ClientApp/package*.json ./

# Install only production dependencies
RUN npm install --only=production

# Copy the built Next.js application from the build stage to the current directory
COPY --from=build /app ./

# Expose port 3000 to access the application
EXPOSE 3000

# Start the Next.js server
CMD ["npm", "start"]
