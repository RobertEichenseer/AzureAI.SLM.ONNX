using FTA.AI.SLM.Intro;
using DotNetEnv; 
using System.Text.Json; 

//Read configuration
string _configurationFile = "../../config/config.env";
Env.Load(_configurationFile);
Config config = new Config(){
    ModelPath = Env.GetString("SLM_MODELPATH")
};
InferenceOnnx inferenceOnnx = new InferenceOnnx(config);

var builder = WebApplication.CreateBuilder(args); 

builder.WebHost.UseKestrel();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder =>
        {
            builder
                .WithOrigins("http://localhost:4200")
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

var app = builder.Build();
app.UseCors("AllowSpecificOrigin");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// API endpoints
app.MapGet("/response", async (string text) =>
{
    string userMessage = text; 
    (bool success, string modelResponse, float tokenPerSec) result = await inferenceOnnx.Completion_Onnx(userMessage);
    var response = new {
        tokenPerSec = result.tokenPerSec,
        modelResponse = result.modelResponse,
    };
    return response;
})
.WithName("GetResponse")
.WithOpenApi();

app.MapGet("/responsestream", async (HttpContext context, string text) =>
{

    // context.Response.ContentType = "text/event-stream";
    // await using (StreamWriter streamWriter = new StreamWriter(context.Response.Body))
    // {
    //     foreach (string token in text.Split(" "))
    //     {
    //         await streamWriter.WriteLineAsync(token);
    //         await Task.Delay(1000);
    //         await streamWriter.FlushAsync();
    //     }
    // } 


    string userMessage = text;
    context.Response.ContentType = "text/event-stream";
    await using (StreamWriter streamWriter = new StreamWriter(context.Response.Body))
    {
        await foreach (string token in inferenceOnnx.Completion_OnnxStream(userMessage))
        {
            await streamWriter.WriteLineAsync(token);
            await streamWriter.FlushAsync();
        }
    }
});

app.MapGet("/ping", (string text) =>
{
    var response = new {
        response = text
    };
    
    return response;
})
.WithName("Ping")
.WithOpenApi();

app.Lifetime.ApplicationStopping.Register(() =>
{
    inferenceOnnx.Dispose();
});


// Run app
app.Run();
