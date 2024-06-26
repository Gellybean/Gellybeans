﻿using System.Text;

namespace Gellybeans.Expressions
{
    public class AssignVarNode : ExpressionNode
    {
        readonly string identifier;
        readonly ExpressionNode assignment;

        readonly Func<string, dynamic, IContext, dynamic> op;

        public AssignVarNode(string identifier, dynamic assignment, Func<string, dynamic, IContext, dynamic> op)
        {
            this.identifier = identifier;
            this.assignment = assignment;
            this.op = op;
        }

        public override dynamic Eval(int depth, object caller, StringBuilder sb, IContext ctx = null!)
        {
            depth++;
            if(depth > Parser.MAX_DEPTH)
                return "operation cancelled: maximum evaluation depth reached.";

            return op(identifier, assignment.Eval(depth: depth, caller: this, sb: sb, ctx : ctx), ctx);
        }
            

    }
}
