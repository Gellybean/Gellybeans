﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gellybeans.Expressions
{
    public enum TokenType
    {
        Attack,
        Ternary,
        AssignMod,
        AssignDiv,
        AssignMul,
        AssignSub,
        AssignAdd,
        AssignEquals,
        LogicalOr,
        LogicalAnd,
        BitwiseOr,
        BitwiseXor,
        BitwiseAnd,
        Equals,
        NotEquals,
        Greater,
        GreaterEquals,
        Less,
        LessEquals,       
        Not,
        Add,
        Sub,
        Mul,
        Div,
        Modulo,      
        Number, //integer
        OpenPar,
        ClosePar,
        Comma,
        Var,
        Dice,
        EOF,       
    }
}
