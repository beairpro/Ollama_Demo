using System;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var baseUrl = "http://localhost:11434"; // Ollama 地址
        using var ollamaClient = new OllamaClient(baseUrl);

        Console.WriteLine("✅ Ollama 聊天客户端已启动！");
        Console.WriteLine("👉 输入你的问题开始对话。");
        Console.WriteLine("💬 输入 /reset 重置对话历史，输入 /exit 退出程序。\n");

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("\n你：");
            Console.ResetColor();
            string? message = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(message))
                continue;

            if (message.Trim().Equals("/exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("👋 再见！");
                break;
            }
            if (message.Trim().Equals("/reset", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("🔄 已重置（但当前示例程序未保存上下文）");
                continue;
            }

            try
            {
                using var cts = new CancellationTokenSource();
                await ollamaClient.SendMessageStreamAsync(message, cts.Token);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ 错误：{ex.Message}");
                Console.ResetColor();
            }
        }
    }
}
