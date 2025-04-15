using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

enum TelemetryMode {
    Simulated,
    UDP,
    Serial
}

class TelemetryData {
    public int Speed { get; set; }
    public int RPM { get; set; }
    public int Gear { get; set; }
    public double Throttle { get; set; }
    public double Brake { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

class Program {
    static Random rand = new Random();
    static bool paused = false;
    static bool running = true;
    static int? injectedSpeed = null;
    static readonly string logDir = "telemetry_logs";
    static readonly string sessionLogFile = Path.Combine(logDir, $"session_{DateTime.Now:yyyyMMdd_HHmmss}.jsonl");

    static async Task Main(string[] args) {
        Console.WriteLine("Choose telemetry mode:");
        Console.WriteLine("1. Simulated");
        Console.WriteLine("2. UDP");
        Console.WriteLine("3. Serial");
        Console.Write("Enter option (1/2/3): ");
        string? input = Console.ReadLine()?.Trim();
        TelemetryMode mode = input switch {
            "2" => TelemetryMode.UDP,
            "3" => TelemetryMode.Serial,
            _ => TelemetryMode.Simulated
        };

        var cts = new CancellationTokenSource();
        List<Task> tasks = new();

        if (mode == TelemetryMode.Simulated)
            tasks.Add(Task.Run(() => SimulatedLoop(cts.Token, mode)));

        if (mode == TelemetryMode.UDP)
            tasks.Add(Task.Run(() => UdpListenerLoop(cts.Token)));

        if (mode == TelemetryMode.Serial)
            StartSerialListener();

        tasks.Add(Task.Run(() => HandleUserInput(cts)));

        await Task.WhenAll(tasks);
    }

    static async Task SimulatedLoop(CancellationToken token, TelemetryMode mode) {
        while (!token.IsCancellationRequested && running) {
            if (paused) {
                await Task.Delay(500);
                continue;
            }

            var data = new TelemetryData {
                Speed = injectedSpeed ?? rand.Next(100, 250),
                RPM = rand.Next(5000, 9000),
                Gear = rand.Next(1, 6),
                Throttle = Math.Round(rand.NextDouble(), 2),
                Brake = Math.Round(rand.NextDouble() * 0.5, 2),
                Timestamp = DateTime.UtcNow
            };

            RenderDashboard(data, mode);
            await LogTelemetryAsync(data);
            await Task.Delay(1000);
        }
    }

    static async Task UdpListenerLoop(CancellationToken token) {
        using UdpClient udp = new UdpClient(9000);
        udp.Client.ReceiveTimeout = 1000;

        while (!token.IsCancellationRequested) {
            try {
                var result = await udp.ReceiveAsync();
                string message = Encoding.UTF8.GetString(result.Buffer);

                try {
                    var data = JsonSerializer.Deserialize<TelemetryData>(message);
                    if (data != null) {
                        RenderDashboard(data, TelemetryMode.UDP);
                        await LogTelemetryAsync(data);
                    }
                } catch (JsonException) {
                    Console.WriteLine($"[Invalid JSON] {message}");
                }
            } catch (SocketException) { }
        }
    }

    static void StartSerialListener() {
        string[] ports = SerialPort.GetPortNames();
        if (ports.Length == 0) {
            Console.WriteLine("⚠ No serial ports detected.");
            return;
        }

        var port = ports[0];
        SerialPort serial = new SerialPort(port, 9600);
        serial.DataReceived += async (s, e) => {
            try {
                var line = serial.ReadLine();
                var data = ParseSerial(line);
                if (data != null) {
                    RenderDashboard(data, TelemetryMode.Serial);
                    await LogTelemetryAsync(data);
                }
            } catch { }
        };

        try { serial.Open(); }
        catch (Exception ex) { Console.WriteLine($"❌ Failed to open {port}: {ex.Message}"); }
    }

    static TelemetryData? ParseSerial(string line) {
        try {
            var parts = line.Split(',');
            var dict = parts.Select(p => p.Split('=')).ToDictionary(p => p[0], p => p[1]);
            return new TelemetryData {
                Speed = int.Parse(dict["Speed"]),
                RPM = int.Parse(dict["RPM"]),
                Gear = int.Parse(dict["Gear"]),
                Throttle = double.Parse(dict.GetValueOrDefault("Throttle", "0")),
                Brake = double.Parse(dict.GetValueOrDefault("Brake", "0")),
                Timestamp = DateTime.UtcNow
            };
        } catch { return null; }
    }

    static void RenderDashboard(TelemetryData data, TelemetryMode mode) {
        Console.Clear();
        Console.WriteLine($"[Mode: {mode}] - {data.Timestamp.ToLongTimeString()}");
        Console.WriteLine($"+----------------------------+");
        Console.WriteLine($"| Speed: {data.Speed} km/h");
        Console.WriteLine($"| RPM:   {data.RPM}");
        Console.WriteLine($"| Gear:  {data.Gear}");
        Console.WriteLine($"+----------------------------+");

        Console.WriteLine($"Throttle: {new string('▮', (int)(data.Throttle * 20)).PadRight(20)} {(data.Throttle * 100):0}%");
        Console.WriteLine($"Brake:    {new string('▮', (int)(data.Brake * 20)).PadRight(20)} {(data.Brake * 100):0}%");
        Console.WriteLine();
        Console.WriteLine("Commands: pause | resume | stop | speed=123");
    }

    static async Task LogTelemetryAsync(TelemetryData data) {
        try {
            Directory.CreateDirectory(logDir);
            string json = JsonSerializer.Serialize(data);
            await File.AppendAllTextAsync(sessionLogFile, json + Environment.NewLine);
        } catch (Exception ex) {
            Console.WriteLine($"[Logging Error] {ex.Message}");
        }
    }

    static void HandleUserInput(CancellationTokenSource cts) {
        while (running) {
            var cmd = Console.ReadLine()?.ToLower()?.Trim();
            if (cmd == "pause") paused = true;
            else if (cmd == "resume") paused = false;
            else if (cmd == "stop") {
                running = false;
                cts.Cancel();
            } else if (cmd?.StartsWith("speed=") == true) {
                var parts = cmd.Split('=');
                if (parts.Length == 2 && int.TryParse(parts[1], out int val)) {
                    injectedSpeed = val;
                }
            } else {
                Console.WriteLine("[Invalid Command]");
            }
        }
    }
}
