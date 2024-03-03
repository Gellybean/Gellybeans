﻿using System.Text;

namespace Gellybeans.Expressions
{
    /// <summary>
    /// Expression Nodes determine how a particular group of values are calculated. 
    /// 
    /// These nodes are typically generated by the Parser, which creates a tree of nodes based on a given expression
    /// </summary>
    
    public abstract class ExpressionNode 
    {
        public abstract ValueNode Eval(IContext ctx = null!, StringBuilder sb = null!);
    }    
}


