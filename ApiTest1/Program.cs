using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;

namespace ApiTest1;

class Program
{
    static Option<string> OutputDirectoryOption = new Option<string>(new string[] { "-o", "--output-dir" }, () => "data", "output directory");
    static Option<string> ServerAddressOption = new Option<string>(new string[] { "-s", "--server-address" }, () => "127.0.0.1:8188", "server address");
    static Option<int> SeedStartOption = new Option<int>(new string[] { "-ss", "--seed-start" }, () => 0, "seed start");
    static Option<int> SeedEndOption = new Option<int>(new string[] { "-se", "--seed-end" }, () => 100, "seed end");

    static async Task Main(string[] args)
    {
        var prg = new Program();

        var root = new RootCommand();
        root.AddOption(OutputDirectoryOption);
        root.AddOption(ServerAddressOption);
        root.AddOption(SeedStartOption);
        root.AddOption(SeedEndOption);

        root.SetHandler(prg.DoProcess);
        await root.InvokeAsync(args);
    }

    CancellationTokenSource tokenSource = new CancellationTokenSource();

    Program()
    {
        Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e)=>
        {
            tokenSource.Cancel();
            e.Cancel = true;
        };
    }

    async Task DoProcess(InvocationContext context)
    {
        var outDir = context.ParseResult.GetValueForOption(OutputDirectoryOption);
        var serverAddress = context.ParseResult.GetValueForOption(ServerAddressOption);
        var seedStart = context.ParseResult.GetValueForOption(SeedStartOption);
        var seedEnd = context.ParseResult.GetValueForOption(SeedEndOption);

        try
        {
            using (var ws = new ClientWebSocket())
            {
                var uri = new Uri($"ws://{serverAddress}/ws");
                await ws.ConnectAsync(uri, tokenSource.Token);

                int seed = seedStart;
                while (ws.State == WebSocketState.Open && !tokenSource.IsCancellationRequested)
                {
                    var rbuf = new byte[1024];
                    var res = await ws.ReceiveAsync(rbuf, tokenSource.Token);
                    if (!tokenSource.IsCancellationRequested)
                    {
                        var json = Encoding.UTF8.GetString(rbuf, 0, res.Count);
                        Debug.WriteLine(json);
                        Console.WriteLine(json);
                        try
                        {
                            var receivedObject = JsonSerializer.Deserialize<ComfyReceivedObject>(json);

                            switch (receivedObject?.type)
                            {
                                case "status":
                                    {
                                        var statusObject = JsonSerializer.Deserialize<ComfyReceivedStatusObject>(json);
                                        if (statusObject?.data?.status?.exec_info != null && statusObject.data.status.exec_info.queue_remaining == 0)
                                        {
                                            if (seed > seedEnd)
                                            {
                                                tokenSource.Cancel();
                                                continue;
                                            }

                                            try
                                            {
                                                var workflow = Workflow.Build();
                                                workflow.Seed = seed;
                                                workflow.OutputFilePrefix = $"{outDir}/{seed:00000}";
                                                Debug.WriteLine(JsonSerializer.Serialize(workflow));

                                                var prompt = new Prompt<Workflow>(workflow);
                                                var client = new HttpClient()
                                                {
                                                    Timeout = TimeSpan.FromSeconds(300),
                                                };
                                                var wfRes = await client.PostAsJsonAsync($"http://{serverAddress}/prompt", prompt, tokenSource.Token);
                                                Debug.WriteLine(wfRes);
                                                var content = await wfRes.Content.ReadAsStringAsync();
                                                Debug.WriteLine(content);
                                                Console.WriteLine($"finish task, {seed}");
                                                seed++;
                                            }
                                            catch (Exception ex)
                                            {
                                                Debug.WriteLine(ex);
                                                Debugger.Break();
                                            }
                                        }
                                        else
                                        {
                                        }
                                    }
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            Console.WriteLine(ex);
            Debugger.Break();
        }
    }
}

public class ComfyReceivedObject
{
    public string type { get; set; }
}

public class ComfyReceivedStatusObject : ComfyReceivedObject
{
    public Data data { get; set; }

    public class Data
    {
        public Status status { get; set; }
        public string sid { get; set; }
    }

    public class Status
    {
        public Exec_Info exec_info { get; set; }
    }

    public class Exec_Info
    {
        public int queue_remaining { get; set; }
    }

}
