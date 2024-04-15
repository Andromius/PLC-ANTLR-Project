using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using MyGrammar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Project;
public class InstructionListener(ParseTreeProperty<VarType> parseTreeProperty) : MyGrammarBaseListener
{
    private int _labelCount = 0;
    private int _labelWhileStart = 0;
    private bool _needsConversion = false;
    //public List<Instruction> Ins { get; private set; } = [];
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
    private readonly ParseTreeProperty<VarType> _parseTreeProperty = parseTreeProperty;
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
        //if (context.expr().assign() is null)
        //{
        //    //Ins.Add(new(InstructionType.POP));
        //    Instructions.Add("pop");
        //}
    }

    public override void EnterStatement([NotNull] MyGrammarParser.StatementContext context)
    {
        if (context.expr() is MyGrammarParser.ExprContext exprContext)
            HandleExpr(exprContext);

        return;
    }

    public override void EnterWhileStmt([NotNull] MyGrammarParser.WhileStmtContext context)
    {
        _labelWhileStart = _labelCount;
        Instructions.Add($"label {_labelCount}");
        _labelCount++;
        //Ins.Add(new ValueInstruction(InstructionType.LABEL, _labelCount.ToString()));
        HandleExpr(context.expr());
        //Ins.Add(new ValueInstruction(InstructionType.FJMP, (_labelCount + 1).ToString()));
        Instructions.Add($"fjmp {_labelCount}");
    }

    public override void ExitWhileStmt([NotNull] MyGrammarParser.WhileStmtContext context)
    {
        //Instructions.Add($"jmp {_labelCount}");
        Instructions.Add($"jmp {_labelWhileStart}");
        //Ins.Add(new ValueInstruction(InstructionType.JMP, _labelCount.ToString()));
        _labelCount++;
        //Ins.Add(new ValueInstruction(InstructionType.LABEL, _labelCount.ToString()));
        Instructions.Add($"label {_labelWhileStart+1}");
        //_labelCount++;
    }

    public override void ExitStatement([NotNull] MyGrammarParser.StatementContext context)
    {
        if (context.Parent is MyGrammarParser.IfStmtContext ifStmtContext)
        {
            if (ifStmtContext.statement(0) == context)
            {
                //Ins.Add(new ValueInstruction(InstructionType.JMP, (_labelCount + 1).ToString()));
                Instructions.Add($"jmp {_labelCount + 1}");
                //Ins.Add(new ValueInstruction(InstructionType.LABEL, _labelCount.ToString()));
                Instructions.Add($"label {_labelCount}");
            }
        }
        if (context.expr() is not null)
        {
            Instructions.Add("pop");
        }
        base.ExitStatement(context);
    }

    public override void EnterIfStmt([NotNull] MyGrammarParser.IfStmtContext context)
    {
        HandleExpr(context.expr());
        Instructions.Add($"fjmp {_labelCount}");
        //Ins.Add(new ValueInstruction(InstructionType.FJMP, _labelCount.ToString()));
    }

    public override void ExitIfStmt([NotNull] MyGrammarParser.IfStmtContext context)
    {
        _labelCount++;
        Instructions.Add($"label {_labelCount}");
        //Ins.Add(new ValueInstruction(InstructionType.LABEL, _labelCount.ToString()));
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
        //Ins.Add(new ValueInstruction(InstructionType.PRINT, expressions.Length.ToString()));
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
                //Ins.Add(new PushInstruction(VarType.INT, literalContext.INT().GetText()));
                Instructions.Add($"push I {literalContext.INT().GetText()}");
                return "INT";
            }
            else if (literalContext.FLOAT() is not null)
            {
                //Ins.Add(new PushInstruction(VarType.FLOAT, literalContext.FLOAT().GetText()));
                Instructions.Add($"push F {literalContext.FLOAT().GetText()}");
                return "FLOAT";
            }
            else if (literalContext.STRING() is not null)
            {
                //Ins.Add(new PushInstruction(VarType.STRING, literalContext.STRING().GetText()));
                Instructions.Add($"push S {literalContext.STRING().GetText()}");
            }
        }
        else if (expr.ID() is ITerminalNode node)
        {
            //Ins.Add(new ValueInstruction(InstructionType.LOAD, node.GetText()));
            Instructions.Add($"load {node.GetText()}");
        }
        else if (expr.ChildCount == 3)
        {
            var type1 = _parseTreeProperty.Get(expr.expr(0));
            var type2 = _parseTreeProperty.Get(expr.expr(1));
            string type = HandleExpr(expr.expr(0));
            if (type1 is VarType.INT && type2 is VarType.FLOAT)
            {
                Instructions.Add("itof");
            }
            if (type1 is VarType.FLOAT && type2 is VarType.INT)
            {
                Instructions.Add("itof");
            }
            string type_second = HandleExpr(expr.expr(1));
            if (type1 is VarType.FLOAT && type2 is VarType.INT)
            {
                Instructions.Add("itof");
            }

            if (expr.op.Text is "!=")
            {
                Instructions.Add(_binaryOperations["=="]);
                Instructions.Add(_unaryOperations['!']);
            }
            else
            {
                if (type1 is VarType.INT && type2 is VarType.FLOAT ||
                    type1 is VarType.FLOAT && type2 is VarType.INT)
                {
                    if (expr.op.Text is "*" or "/" or "-" or "+")
                    {
                        Instructions.Add($"{_binaryOperations[expr.op.Text]} F");
                    }
                    else
                    {
                        Instructions.Add($"{_binaryOperations[expr.op.Text]}");
                    }

                    return "F";
                }
                else
                {
                    if (expr.op.Text is ("*" or "/" or "-" or "+"))
                    {
                        Instructions.Add($"{_binaryOperations[expr.op.Text]} {type switch {
                            "INT" => "I",
                            "FLOAT" => "F",
                            _ => type_second switch 
                            { 
                                "INT" => "I",
                                "FLOAT" => "F"
                            }
                        }}");
                    }
                    else
                        Instructions.Add($"{_binaryOperations[expr.op.Text]}");
                    return type;
                }
            }

            if (type is "INT" && type_second is "FLOAT" ||
                type is "FLOAT" && type_second is "INT")
                return "F";

        }
        else if (expr.assign() is MyGrammarParser.AssignContext assignContext)
        {
            var type1 = _parseTreeProperty.Get(assignContext.expr());
            string type = HandleExpr(assignContext.expr());
            
            string varName = assignContext.ID().GetText();
            if (_variables.Single(x => x.Any(v => v.Key == varName))[varName] == 'F' && type1 == VarType.INT)
            {
                Instructions.Add("itof");
            }
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
            return HandleExpr(parenExprContext.expr());
        }
        return "";
    }
}
