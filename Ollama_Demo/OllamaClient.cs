using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class OllamaClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private bool _disposed = false;

    public OllamaClient(string baseUrl)
    {
        _baseUrl = baseUrl;
        _httpClient = new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan // 设置 HttpClient 不超时
        };
    }

    public async Task SendMessageStreamAsync(string message, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}/v1/chat/completions";

        var requestBody = new
        {
            model = "deepseek-r1:14b",
            messages = new[]
            {
                new { role = "user", content = message }
            },
            temperature = 0.7,
            max_tokens = 2048,
            stream = true // 🔥开启流式
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        try
        {

            //using var response = await _httpClient.PostAsync(url, content, cancellationToken);

            using var response = await _httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, url) { Content = content },
                HttpCompletionOption.ResponseHeadersRead, // 关键
                cancellationToken
            );

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

            Console.WriteLine("\n🧠 Ollama 正在思考...");

            var buffer = new byte[4096];
            int bytesRead;
            var textBuffer = new StringBuilder();

            while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                var chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                textBuffer.Append(chunk);

                while (true)
                {
                    var newlineIndex = textBuffer.ToString().IndexOf("\n");

                    if (newlineIndex == -1)
                        break;

                    var line = textBuffer.ToString(0, newlineIndex).Trim();
                    textBuffer.Remove(0, newlineIndex + 1);

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (line.StartsWith("data: "))
                        line = line.Substring(6);

                    if (line == "[DONE]")
                        return;

                    try
                    {
                        var jsonDoc = JsonDocument.Parse(line);
                        var root = jsonDoc.RootElement;

                        if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                        {
                            var delta = choices[0].GetProperty("delta");
                            if (delta.TryGetProperty("content", out var contentPart))
                            {
                                var textPart = contentPart.GetString();
                                if (!string.IsNullOrEmpty(textPart))
                                {
                                    Console.Write(textPart); // 逐字符实时输出！
                                    await Task.Delay(10, cancellationToken);
                                }
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Ignore invalid JSON
                    }
                }
            }

            Console.WriteLine();
        }
        catch (HttpRequestException hre)
        {
            throw new Exception($"请求失败：{hre.Message}", hre);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _httpClient.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
