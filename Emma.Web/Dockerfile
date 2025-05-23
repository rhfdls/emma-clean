# Build stage
FROM node:20 AS build
WORKDIR /app

# Install app dependencies
COPY package*.json ./
RUN npm install

# Copy app source
COPY . .

# Set environment variables
ENV NODE_OPTIONS=--openssl-legacy-provider

# Build the app
ARG REACT_APP_API_URL
ENV REACT_APP_API_URL=${REACT_APP_API_URL:-http://localhost:5262}

# Create .env file with the environment variable
RUN echo "REACT_APP_API_URL=$REACT_APP_API_URL" > .env

# Build the application
RUN npm run build

# Production stage
FROM nginx:alpine

# Copy the build files
COPY --from=build /app/build /usr/share/nginx/html

# Copy nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose port 80
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
