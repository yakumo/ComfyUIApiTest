using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ApiTest2;

public class Workflow
{
    const string WorkflowFilename = "workflow_api.json";

    public static Workflow Build()
    {
        var assembly = Assembly.GetExecutingAssembly();
        string source;
        using (var s = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{WorkflowFilename}"))
        {
            using (var ts = new StreamReader(s))
            {
                source = ts.ReadToEnd();
            }
        }
        Debug.WriteLine(source);
        return JsonSerializer.Deserialize<Workflow>(source);
    }

    [JsonConstructor]
    private Workflow() { }

    [JsonIgnore]
    public int Seed
    {
        get => n13.inputs.seed;
        set => n13.inputs.seed = value;
    }

    [JsonPropertyName("3")]
    public _3 n3 { get; set; }
    [JsonPropertyName("4")]
    public _4 n4 { get; set; }
    [JsonPropertyName("6")]
    public _6 n6 { get; set; }
    [JsonPropertyName("10")]
    public _10 n10 { get; set; }
    [JsonPropertyName("11")]
    public _11 n11 { get; set; }
    [JsonPropertyName("12")]
    public _12 n12 { get; set; }
    [JsonPropertyName("13")]
    public _13 n13 { get; set; }
    [JsonPropertyName("14")]
    public _14 n14 { get; set; }
    [JsonPropertyName("15")]
    public _15 n15 { get; set; }
    [JsonPropertyName("23")]
    public _23 n23 { get; set; }
    [JsonPropertyName("29")]
    public _29 n29 { get; set; }

    public class _3
    {
        public Inputs inputs { get; set; }
        public string class_type { get; set; }
        public _Meta _meta { get; set; }
    }

    public class Inputs
    {
        public string control_net_name { get; set; }
    }

    public class _Meta
    {
        public string title { get; set; }
    }

    public class _4
    {
        public Inputs1 inputs { get; set; }
        public string class_type { get; set; }
        public _Meta1 _meta { get; set; }
    }

    public class Inputs1
    {
        public string json_str { get; set; }
    }

    public class _Meta1
    {
        public string title { get; set; }
    }

    public class _6
    {
        public Inputs2 inputs { get; set; }
        public string class_type { get; set; }
        public _Meta2 _meta { get; set; }
    }

    public class Inputs2
    {
        public bool render_body { get; set; }
        public bool render_hand { get; set; }
        public bool render_face { get; set; }
        public object[] kps { get; set; }
    }

    public class _Meta2
    {
        public string title { get; set; }
    }

    public class _10
    {
        public Inputs3 inputs { get; set; }
        public string class_type { get; set; }
        public _Meta3 _meta { get; set; }
    }

    public class Inputs3
    {
        public string ckpt_name { get; set; }
    }

    public class _Meta3
    {
        public string title { get; set; }
    }

    public class _11
    {
        public Inputs4 inputs { get; set; }
        public string class_type { get; set; }
        public _Meta4 _meta { get; set; }
    }

    public class Inputs4
    {
        public string text { get; set; }
        public object[] clip { get; set; }
    }

    public class _Meta4
    {
        public string title { get; set; }
    }

    public class _12
    {
        public Inputs5 inputs { get; set; }
        public string class_type { get; set; }
        public _Meta5 _meta { get; set; }
    }

    public class Inputs5
    {
        public string text { get; set; }
        public object[] clip { get; set; }
    }

    public class _Meta5
    {
        public string title { get; set; }
    }

    public class _13
    {
        public Inputs6 inputs { get; set; }
        public string class_type { get; set; }
        public _Meta6 _meta { get; set; }
    }

    public class Inputs6
    {
        public int seed { get; set; }
        public int steps { get; set; }
        public int cfg { get; set; }
        public string sampler_name { get; set; }
        public string scheduler { get; set; }
        public int denoise { get; set; }
        public object[] model { get; set; }
        public object[] positive { get; set; }
        public object[] negative { get; set; }
        public object[] latent_image { get; set; }
    }

    public class _Meta6
    {
        public string title { get; set; }
    }

    public class _14
    {
        public Inputs7 inputs { get; set; }
        public string class_type { get; set; }
        public _Meta7 _meta { get; set; }
    }

    public class Inputs7
    {
        public int width { get; set; }
        public int height { get; set; }
        public int batch_size { get; set; }
    }

    public class _Meta7
    {
        public string title { get; set; }
    }

    public class _15
    {
        public Inputs8 inputs { get; set; }
        public string class_type { get; set; }
        public _Meta8 _meta { get; set; }
    }

    public class Inputs8
    {
        public object[] samples { get; set; }
        public object[] vae { get; set; }
    }

    public class _Meta8
    {
        public string title { get; set; }
    }

    public class _23
    {
        public Inputs9 inputs { get; set; }
        public string class_type { get; set; }
        public _Meta9 _meta { get; set; }
    }

    public class Inputs9
    {
        public int strength { get; set; }
        public object[] conditioning { get; set; }
        public object[] control_net { get; set; }
        public object[] image { get; set; }
    }

    public class _Meta9
    {
        public string title { get; set; }
    }

    public class _29
    {
        public Inputs10 inputs { get; set; }
        public string class_type { get; set; }
        public _Meta10 _meta { get; set; }
    }

    public class Inputs10
    {
        public object[] images { get; set; }
    }

    public class _Meta10
    {
        public string title { get; set; }
    }
}

public class Prompt<T>
{
    public Prompt(T workflow) { Workflow = workflow; }
    private Prompt() { }

    [JsonPropertyName("prompt")]
    public T Workflow { get; set; }
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = Guid.NewGuid().ToString();
}
