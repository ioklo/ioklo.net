using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace QuickSC
{
    public class FuncionalTest
    {
        //[Fact]
        //public async Task TestEvaluateScript()
        //{
        //    var evaluator = new QsEvaluator();
        //    var buffer = new QsBuffer(new StringReader("cmd /c notepad"));
        //    var lexer = new QsLexer();
        //    var parser = new QsParser(lexer);
        //    var parserContext = QsParserContext.Make(QsLexerContext.Make(await buffer.MakePosition().NextAsync()));

        //    var scriptResult = await parser.ParseScriptAsync(parserContext);

        //    var evalContext = new QsEvalContext();
        //    evaluator.EvaluateScript(scriptResult.Elem, evalContext);

        //    // �ؾ� �� ��
        //    // Abstract Syntax �����
        //    // 1. ����
        //    // 2. $, ${ .... }
        //    // 3. Literal 
        //    // Script = ExecutionExpression

        //    // ��ü �׽�Ʈ
        //    // literal�� ������ ����..
        //    // 

        //    // �Ľ� �׽�Ʈ
        //    // SyntaxTree Execution �׽�Ʈ
        //}
    }
}
