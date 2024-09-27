using System.CommandLine.Invocation;
using System.CommandLine;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;

namespace ApiTest2;

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
        Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
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

        var clientId = Guid.NewGuid().ToString();
        var promptId = string.Empty;
        Stream savingFile = null;
        bool firstSave = false;

        var client = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(300),
        };

        try
        {
            using (var ws = new ClientWebSocket())
            {
                var uri = new Uri($"ws://{serverAddress}/ws?clientId={clientId}");
                await ws.ConnectAsync(uri, tokenSource.Token);

                var rbuf = new byte[65536];
                int seed = seedStart;
                while (ws.State == WebSocketState.Open && !tokenSource.IsCancellationRequested)
                {
                    var res = await ws.ReceiveAsync(rbuf, tokenSource.Token);
                    if (!tokenSource.IsCancellationRequested && rbuf[0] == '{' && rbuf[1] == '"')
                    {
                        var json = Encoding.UTF8.GetString(rbuf, 0, res.Count);
                        Debug.WriteLine(json);
                        Console.WriteLine(json);
                        try
                        {
                            var tmpObj = JsonSerializer.Deserialize<ComfyReceivedObject>(json);
                            var receivedObj = tmpObj.type switch
                            {
                                "status" => JsonSerializer.Deserialize<ComfyReceivedStatusObject>(json),
                                "execution_start" => JsonSerializer.Deserialize<ComfyReceivedExecutionStartObject>(json),
                                "execution_cached" => JsonSerializer.Deserialize<ComfyReceivedExecutionCachedObject>(json),
                                "executing" => JsonSerializer.Deserialize<ComfyReceivedExecutingObject>(json),
                                "execution_success" => JsonSerializer.Deserialize<ComfyReceivedExecutionSuccessObject>(json),
                                "progress" => JsonSerializer.Deserialize<ComfyReceivedProgressObject>(json),
                                _ => tmpObj
                            };

                            switch (receivedObj)
                            {
                                case ComfyReceivedStatusObject status when status.data?.status?.exec_info != null && status.data.status.exec_info.queue_remaining == 0 && !string.IsNullOrEmpty(status?.data?.sid):
                                case ComfyReceivedExecutingObject executing when string.IsNullOrEmpty(executing.data?.node):
                                    {
                                        promptId = string.Empty;

                                        if (seed > seedEnd)
                                        {
                                            tokenSource.Cancel();
                                            continue;
                                        }

                                        try
                                        {
                                            var workflow = Workflow.Build();
                                            workflow.Seed = seed;
                                            Debug.WriteLine(JsonSerializer.Serialize(workflow));

                                            var prompt = new Prompt<Workflow>(workflow)
                                            {
                                                ClientId = clientId,
                                            };
                                            var wfRes = await client.PostAsJsonAsync($"http://{serverAddress}/prompt", prompt, tokenSource.Token);
                                            Debug.WriteLine(wfRes);
                                            var content = await wfRes.Content.ReadAsStringAsync();
                                            Debug.WriteLine(content);
                                            Console.WriteLine(content);
                                            try
                                            {
                                                var apiRes = JsonSerializer.Deserialize<ComfyHttpResult>(content);
                                                if (!String.IsNullOrEmpty(apiRes.prompt_id))
                                                {
                                                    promptId = apiRes.prompt_id;
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Debug.WriteLine(ex);
                                                Debugger.Break();
                                            }
                                            Console.WriteLine($"finish task, {seed}");
                                            seed++;
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine(ex);
                                            Debugger.Break();
                                        }
                                    }
                                    break;

                                case ComfyReceivedExecutingObject executing when executing.data?.node == "29" && executing.data?.prompt_id == promptId: // SaveImageWebsocket
                                    {
                                        savingFile = new FileStream(Path.Combine(outDir, $"{seed:00000}.png"), FileMode.OpenOrCreate, FileAccess.Write);
                                        firstSave = true;
                                    }
                                    break;

                                case ComfyReceivedExecutionSuccessObject execSuccess:
                                    if (savingFile != null)
                                    {
                                        savingFile.Close();
                                        savingFile.Dispose();
                                        savingFile = null;
                                    }
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }
                    }
                    else
                    {
                        Console.WriteLine($"binary : {res.Count}");
                        if (savingFile != null)
                        {
                            if (firstSave)
                            {
                                savingFile.Write(rbuf, 8, res.Count - 8);
                            }
                            else
                            {
                                savingFile.Write(rbuf, 0, res.Count);
                            }
                            firstSave = false;
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


public class ComfyHttpResult
{
    public string prompt_id { get; set; }
    public int number { get; set; }
    public Node_Errors node_errors { get; set; }

    public class Node_Errors
    {
    }
}


public class ComfyReceivedObject
{
    public string type { get; set; }
}
public class ComfyReceivedObject<T> : ComfyReceivedObject
{
    public T data { get; set; }
}
public class ComfyReceivedStatusObject : ComfyReceivedObject<ComfyDataStatus> { } // type = "status"
public class ComfyReceivedExecutionStartObject : ComfyReceivedObject<ComfyDataExecutionStart> { } // type = "execution_start"
public class ComfyReceivedExecutionCachedObject : ComfyReceivedObject<ComfyDataExecutionCached> { } // type = "execution_cached"
public class ComfyReceivedExecutingObject : ComfyReceivedObject<ComfyDataExecuting> { } // type = "executing"
public class ComfyReceivedExecutionSuccessObject : ComfyReceivedObject<ComfyDataExecutionSuccess> { } // type = "execution_success"
public class ComfyReceivedProgressObject : ComfyReceivedObject<ComfyDataProgress> { } // type = "progress"

public class ComfyDataStatus
{
    public Status status { get; set; }
    public string sid { get; set; }

    public class Status
    {
        public Exec_Info exec_info { get; set; }
    }

    public class Exec_Info
    {
        public int queue_remaining { get; set; }
    }
}

public class ComfyDataExecutionStart
{
    public string prompt_id { get; set; }
    public long timestamp { get; set; }
}

public class ComfyDataExecutionCached
{
    public string[] nodes { get; set; }
    public string prompt_id { get; set; }
    public long timestamp { get; set; }
}

public class ComfyDataExecuting
{
    public string node { get; set; }
    public string display_node { get; set; }
    public string prompt_id { get; set; }
}

public class ComfyDataExecutionSuccess
{
    public string prompt_id { get; set; }
    public long timestamp { get; set; }
}

public class ComfyDataProgress
{
    public int value { get; set; }
    public int max { get; set; }
    public string prompt_id { get; set; }
    public string node { get; set; }
}
