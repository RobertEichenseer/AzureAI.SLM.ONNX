namespace FTA.AI.SLM.Intro;

using System.Text;
using Microsoft.ML.OnnxRuntimeGenAI;
using System.Diagnostics;

public class InferenceOnnx : IDisposable
{

    Config _config = new Config();


    Model _model;  
    Tokenizer _tokenizer;
    TokenizerStream _tokenizerStream;
    GeneratorParams _generatorParams;
    OgaHandle _ogaHandle;


    public InferenceOnnx(Config config)
    {
        _config = config;

        _model = new Model(config.ModelPath);
        _tokenizer = new Tokenizer(_model);
        _tokenizerStream = _tokenizer.CreateStream(); 
        _generatorParams = new GeneratorParams(_model); 
        _ogaHandle = new OgaHandle();

    }

    #pragma warning disable CS1998
    public async Task<(bool success, string modelResponse, float tokenPerSec)> Completion_Onnx(string userMessage)
    {
        string prompt = $"<|user|> {userMessage} \n <|end|>\n<|assistant|>";

        Sequences sequences = _tokenizer.Encode(prompt); 
        _generatorParams.SetSearchOption("max_length", 200);
        _generatorParams.SetSearchOption("temperature", 0.1f);
        _generatorParams.SetInputSequences(sequences); 
        _generatorParams.TryGraphCaptureWithMaxBatchSize(1); 

        using Generator generator = new Generator(_model, _generatorParams); 

        int tokenCount = 0; 
        Stopwatch stopWatch = Stopwatch.StartNew();

        StringBuilder modelResponse = new StringBuilder();
        while (!generator.IsDone())
        {
            generator.ComputeLogits();
            generator.GenerateNextToken();
            int token = generator.GetSequence(0)[tokenCount];
            modelResponse.Append(_tokenizerStream.Decode(token));
            tokenCount++;
        }

        stopWatch.Stop();
        float tokenPerSec = (float)tokenCount/stopWatch.ElapsedMilliseconds*1000;
        
        return (true, modelResponse.ToString(),tokenPerSec); 
    }
    #pragma warning restore CS1998

    #pragma warning disable CS1998
    public async IAsyncEnumerable<string> Completion_OnnxStream(string userMessage)
    {
        string prompt = $"<|user|> {userMessage} \n <|end|>\n<|assistant|>";

        Sequences sequences = _tokenizer.Encode(prompt); 
        _generatorParams.SetSearchOption("max_length", 500);
        _generatorParams.SetSearchOption("temperature", 0.1f);
        _generatorParams.SetInputSequences(sequences); 
        _generatorParams.TryGraphCaptureWithMaxBatchSize(1); 
        
        using Generator generator = new Generator(_model, _generatorParams); 

        int tokenCount = 0; 
        Stopwatch stopWatch = Stopwatch.StartNew();

        while (!generator.IsDone())
        {
            generator.ComputeLogits();
            generator.GenerateNextToken();
            int token = generator.GetSequence(0)[tokenCount];
            string decodedToken = _tokenizerStream.Decode(token);
            yield return decodedToken;
            tokenCount++;
        }
    }
    #pragma warning restore 1998

    public void Dispose()
    {
        _model.Dispose();
        _tokenizer.Dispose();
        _tokenizerStream.Dispose();
        _generatorParams.Dispose();
        _ogaHandle.Dispose();
    }
}
