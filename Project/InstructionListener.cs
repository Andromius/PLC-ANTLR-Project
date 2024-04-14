using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using MyGrammar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Project;
public class InstructionListener : MyGrammarBaseListener
{
    private int _labelCount = 0;
    public List<string> Instructions { get; private set; } = [];
    private readonly ReadOnlyDictionary<char, (string Value, VarType Type)> _defaults = new(new Dictionary<char, (string, VarType)>()
    {
        { 'I', ("0", VarType.INT) },
        { 'F', ("0.0", VarType.FLOAT) },
        { 'S', ("\"\"", VarType.STRING) },
        { 'B', ("false", VarType.BOOL) },
    });

    private readonly ReadOnlyDictionary<string, string> _binaryOperations = new(new Dictionary<string, string>()
    {
        { "-", "sub" },
        { "+", "add" },
        { "*", "mul" },
        { "/", "div" },
        { "%", "mod" },
        { ".", "concat" },
        { "&&", "and" },
        { "||", "or" },
        { ">", "gt" },
        { "<", "lt" },
        { "==", "eq" }
    });

    private readonly ReadOnlyDictionary<char, string> _unaryOperations = new(new Dictionary<char, string>()
    {
        { '-', "uminus" },
        { '!', "not" }
    });

    private readonly Stack<Dictionary<string, char>> _variables = new();
    public override void EnterProgram([NotNull] MyGrammarParser.ProgramContext context)
    {
        _variables.Push([]);
    }

    public override void ExitProgram([NotNull] MyGrammarParser.ProgramContext context)
    {
        _variables.Pop();
    }
    public override void EnterBlockStmt([NotNull] MyGrammarParser.BlockStmtContext context)
    {
        _variables.Push([]);
    }

    public override void ExitBlockStmt([NotNull] MyGrammarParser.BlockStmtContext context)
    {
        _variables.Pop();
    }

    public override void ExitAssign([NotNull] MyGrammarParser.AssignContext context)
    {
        if (context.expr().assign() is null)
            Instructions.Add("pop");
    }

    public override void EnterStatement([NotNull] MyGrammarParser.StatementContext context)
    {
        if (context.expr() is MyGrammarParser.ExprContext exprContext)
            HandleExpr(exprContext);

        return;
    }

    public override void EnterWhileStmt([NotNull] MyGrammarParser.WhileStmtContext context)
    {
        Instructions.Add($"label {_labelCount}");
        HandleExpr(context.expr());
        Instructions.Add($"fjmp {_labelCount + 1}");
    }

    public override void ExitWhileStmt([NotNull] MyGrammarParser.WhileStmtContext context)
    {
        Instructions.Add($"jmp {_labelCount}");
        _labelCount++;
        Instructions.Add($"label {_labelCount}");
        _labelCount++;
    }

    public override void ExitStatement([NotNull] MyGrammarParser.StatementContext context)
    {
        if (context.Parent is MyGrammarParser.IfStmtContext ifStmtContext)
        {
            if (ifStmtContext.statement(0) == context)
            {
                Instructions.Add($"jmp {_labelCount + 1}");
                Instructions.Add($"label {_labelCount}");
            }
        }
        base.ExitStatement(context);
    }

    public override void EnterIfStmt([NotNull] MyGrammarParser.IfStmtContext context)
    {
        HandleExpr(context.expr());
        Instructions.Add($"fjmp {_labelCount}");
    }

    public override void ExitIfStmt([NotNull] MyGrammarParser.IfStmtContext context)
    {
        _labelCount++;
        Instructions.Add($"label {_labelCount}");
        _labelCount++;
    }
    public override void EnterWriteStmt([NotNull] MyGrammarParser.WriteStmtContext context)
    {
        var expressions = context.expr();
        foreach (var expression in expressions)
        {
            HandleExpr(expression);
        }
        Instructions.Add($"print {expressions.Length}");  
    }

    public override void EnterReadStmt([NotNull] MyGrammarParser.ReadStmtContext context)
    {
        var identifiers = context.ID();
        foreach (var identifier in identifiers)
        {
            Instructions.Add($"read {_variables.Single(x => x.Any(v => v.Key == identifier.GetText()))[identifier.GetText()]}");
            Instructions.Add($"save {identifier.GetText()}");
        }
    }

    public override void EnterVarDecl([NotNull] MyGrammarParser.VarDeclContext context)
    {
        char type = context.type().GetText() switch
        {
            "int" => 'I',
            "float" => 'F',
            "string" => 'S',
            "bool" => 'B',
            _ => throw new ArgumentException("Somehow got a weird string")
        };
        
        foreach (var node in context.ID())
        {
            _variables.Peek().Add(node.GetText(), type);
            Instructions.Add($"push {type} {_defaults[type].Value}");
            Instructions.Add($"save {node.GetText()}");
        }
    }

    public override void ExitExpr([NotNull] MyGrammarParser.ExprContext context)
    {
        //if (context.Parent is MyGrammarParser.ExprContext)
        //    Console.WriteLine(context.GetText());
    }

    private string HandleExpr(MyGrammarParser.ExprContext expr)
    {
        if (expr.literal() is MyGrammarParser.LiteralContext literalContext)
        {
            if (literalContext.BOOL() is not null)
            {
                Instructions.Add($"push B {literalContext.BOOL().GetText()}");
            }
            else if (literalContext.INT() is not null)
            {
                Instructions.Add($"push I {literalContext.INT().GetText()}");
                return "INT";
            }
            else if (literalContext.FLOAT() is not null)
            {
                Instructions.Add($"push F {literalContext.FLOAT().GetText()}");
                return "FLOAT";
            }
            else if (literalContext.STRING() is not null)
            {
                Instructions.Add($"push S {literalContext.STRING().GetText()}");
            }
        }
        else if (expr.ID() is ITerminalNode node)
        {
            Instructions.Add($"load {node.GetText()}");
        }
        else if (expr.ChildCount == 3)
        {
            string type = HandleExpr(expr.expr(0));
            string type_second = HandleExpr(expr.expr(1));
            if (type is "INT" && type_second is "FLOAT")
                Instructions.Insert(Instructions.Count - 1, "itof");
            else if (type is "FLOAT" && type_second is "INT")
                Instructions.Add("itof");

            if (expr.op.Text is "!=")
            {
                Instructions.Add(_binaryOperations["=="]);
                Instructions.Add(_unaryOperations['!']);
            }
            else
                Instructions.Add(_binaryOperations[expr.op.Text]);
        }
        else if (expr.assign() is MyGrammarParser.AssignContext assignContext)
        {
            HandleExpr(assignContext.expr());
            
            string varName = assignContext.ID().GetText();
            if (_variables.Single(x => x.Any(v => v.Key == varName))[varName] == 'F')
                Instructions.Add("itof");
            Instructions.Add($"save {varName}");
            Instructions.Add($"load {varName}");
        }
        else if (expr.unaryExpr() is MyGrammarParser.UnaryExprContext unaryExprContext)
        {
            HandleExpr(unaryExprContext.expr());
            char op = unaryExprContext.NOT()?.GetText().First() ?? 
                unaryExprContext.SUB().GetText().First();

            Instructions.Add(_unaryOperations[op]);
        }
        else if (expr.parenExpr() is MyGrammarParser.ParenExprContext parenExprContext)
        {
            HandleExpr(parenExprContext.expr());
        }
        return "";
    }

    public void HandleVarDecl(MyGrammarParser.VarDeclContext context)
    {

    }
}
