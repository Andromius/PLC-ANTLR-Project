using Antlr4.Runtime.Misc;
using Antlr4.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project;
public class VerboseListener : BaseErrorListener
{
    public override void SyntaxError(TextWriter output, [NotNull] IRecognizer recognizer, [Nullable] IToken offendingSymbol, int line, int charPositionInLine, [NotNull] string msg, [Nullable] RecognitionException e)
    {

        IList<string> stack = ((Parser)recognizer).GetRuleInvocationStack();
        stack.Reverse();
        Console.Error.WriteLine("rule stack: " + string.Join(", ", stack));
        Console.Error.WriteLine("line " + line + ":" + charPositionInLine + " at " + offendingSymbol + ": " + msg);
    }
}
