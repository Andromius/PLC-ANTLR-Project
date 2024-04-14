namespace Project;

using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using MyGrammar;

public class Program
{
    public static void Run(List<string> fileNames)
    {
        foreach (var fileName in fileNames)
        {
            Console.WriteLine("\u001b[93mParsing:\u001b[0m " + fileName);
            var inputFile = new StreamReader(fileName);
            AntlrInputStream input = new(inputFile);
            MyGrammarLexer lexer = new(input);
            CommonTokenStream tokens = new(lexer);
            MyGrammarParser parser = new(tokens);

            parser.RemoveErrorListeners();
            parser.AddErrorListener(new VerboseListener());

            IParseTree tree = parser.program();

            if (parser.NumberOfSyntaxErrors != 0)
            {
                Console.WriteLine();
                continue;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Finished parsing {fileName} without issues");
            Console.ForegroundColor = ConsoleColor.Gray;

            ParseTreeWalker walker = new();
            GrammarListener grammarListener = new();
            walker.Walk(grammarListener, tree);

            if (grammarListener.HasError)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Type checking {fileName} has finished with Errors");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine(string.Join('\n', grammarListener.Errors));
                Console.WriteLine();
                continue;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Finished type checking {fileName} without issues");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();

            walker = new();
            InstructionListener instructionListener = new();
            walker.Walk(instructionListener, tree);
            Console.WriteLine(string.Join('\n', instructionListener.Instructions));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Finished creating instructions");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
        }
    }

    public static void Main(string[] args)
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        Run(["test_1.txt",
             "test_2.txt",
             "test_3.txt",
             "test_err.txt"]);
    }
}
