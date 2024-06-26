﻿using System.Text;

namespace Gellybeans.Expressions
{
    public class FunctionValue
    {
        public string[] VarNames {  get; set; }
        public string Expression { get; set; }

        public FunctionValue(string[] varNames, string expression)
        {
            VarNames = varNames;
            Expression = expression;
        }

        public dynamic Invoke(int depth, object caller, dynamic[] args, StringBuilder sb, IContext ctx)
        {
            depth++;
            if(depth > Parser.MAX_DEPTH)
                return "operation cancelled: maximum evaluation depth reached.";


            if(args.Length != VarNames.Length)
                return "Arguments don't match parameter count for this function.";


            var dict = new Dictionary<string, dynamic>();
            for(int i = 0; i < args.Length; i++)
            {
                dict.Add(VarNames[i].ToUpper(), args[i] is VarNode v ? v.Reduce(depth, caller, sb, ctx) : args[i]);
            }
          

            var scope = new ScopedContext(ctx, dict, true);
            var result = Parser.Parse(Expression, caller, sb, scope).Eval(depth: depth, caller: this, sb: sb, ctx: scope);

            //synchronize any vars back to outer scope
            for(int i = 0; i < args.Length; i++)
                if(args[i] is VarNode v)
                {
                    Console.WriteLine($"SETTING BACK{v.VarName}");
                    ctx[v.VarName] = scope[VarNames[i].ToUpper()];
                }

            return result;
        }     

        public override string ToString()
        {
            var sb = new StringBuilder();
            var exprs = Expression.Split(new char[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
            for(int i = 0; i < exprs.Length; i++)
            {
                if(i == 0) sb.AppendLine(exprs[i]);
                else sb.AppendLine(exprs[i].Trim());
            }
            return $"### **Function**\n>>> ### ({GetParamNames()})\n```{sb}```";
        }
            
        
        public string GetParamNames()
        {
            string s = "";
            for(int i = 0; i < VarNames.Length; i++)
                s += $"{VarNames[i]},";
            return s.Trim(',');
        }
    }
}
