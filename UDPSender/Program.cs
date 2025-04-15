using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class TelemetryData {
    public int Speed { get; set; }
    public int RPM { get; set; }
    public int Gear { get; set; }
    public double Throttle { get; set; }
    public double Brake { get; set; }
    public DateTime Timestamp { get; set; }
}

class Program {
    static async Task Main() {
        using UdpClient udpClient = new UdpClient();
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 9000);
        Random rand = new Random();

        Console.WriteLine("Sending UDP telemetry to 127.0.0.1:9000");

        while (true) {
            var telemetry = new TelemetryData {
                Speed = rand.Next(100, 300),
                RPM = rand.Next(4000, 9000),
                Gear = rand.Next(1, 6),
                Throttle = Math.Round(rand.NextDouble(), 2),
                Brake = Math.Round(rand.NextDouble() * 0.4, 2),
                Timestamp = DateTime.UtcNow
            };

            string json = JsonSerializer.Serialize(telemetry);
            byte[] data = Encoding.UTF8.GetBytes(json);

            await udpClient.SendAsync(data, data.Length, endPoint);
            Console.WriteLine($"Sent: {json}");

            await Task.Delay(1000); // send every second
        }
    }
}
