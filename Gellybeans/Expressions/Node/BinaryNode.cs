﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Text;

namespace Gellybeans.Expressions
{
    public class BinaryNode : ExpressionNode
    {
        readonly ExpressionNode lhs;
        readonly ExpressionNode rhs;

        public dynamic LResult { get; private set; } = null!;
        public dynamic RResult { get; private set; } = null!;

        Func<dynamic, dynamic, dynamic> op;

        public BinaryNode(ExpressionNode lhs, ExpressionNode rhs, Func<dynamic, dynamic, dynamic> op)
        {
            this.lhs = lhs;
            this.rhs = rhs;
            this.op = op;
        }

        public override string ToString()
        {
            return $"BINARY: {lhs.GetType()} : {rhs.GetType()}";
        }

        public override dynamic Eval(int depth, object caller, StringBuilder sb, IContext ctx = null!)
        {
            depth++;
            if(depth > Parser.MAX_DEPTH)
                return "Operation cancelled: maximum evaluation depth reached.";

            var lhValue = lhs.Eval(depth, caller, sb, ctx);
            var rhValue = rhs.Eval(depth, caller, sb, ctx);

            LResult = lhValue;
            RResult = rhValue;

            return op(lhValue, rhValue);
        }
    }
}
