using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using MyGrammar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project;
public class GrammarListener : MyGrammarBaseListener 
{
    private readonly Dictionary<string, VarType> _variables = [];
    public bool HasError { get; private set; } = false;
    //public override void EnterEveryRule([NotNull] ParserRuleContext context)
    //{
    //    base.EnterEveryRule(context);
    //    Console.WriteLine($"\u001b[96m{context.GetType().Name.Replace("Context", ""),-10}\u001b[0m\t\u001b[90m{context.GetText()}\u001b[0m");
    //}

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
            if (_variables.ContainsKey(id))
            {
                Console.WriteLine("Variable with the same identifier has already been declared");
                HasError = true;
                continue;
            }

            _variables[id] = currentType switch
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
        if (!_variables.ContainsKey(variableIdentifier))
        {
            Console.WriteLine($"Attempt to assign value to an undeclared variable \"{variableIdentifier}\"");
            HasError = true;
            return;
        }

        ParserRuleContext? expr = null;
        do
        {
            expr = expr is null ? context.expr() : (expr as MyGrammarParser.AssignContext)?.expr();
        } while (expr is MyGrammarParser.AssignContext);

        MyGrammarParser.ExprContext exprContext = (MyGrammarParser.ExprContext)expr!;
        if (exprContext.literal() is MyGrammarParser.LiteralContext literalContext)
            ProcessLiteral(variableIdentifier, literalContext);
        else if (exprContext.ID() is ITerminalNode node)
        {
            if (!_variables.ContainsKey(node.GetText()))
            {
                Console.WriteLine($"Attempt to assign undeclared variable {node.GetText()} to variable {variableIdentifier}");
                HasError = true;
                return;
            }
            
            if (_variables[variableIdentifier] != _variables[node.GetText()] || 
                (_variables[variableIdentifier] is VarType.FLOAT && _variables[node.GetText()] is not VarType.INT))
            {
                Console.WriteLine($"Attempt to assign variable \"{node.GetText()}\" of type {_variables[node.GetText()]} to variable" +
                    $" \"{variableIdentifier}\" of type {_variables[variableIdentifier]}");
                HasError = true;
                return;
            }
        }
            
    }

    public override void EnterUnaryExpr([NotNull] MyGrammarParser.UnaryExprContext context)
    {
        base.EnterUnaryExpr(context);
        MyGrammarParser.ExprContext? expr = null;
        if(context.NOT() is not null)
        {
            expr = context.expr();
            do
            {
                if (expr.literal() is not null)
                {
                    if (expr.literal().BOOL() is null)
                        Console.WriteLine("Value must be of type BOOL for the NOT operator");
                }

            } while (expr is MyGrammarParser.ExprContext);
        }
        else if(context.SUB() is not null)
        {

        }
    }

    public override void EnterExpr([NotNull] MyGrammarParser.ExprContext context)
    {
        base.EnterExpr(context);
    }

    private static void WriteAssignmentError(VarType literalType, VarType variableType) => 
        Console.WriteLine($"Attempt to assign value with type {literalType} to a variable with type {variableType}");

    private void ProcessLiteral(string variableIdentifier, MyGrammarParser.LiteralContext literalContext)
    {
        if (literalContext.INT() is not null)
        {
            if (_variables[variableIdentifier] is not (VarType.INT or VarType.FLOAT))
            {
                WriteAssignmentError(VarType.INT, _variables[variableIdentifier]);
                HasError = true;
                return;
            }
        }
        else if (literalContext.BOOL() is not null)
        {
            if (_variables[variableIdentifier] != VarType.BOOL)
            {
                WriteAssignmentError(VarType.BOOL, _variables[variableIdentifier]);
                HasError = true;
                return;
            }
        }
        else if (literalContext.STRING() is not null)
        {
            if (_variables[variableIdentifier] != VarType.STRING)
            {
                WriteAssignmentError(VarType.STRING, _variables[variableIdentifier]);
                HasError = true;
                return;
            }
        }
        else if (literalContext.FLOAT() is not null)
        {
            if (_variables[variableIdentifier] != VarType.FLOAT)
            {
                WriteAssignmentError(VarType.STRING, _variables[variableIdentifier]);
                HasError = true;
                return;
            }
        }
    }
}
