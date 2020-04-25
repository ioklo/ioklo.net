using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickSC;
using Microsoft.JSInterop;
using System.IO;

namespace Homepage.QuickSC
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            await builder.Build().RunAsync();
        }

        class QsDemoCommandProvider : IQsCommandProvider
        {
            StringBuilder sb = new StringBuilder();

            public void Execute(string text)
            {
                text = text.Trim();

                if (text.StartsWith("echo "))
                {
                    sb.Append(text.Substring(5).Replace("\\n", "\n"));
                }
                else
                {
                    sb.AppendLine($"알 수 없는 명령어 입니다: {text}");
                }
            }

            public string GetOutput() => sb.ToString();
        }

        [JSInvokable]
        public static async ValueTask<string> RunAsync(string input)
        {
            try
            {
                var lexer = new QsLexer();
                var parser = new QsParser(lexer);
                var buffer = new QsBuffer(new StringReader(input));
                var pos = await buffer.MakePosition().NextAsync();
                var parserContext = QsParserContext.Make(QsLexerContext.Make(pos));

                var scriptResult = await parser.ParseScriptAsync(parserContext);
                if (!scriptResult.HasValue)
                    return "에러 (파싱 실패)";

                var demoCmdProvider = new QsDemoCommandProvider();

                var evaluator = new QsEvaluator(demoCmdProvider);
                var evalContext = QsEvalContext.Make();
                var newEvalContext = evaluator.EvaluateScript(scriptResult.Elem, evalContext);
                if (newEvalContext == null)
                    return "에러 (실행 실패)";

                return demoCmdProvider.GetOutput();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
    }
}
