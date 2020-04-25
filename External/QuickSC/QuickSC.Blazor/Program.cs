using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.IO;
using System.Collections.Immutable;

namespace QuickSC.Blazor
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            builder.Services.AddBaseAddressHttpClient();

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
                    sb.AppendLine($"�� �� ���� ��ɾ� �Դϴ�: {text}");
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
                    return "���� (�Ľ� ����)";

                var demoCmdProvider = new QsDemoCommandProvider();

                var evaluator = new QsEvaluator(demoCmdProvider);
                var evalContext = QsEvalContext.Make();
                var newEvalContext = evaluator.EvaluateScript(scriptResult.Elem, evalContext);
                if (newEvalContext == null)
                    return "���� (���� ����)";

                return demoCmdProvider.GetOutput();
            }
            catch (Exception e)
            {
                return e.ToString();
            }
        }
    }
}
