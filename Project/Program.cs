namespace Project;

using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using MyGrammar;

public class Program
{

    public static void Main(string[] args)
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        var fileName = "C:\\Users\\marti\\Desktop\\PJP\\ANTLR\\Project\\Project\\test_1.txt";
        Console.WriteLine("Parsing: " + fileName);
        var inputFile = new StreamReader(fileName);
        AntlrInputStream input = new(inputFile);
        MyGrammarLexer lexer = new(input);
        CommonTokenStream tokens = new(lexer);
        MyGrammarParser parser = new(tokens);

        parser.RemoveErrorListeners();
        parser.AddErrorListener(new VerboseListener());

        IParseTree tree = parser.program();

        if (parser.NumberOfSyntaxErrors != 0) return;

        ParseTreeWalker walker = new();
        GrammarListener grammarListener = new();
        walker.Walk(grammarListener, tree);

        if (grammarListener.HasError)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Type checking has finished with Errors");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(string.Join('\n', grammarListener.Errors));
            return;
        }
    }
}
