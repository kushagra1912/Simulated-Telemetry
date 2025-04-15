#!/bin/bash

echo "Starting SimTelemetry in UDP mode..."
osascript -e 'tell application "Terminal" to do script "cd $(pwd)/SimulatedTelemetry && dotnet run"'

sleep 2

echo "Starting UDP Sender..."
osascript -e 'tell application "Terminal" to do script "cd $(pwd)/SimulatedTelemetry/UDPSender && dotnet run"'
