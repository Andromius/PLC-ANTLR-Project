using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project;
public class Interpreter(string path)
{
    private int _instructionPointer = 0;
    private readonly List<string> _instructions = File.ReadAllLines(path).ToList();
    private readonly Stack<string> _values = new();
    private readonly Dictionary<string, Variable> _variables = [];

    public void Interpret()
    {
        while (_instructionPointer < _instructions.Count)
        {
            string[] instruction = _instructions[_instructionPointer].Split(' ', 2);
            switch (instruction[0])
            {
                case "add":
                {
                    string value1 = _values.Pop().Split(' ', 2)[1];
                    string value2 = _values.Pop().Split(' ', 2)[1];
                    if (instruction[1] == "I")
                    {
                        _values.Push($"I {int.Parse(value1) + int.Parse(value2)}");
                        break;
                    }
                    _values.Push($"F {float.Parse(value1) + float.Parse(value2)}");
                    break;
                }
                case "sub":
                {
                    string value1 = _values.Pop().Split(' ', 2)[1];
                    string value2 = _values.Pop().Split(' ', 2)[1];
                    if (instruction[1] == "I")
                    {
                        _values.Push($"I {int.Parse(value2) - int.Parse(value1)}");
                        break;
                    }
                    _values.Push($"F {float.Parse(value2) - float.Parse(value1)}");
                    break;
                }
                case "mul":
                {
                    string value1 = _values.Pop().Split(' ', 2)[1];
                    string value2 = _values.Pop().Split(' ', 2)[1];
                    if (instruction[1] == "I")
                    {
                        _values.Push($"I {int.Parse(value1) * int.Parse(value2)}");
                        break;
                    }
                    string evaluatedExpression = (float.Parse(value2) * float.Parse(value1)).ToString();
                    evaluatedExpression = evaluatedExpression.Contains('.') ? evaluatedExpression : $"{evaluatedExpression}.0";
                    _values.Push($"F {evaluatedExpression}");
                    break;
                }
                case "div":
                {
                    string value1 = _values.Pop().Split(' ', 2)[1];
                    string value2 = _values.Pop().Split(' ', 2)[1];
                    if (instruction[1] == "I")
                    {
                        _values.Push($"I {int.Parse(value2) / int.Parse(value1)}");
                        break;
                    }
                    string evaluatedExpression = (float.Parse(value2) / float.Parse(value1)).ToString();
                    evaluatedExpression = evaluatedExpression.Contains('.') ? evaluatedExpression : $"{evaluatedExpression}.0";
                    _values.Push($"F {evaluatedExpression}");
                    break;
                }
                case "mod":
                {
                    string[] value1 = _values.Pop().Split(' ', 2);
                    string[] value2 = _values.Pop().Split(' ', 2);
                    _values.Push($"{int.Parse(value2[1]) % int.Parse(value1[1])}");
                    break;
                }
                case "uminus":
                {
                    string[] value = _values.Pop().Split(' ', 2);
                    if (value[0] == "I")
                    {
                        _values.Push($"I {-int.Parse(value[1])}");
                        break;
                    }
                    _values.Push($"F {-float.Parse(value[1])}");
                    break;
                }
                case "concat":
                {
                    string[] value1 = _values.Pop().Split(' ', 2);
                    string[] value2 = _values.Pop().Split(' ', 2);
                    _values.Push($"S {HandleItem(value2)}{HandleItem(value1)}");
                    break;
                }
                case "and":
                {
                    string value1 = _values.Pop().Split(' ', 2)[1];
                    string value2 = _values.Pop().Split(' ', 2)[1];
                    _values.Push($"B {(bool.Parse(value1) && bool.Parse(value2)).ToString().ToLower()}");
                    break;
                }
                case "or":
                {
                    string value1 = _values.Pop().Split(' ', 2)[1];
                    string value2 = _values.Pop().Split(' ', 2)[1];
                    _values.Push($"B {(bool.Parse(value1) || bool.Parse(value2)).ToString().ToLower()}");
                    break;
                }
                case "gt":
                {
                    string[] value1 = _values.Pop().Split(' ', 2);
                    string[] value2 = _values.Pop().Split(' ', 2);
                    if (value1[0] == "I")
                    {
                        _values.Push($"B {(int.Parse(value2[1]) > int.Parse(value1[1])).ToString().ToLower()}");
                        break;
                    }
                    _values.Push($"B {(float.Parse(value2[1]) > float.Parse(value1[1])).ToString().ToLower()}");
                    break;
                }
                case "lt":
                {
                    string[] value1 = _values.Pop().Split(' ', 2);
                    string[] value2 = _values.Pop().Split(' ', 2);
                    if (value1[0] == "I")
                    {
                        _values.Push($"B {(int.Parse(value2[1]) < int.Parse(value1[1])).ToString().ToLower()}");
                        break;
                    }
                    _values.Push($"B {(float.Parse(value2[1]) < float.Parse(value1[1])).ToString().ToLower()}");
                    break;
                }
                case "eq":
                {
                    string[] value1 = _values.Pop().Split(' ', 2);
                    string[] value2 = _values.Pop().Split(' ', 2);
                    if (value1[0] == "I")
                        _values.Push($"B {(int.Parse(value2[1]) == int.Parse(value1[1])).ToString().ToLower()}");
                    else if (value1[0] == "F")
                        _values.Push($"B {(float.Parse(value2[1]) == float.Parse(value1[1])).ToString().ToLower()}");
                    else if (value1[0] == "S")
                        _values.Push($"B {(HandleItem(value2) == HandleItem(value1)).ToString().ToLower()}");
                    else if (value1[0] == "B")
                        _values.Push($"B {(bool.Parse(value2[1]) == bool.Parse(value1[1])).ToString().ToLower()}");
                    break;
                }
                case "not":
                {
                    string value1 = _values.Pop().Split(' ', 2)[1];
                    _values.Push($"B {(!bool.Parse(value1)).ToString().ToLower()}");
                    break;
                }
                case "itof":
                {
                    string value1 = _values.Pop().Split(' ', 2)[1];
                    _values.Push($"F {float.Parse(value1)}");
                    break;
                }
                case "push":
                {
                    _values.Push(instruction[1]);
                    break;
                }
                case "pop":
                {
                    _values.Pop();
                    break;
                }
                case "load":
                {
                    _values.Push($"{GetCharFromType(_variables[instruction[1]].VarType)} {_variables[instruction[1]].Value}");
                    break;
                }
                case "save":
                {
                    string[] poppedValue = _values.Pop().Split(' ', 2);
                    if (!_variables.TryGetValue(instruction[1], out Variable value))
                    {
                        string val = poppedValue[1];
                        if (GetType(poppedValue[0]) == VarType.FLOAT)
                            val = val.Contains('.') ? val : $"{val}.0";
                        _variables.Add(instruction[1], new(GetType(poppedValue[0]), val));
                        break;
                    }
                    value.Value = poppedValue[1];
                    break;
                }
                case "label": 
                { 
                    break;
                }
                case "jmp":
                {
                    _instructionPointer = _instructions.IndexOf($"label {instruction[1]}");
                    break;
                }
                case "fjmp":
                {
                    if (!bool.Parse(_values.Pop().Split(" ", 2)[1]))
                    {
                        _instructionPointer = _instructions.IndexOf($"label {instruction[1]}");
                    }
                    break;
                }
                case "print":
                {
                    int toPop = int.Parse(instruction[1]);
                    List<string> poppedItems = [];
                    StringBuilder toPrint = new();
                    for (int i = 0; i < toPop; i++)
                        poppedItems.Add(_values.Pop());
                    poppedItems.Reverse();

                    for (int i = 0; i < poppedItems.Count; i++)
                    {
                        if (i == toPop - 1)
                        {
                            toPrint.Append(HandleItem(poppedItems[i].Split(' ', 2))).AppendLine();
                            continue;
                        }
                        toPrint.Append(HandleItem(poppedItems[i].Split(' ', 2)));
                    }
                    Console.Write($"\u001b[90m{nameof(Interpreter)}\u001b[0m>> {toPrint}");
                    break;
                }
                case "read":
                {
                    bool isCorrect = true;
                    Console.WriteLine($"\u001b[96m{nameof(Interpreter)}\u001b[0m>> Provide a value of type {GetType(instruction[1])}");
                    do
                    {
                        if (!isCorrect)
                            Console.WriteLine($"\u001b[91m{nameof(Interpreter)}\u001b[0m>> Provided wrong value!!!");
                        Console.Write($"\u001b[93m{nameof(Interpreter)}\u001b[0m>> ");
                        string? userInput = Console.ReadLine();
                        ArgumentNullException.ThrowIfNull(userInput);
                        if (instruction[1] == "I")
                        {
                            isCorrect = int.TryParse(userInput, out int intValue);
                            if (isCorrect)
                                _values.Push($"I {intValue}");
                        }
                        else if (instruction[1] == "F")
                        {
                            isCorrect = float.TryParse(userInput, out float floatValue);
                            if (isCorrect)
                                _values.Push($"F {floatValue}");
                        }
                        else if (instruction[1] == "B")
                        {
                            isCorrect = bool.TryParse(userInput, out bool boolValue);
                            if (isCorrect)
                                _values.Push($"B {boolValue.ToString().ToLower()}");
                        }
                        else if (instruction[1] == "S")
                        {
                            _values.Push($"S {userInput}");
                        }
                    } while (!isCorrect);
                    break;
                }

            }
            _instructionPointer++; 
        }
        

    }

    private static string HandleItem(string[] item) => item[0] switch
    {
        "S" => item[1].TrimStart('\"').TrimEnd('\"'),
        "I" or "F" or "B" => item[1],
        _ => item[0]
    };

    private static VarType GetType(string type) => type switch
    {
        "S" => VarType.STRING,
        "I" => VarType.INT,
        "F" => VarType.FLOAT,
        "B" => VarType.BOOL
    };

    private static char GetCharFromType(VarType varType) => varType switch
    {
        VarType.INT => 'I',
        VarType.FLOAT => 'F',
        VarType.STRING => 'S',
        VarType.BOOL => 'B'
    };
}
