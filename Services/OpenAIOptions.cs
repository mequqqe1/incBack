// Services/OpenAIOptions.cs
public class OpenAIOptions
{
    public string ApiKey { get; set; } = "";          // заполняется конфигом/секретами
    public string Model  { get; set; } = "gpt-4o-mini";
}