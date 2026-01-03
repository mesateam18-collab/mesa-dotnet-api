#!/bin/bash

# Script to start the .NET API server
# Automatically kills any existing process on port 5254

PORT=5254

echo "🔍 Checking for processes on port $PORT..."

# Kill any existing process on port 5254
PID=$(lsof -ti:$PORT)
if [ ! -z "$PID" ]; then
    echo "⚠️  Found existing process (PID: $PID) on port $PORT"
    echo "🛑 Stopping existing process..."
    kill -9 $PID 2>/dev/null
    sleep 1
    echo "✅ Port $PORT is now free"
else
    echo "✅ Port $PORT is available"
fi

echo ""
echo "🚀 Starting .NET API server..."
echo ""

# Start the server
dotnet run --launch-profile http

