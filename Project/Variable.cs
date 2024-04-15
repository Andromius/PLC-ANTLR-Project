using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project;
public class Variable
{
    public Variable(VarType varType, object value)
    {
        VarType = varType;
        Value = value;
    }

    public VarType VarType { get; set; }
    public object Value { get; set; }
}
