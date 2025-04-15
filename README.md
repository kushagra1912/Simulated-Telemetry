# Simulated-Telemetry

🚦 A cross-platform C# console application for simulating and visualizing racing telemetry data.  
Designed for real-time use cases such as racing cockpits, simulators, and telemetry training tools.

---

## 🧰 Features

- ✅ **Telemetry Modes**
  - `Simulated`: Generates internal mock data
  - `UDP`: Listens for incoming telemetry over UDP
  - `Serial`: Connects to physical telemetry devices (e.g., Arduino)

- ✅ **Live ASCII Dashboard**  
  Displays speed, RPM, gear, throttle, and brake in real time.

- ✅ **Input Commands**  
  Interact using:
  - `pause`, `resume`, `stop`
  - `speed=<value>` to override speed in simulation mode

- ✅ **Telemetry Logging**  
  - JSONL logs per session in `telemetry_logs/`
  - Ideal for analysis and replay

---

## 📁 Project Structure

```
SimTelemetry/
├── Program.cs           # Main telemetry processor (Simulated, UDP, Serial)
├── telemetry_logs/      # Automatically generated on runtime

UDPSender/
└── Program.cs           # Sends mock JSON telemetry to SimTelemetry
```

---

## 🚀 Getting Started

### 1. Requirements

- [.NET SDK 7+](https://dotnet.microsoft.com/download)
- macOS, Windows, or Linux

---

## ▶️ Run the SimTelemetry App

```bash
cd SimTelemetry
dotnet run
```

Choose one of the modes:
- `1` - Simulated (no external input)
- `2` - UDP (must run UDPSender separately)
- `3` - Serial (requires a serial device or emulator)

---

## 🌐 Run the UDP Sender

In a separate terminal:

```bash
cd UDPSender
dotnet run
```

This app sends mock JSON packets every second to `localhost:9000`.

Sample packet:
```json
{
  "Speed": 185,
  "RPM": 7200,
  "Gear": 4,
  "Throttle": 0.87,
  "Brake": 0.13,
  "Timestamp": "2025-04-15T10:30:00Z"
}
```

---

## 🧪 Simulate Serial Input (Optional)

Upload the following sketch to an Arduino:

```cpp
void setup() {
  Serial.begin(9600);
}
void loop() {
  Serial.println("Speed=183,RPM=7400,Gear=4,Throttle=0.88,Brake=0.10");
  delay(1000);
}
```

Connect via USB and choose option `3` in SimTelemetry.

---

## 🗃️ Logs

All telemetry is logged to:
```
telemetry_logs/session_YYYYMMDD_HHMMSS.jsonl
```

Each line is a JSON object for easy import into tools like Excel, Power BI, or Grafana.

---

## 🧠 Tech Stack

- C# .NET 7 (console apps)
- System.Text.Json
- UdpClient / SerialPort
- async/await for concurrency

---

## 📌 Future Ideas

- Add replay mode from JSON logs
- Create .NET MAUI GUI version
- Add WebSocket server for broadcasting telemetry