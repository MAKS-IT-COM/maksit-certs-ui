#!/bin/bash

# Variables
SERVICE_NAME="maks-it-agent"
SERVICE_PORT="5000"
SERVICE_FILE="/etc/systemd/system/$SERVICE_NAME.service"
INSTALL_DIR="/opt/$SERVICE_NAME"
DOTNET_EXEC="/usr/bin/dotnet"
EXEC_CMD="$DOTNET_EXEC $INSTALL_DIR/Agent.dll --urls \"http://*:$SERVICE_PORT\""
APPSETTINGS_FILE="appsettings.json"
NO_NEW_KEY_FLAG="--no-new-key"

# Update package index and install the Microsoft package repository
sudo rpm -Uvh https://packages.microsoft.com/config/centos/8/packages-microsoft-prod.rpm
sudo dnf install -y dotnet-sdk-8.0

# Check if the service exists and stop it if it does
if systemctl list-units --full -all | grep -Fq "$SERVICE_NAME.service"; then
    sudo systemctl stop $SERVICE_NAME.service
    sudo systemctl disable $SERVICE_NAME.service
    sudo rm -f $SERVICE_FILE
fi

# Clean up the old files if they exist
sudo rm -rf $INSTALL_DIR

# Create the application directory
sudo mkdir -p $INSTALL_DIR

# Update appsettings.json if --no-new-key flag is not provided
if [[ "$1" != "$NO_NEW_KEY_FLAG" ]]; then
    NEW_API_KEY=$(openssl rand -base64 32)
    jq --arg newApiKey "$NEW_API_KEY" '.Configuration.ApiKey = $newApiKey' $APPSETTINGS_FILE > tmp.$$.json && mv tmp.$$.json $APPSETTINGS_FILE
fi

# Build and publish the .NET application
sudo dotnet build --configuration Release
sudo dotnet publish -c Release -o $INSTALL_DIR

# Create the systemd service unit file
sudo bash -c "cat > $SERVICE_FILE <<EOL
[Unit]
Description=Maks-IT Agent
After=network.target

[Service]
WorkingDirectory=$INSTALL_DIR
ExecStart=$EXEC_CMD
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=dotnet-servicereloader
User=root
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
EOL"

# Reload systemd to recognize the new service, enable it to start on boot, and start the service now
sudo systemctl daemon-reload
sudo systemctl enable --now $SERVICE_NAME.service

# Create the firewall service rule
echo '<?xml version="1.0" encoding="utf-8"?>
<service>
    <short>Maks-IT Agent</short>
    <port protocol="tcp" port="'$SERVICE_PORT'"/>
</service>' > /etc/firewalld/services/maks-it-agent.xml

sleep 10

# Add the services to the firewall
firewall-cmd --permanent --add-service=maks-it-agent

# Reload the firewall
firewall-cmd --reload
