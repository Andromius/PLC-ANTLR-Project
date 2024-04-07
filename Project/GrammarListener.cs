using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using MyGrammar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using static Antlr4.Runtime.Atn.SemanticContext;

namespace Project;
public class GrammarListener(bool printRules = false) : MyGrammarBaseListener 
{
    private readonly Stack<Dictionary<string, VarType>> _variables = new();
    public bool HasError { get; private set; } = false;
    public bool PrintRules { get; set; } = printRules;
    public override void EnterEveryRule([NotNull] ParserRuleContext context)
    {
        base.EnterEveryRule(context);
        if (PrintRules)
            Console.WriteLine($"\u001b[96m{context.GetType().Name.Replace("Context", ""),-10}\u001b[0m\t\u001b[90m{context.GetText()}\u001b[0m");
    }
    public override void EnterProgram([NotNull] MyGrammarParser.ProgramContext context)
    {
        base.EnterProgram(context);
        _variables.Push([]);
    }

    public override void EnterBlockStmt([NotNull] MyGrammarParser.BlockStmtContext context)
    {
        base.EnterBlockStmt(context);
        _variables.Push([]);
    }

    public override void ExitBlockStmt([NotNull] MyGrammarParser.BlockStmtContext context)
    {
        base.ExitBlockStmt(context);
        _variables.Pop();
    }

    public override void EnterVarDecl([NotNull] MyGrammarParser.VarDeclContext context)
    {
        base.EnterVarDecl(context);
        
        VarType currentType = context.type().GetText() switch
        {
            "int" => VarType.INT,
            "float" => VarType.FLOAT,
            "string" => VarType.STRING,
            "bool" => VarType.BOOL
        };

        foreach (var identifier in context.ID())
        {
            string id = identifier.ToString()!;
            if (_variables.Any(x => x.ContainsKey(id)))
            {
                Console.WriteLine($"Variable with the identifier \"{id}\" has already been declared");
                HasError = true;
                continue;
            }

            var scope = _variables.Peek();
            scope[id] = currentType switch
            {
                VarType.INT => currentType,
                VarType.FLOAT => currentType,
                VarType.STRING => currentType,
                VarType.BOOL => currentType
            };
        }
    }

    public override void EnterAssign([NotNull] MyGrammarParser.AssignContext context)
    {
        base.EnterAssign(context);

        string variableIdentifier = context.ID().ToString()!;
        var scope = _variables.SingleOrDefault(x => x.ContainsKey(variableIdentifier));
        if (scope is null)
        {
            Console.WriteLine($"Attempt to assign value to an undeclared variable \"{variableIdentifier}\"");
            HasError = true;
            return;
        }
        
        VarType value = scope[variableIdentifier];
        VarType varType = ProcessExpr(context.expr());
        if (value != varType || (value is VarType.FLOAT && varType is not (VarType.INT or VarType.FLOAT)))
        {
            Console.WriteLine($"Attempt to assign a variable of type {varType} to a variable of type {value}");
            HasError = true;
            return;
        }    
    }

    public override void EnterUnaryExpr([NotNull] MyGrammarParser.UnaryExprContext context)
    {
        base.EnterUnaryExpr(context);
        if(context.NOT() is not null)
        {
            VarType varType = ProcessExpr(context.expr());
            if (varType is not VarType.BOOL)
            {
                Console.WriteLine($"Logical operator NOT cannot be used with type {varType}");
                HasError = true;
                return;
            } 
        }
        else if(context.SUB() is not null)
        {
            VarType varType = ProcessExpr(context.expr());
            if (varType is not VarType.INT or VarType.FLOAT)
            {
                Console.WriteLine($"Unary MINUS operator cannot be used with type {varType}");
                HasError = true;
                return;
            }
        }
    }

    private VarType ProcessExpr(MyGrammarParser.ExprContext expr)
    {
        if (expr.literal() is MyGrammarParser.LiteralContext literalContext)
        {
            if (literalContext.BOOL() is not null)
                return VarType.BOOL;
            if (literalContext.INT() is not null)
                return VarType.INT;
            if (literalContext.FLOAT() is not null)
                return VarType.FLOAT;
            if (literalContext.STRING() is not null)
                return VarType.STRING;
        }
        else if (expr.ID() is ITerminalNode node)
        {
            var scope = _variables.SingleOrDefault(x => x.ContainsKey(node.GetText()));
            //scope.TryGetValue(node.GetText(), out VarType value)
            if (scope is null)
            {
                Console.WriteLine($"Variable {node.GetText()} has not been declared");
                return VarType.UNKNOWN;
            }

            return scope[node.GetText()];
        }
        else if (expr.ChildCount == 3)
        {
            VarType first = ProcessExpr(expr.expr(0));
            VarType second = ProcessExpr(expr.expr(1));
            var opCtx = expr.op();
            string op = GetOperator(opCtx);
            VarType result = ProcessBinaryOp(first, second, op);
            if (result == VarType.UNKNOWN)
                Console.WriteLine($"Cannot use operator \"{op}\" with variables of type {first} and {second}");
            return result;
        }
        else if (expr.parenExpr() is MyGrammarParser.ParenExprContext parenExprContext)
            return ProcessExpr(parenExprContext.expr());
        else if (expr.unaryExpr() is MyGrammarParser.UnaryExprContext unaryExprContext)
        {
            string op = "";
            if (unaryExprContext.NOT() is not null)
                op = unaryExprContext.NOT().GetText();
            else if (unaryExprContext.SUB() is not null)
                op = unaryExprContext.SUB().GetText();

            VarType varType = ProcessExpr(unaryExprContext.expr());
            VarType result = ProcessUnaryOp(varType, op);
            if (result == VarType.UNKNOWN)
                Console.WriteLine($"Cannot use operator \"{op}\" with variable of type {varType}");
            return result;
        }
        else if (expr.assign() is MyGrammarParser.AssignContext assignContext)
        {
            ITerminalNode idNode = assignContext.ID();
            var scope = _variables.SingleOrDefault(x => x.ContainsKey(idNode.GetText()));
            if (scope is null)
            {
                Console.WriteLine($"Variable {idNode.GetText()} has not been declared");
                HasError = true;
                return VarType.UNKNOWN;
            }
            
            VarType value = scope[idNode.GetText()];
            VarType varType = ProcessExpr(assignContext.expr());
            if (value != varType || (value is VarType.FLOAT && varType is not (VarType.INT or VarType.FLOAT)))
            {
                Console.WriteLine($"Attempt to assign variable of type {varType} to variable of type {value}");
                HasError = true;
                return varType;
            }
        }

        return VarType.UNKNOWN;
    }

    private static VarType ProcessBinaryOp(VarType first, VarType second, string op) => op switch
    {
        "+" when first is VarType.INT && second is VarType.INT => VarType.INT,
        "+" when first is VarType.FLOAT && second is VarType.INT || first is VarType.INT && second is VarType.FLOAT => VarType.FLOAT,
        "+" => VarType.UNKNOWN,
        "-" when first is VarType.INT && second is VarType.INT => VarType.INT,
        "-" when first is VarType.FLOAT && second is VarType.INT || first is VarType.INT && second is VarType.FLOAT => VarType.FLOAT,
        "-" => VarType.UNKNOWN,
        "*" when first is VarType.INT && second is VarType.INT => VarType.INT,
        "*" when first is VarType.FLOAT && second is VarType.INT || first is VarType.INT && second is VarType.FLOAT => VarType.FLOAT,
        "*" => VarType.UNKNOWN,
        "/" when first is VarType.INT && second is VarType.INT => VarType.INT,
        "/" when first is VarType.FLOAT && second is VarType.INT || first is VarType.INT && second is VarType.FLOAT => VarType.FLOAT,
        "/" => VarType.UNKNOWN,
        "<" when first is VarType.INT or VarType.FLOAT && second is VarType.INT or VarType.FLOAT => VarType.BOOL,
        "<" => VarType.UNKNOWN,
        ">" when first is VarType.INT or VarType.FLOAT && second is VarType.INT or VarType.FLOAT => VarType.BOOL,
        ">" => VarType.UNKNOWN,
        "<=" when first is VarType.INT or VarType.FLOAT && second is VarType.INT or VarType.FLOAT => VarType.BOOL,
        "<=" => VarType.UNKNOWN,
        ">=" when first is VarType.INT or VarType.FLOAT && second is VarType.INT or VarType.FLOAT => VarType.BOOL,
        ">=" => VarType.UNKNOWN,
        "==" when first is VarType.INT or VarType.FLOAT or VarType.STRING && second is VarType.INT or VarType.FLOAT or VarType.STRING => VarType.BOOL,
        "==" => VarType.UNKNOWN,
        "!=" when first is VarType.INT or VarType.FLOAT or VarType.STRING && second is VarType.INT or VarType.FLOAT or VarType.STRING => VarType.BOOL,
        "!=" => VarType.UNKNOWN,
        "&&" when first is VarType.BOOL && second is VarType.BOOL => VarType.BOOL,
        "&&" => VarType.UNKNOWN,
        "||" when first is VarType.BOOL && second is VarType.BOOL => VarType.BOOL,
        "||" => VarType.UNKNOWN,
        "." when first is VarType.STRING && second is VarType.STRING => VarType.BOOL,
        "." => VarType.UNKNOWN,
        "%" when first is VarType.INT && second is VarType.INT => VarType.INT,
        "%" => VarType.UNKNOWN,
        _ => VarType.UNKNOWN
    };

    private static VarType ProcessUnaryOp(VarType type, string op) => op switch
    {
        "!" when type is VarType.BOOL => VarType.BOOL,
        "!" => VarType.UNKNOWN,
        "-" when type is VarType.INT or VarType.FLOAT => type,
        "-" => VarType.UNKNOWN,
        _ => VarType.UNKNOWN
    };

    private static string GetOperator(MyGrammarParser.OpContext opContext)
    {
        if (opContext.ADD() is not null)
            return opContext.ADD().GetText();
        if (opContext.SUB() is not null)
            return opContext.SUB().GetText();
        if (opContext.MUL() is not null)
            return opContext.MUL().GetText();
        if (opContext.DIV() is not null)
            return opContext.DIV().GetText();
        if (opContext.MOD() is not null)
            return opContext.MOD().GetText();
        if (opContext.LT() is not null)
            return opContext.LT().GetText();
        if (opContext.GT() is not null)
            return opContext.GT().GetText();
        if (opContext.LE() is not null)
            return opContext.LE().GetText();
        if (opContext.GE() is not null)
            return opContext.GE().GetText();
        if (opContext.EQ() is not null)
            return opContext.EQ().GetText();
        if (opContext.NE() is not null)
            return opContext.NE().GetText();
        if (opContext.AND() is not null)
            return opContext.AND().GetText();
        if (opContext.OR() is not null)
            return opContext.OR().GetText();
        if (opContext.NOT() is not null)
            return opContext.NOT().GetText();
        if (opContext.DOT() is not null)
            return opContext.DOT().GetText();
        return "";
    }
}
