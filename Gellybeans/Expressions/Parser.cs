﻿using Gellybeans.Pathfinder;
using System.Text;
using System.Text.RegularExpressions;

namespace Gellybeans.Expressions
{

    public class Parser
    {
        public const int MAX_DEPTH = 10000;

        readonly List<Token> tokens;
        readonly IContext ctx;       
        readonly StringBuilder sb;

        int index;

        readonly object caller;

        //dice expression. 0-3 number(s) => d => 1-x number(s) => 0-x instances of ('r' or 'h' or 'l' paired with 1-3 number(s))
        static readonly Regex dRegex = new(@"^([0-9]{0,3})d([0-9]{1,3})((?:r|h|l)(?:[0-9]{1,3})){0,2}$", RegexOptions.Compiled);
   
        public static readonly Regex validVarName = new(@"^[^0-9][^\[\]<>(){}^@:+*/%=!&|;$#?\-.'""]*$", RegexOptions.Compiled);

        Token Current { get { return tokens[index]; } }

        public Parser(List<Token> tokens, object caller, StringBuilder sb, IContext ctx, int index = 0)
        {
            this.tokens = tokens;
            this.caller = caller;
            this.ctx = ctx;
            this.index = index;
            this.sb = sb;
        }

        Token Look(int i = 1)
        {
            if((index + i) < tokens.Count - 1 && (index + 1) > 0)
            {
                return tokens[index + i];
            }
               
            return Current;
        }

        void Move(int count) =>
            index = Math.Clamp(index + count, 0, tokens.Count - 1);

        Token Next()
        {
            if(index < tokens.Count - 1)
            {
                index++;
            }
               
            return Current;
        }

        public ExpressionNode ParseExpr()
        {
            var expr = ParseTermination();                   

            if(Current.TokenType == TokenType.EOF || expr is ErrorNode)
            {
                return expr;
            }
 
            return new ErrorNode($"Invalid expression. Error on token : {(index > 1 && index < tokens.Count - 3 ? $"{Look(-2).Value}{Look(-1).Value}`{Current.Value}`{Look(1).Value}{Look(2).Value}": Current.Value)}");
        }

        ExpressionNode ParseTermination()
        {
            var expr = ParseLoop();             

            if(Current.TokenType == TokenType.Semicolon || Current.TokenType == TokenType.CloseSquig)
            {
                Next();
                if(Current.TokenType == TokenType.EOF)
                {
                    return expr;
                }
                    

                return new MultiExpressionNode(expr, ParseTermination());
            }
            else if(Current.TokenType == TokenType.Pipe)
            {
                Next();
                return new PipeNode(expr, ParseTermination());
            }
            
            return expr;
        }

        ExpressionNode ParseLoop()
        {
            if(Current.TokenType == TokenType.For)
            {
                Next();
                var itr = ParseLeaf();
                if(itr is not VarNode)
                {
                    return new ErrorNode($"Invalid variable name for iterable.");
                }                  

                if(Current.TokenType == TokenType.Separator)
                {
                    Next();
                    var enumerable = ParseLogicalAndOr();
                    if(Current.TokenType == TokenType.Separator)
                    {
                        Next();
                        if(Current.TokenType == TokenType.OpenSquig)
                        {
                            var statement = ParseStatement();
                            if(Current.TokenType != TokenType.CloseSquig)
                            {
                                return new ErrorNode("Expected `}`");
                            }                               
                            return new ForNode((VarNode)itr, enumerable, statement);
                        }
                    }
                }
                return new ErrorNode("Invalid loop.");
            }
            return ParseAssignment();
        }

        ExpressionNode ParseAssignment()
        {
            var node = ParseConditional();
            if(Current.TokenType == TokenType.Assign)
            {                
                if(node is VarNode varNode)
                {
                    Func<string, dynamic, IContext, dynamic> op = null!;

                    switch(Current.Value)
                    {
                        case "=":
                            op = (identifier, assignment, ctx) =>
                            {
                                if(ctx.TryGetVar(identifier, out var var))
                                {
                                    if(var is Stat stat)
                                    {
                                        if(assignment is Stat i)
                                        {
                                            stat.Base = i;
                                            ctx[identifier] = stat;
                                            return ctx[identifier];
                                        }
                                    }
                                    
                                    ctx[identifier] = assignment;
                                    return ctx[identifier];
                                }
                                else
                                {
                                    ctx[identifier] = assignment;
                                    return ctx[identifier];
                                }
                            };
                            break;
                        case "+=":
                            op = (identifier, assignment, ctx) =>
                            {
                                if(ctx.TryGetVar(identifier, out var var))
                                {
                                    ctx[identifier] = var + assignment;
                                    return ctx[identifier];
                                }
                                return new StringValue($"{identifier} not found");
                            };                                                       
                            break;
                        case "-=":
                            op = (identifier, assignment, ctx) =>
                            {
                                if(ctx.TryGetVar(identifier, out var var))
                                {
                                    ctx[identifier] = var - assignment;
                                    return ctx[identifier];
                                }
                                return new StringValue($"{identifier} not found");
                            };
                            break;
                        case "*=":
                            op = (identifier, assignment, ctx) =>
                            {
                                if(ctx.TryGetVar(identifier, out var var))
                                {
                                    ctx[identifier] = var * assignment;
                                    return ctx[identifier];
                                }
                                return new StringValue($"{identifier} not found");
                            };
                            break;
                        case "/=":
                            op = (identifier, assignment, ctx) =>
                            {
                                if(ctx.TryGetVar(identifier, out var var))
                                {
                                    ctx[identifier] = var / assignment;
                                    return ctx[identifier];
                                }
                                return new StringValue($"{identifier} not found");
                            };
                            break;
                        case "%=":
                            op = (identifier, assignment, ctx) =>
                            {
                                if(ctx.TryGetVar(identifier, out var var))
                                {
                                    ctx[identifier] = var % assignment;
                                    return ctx[identifier];
                                }
                                return new StringValue($"{identifier} not found");
                            };
                            break;
                        case "^=":
                            op = (identifier, assignment, ctx) =>
                            {
                                if(ctx.TryElevateVar(identifier, assignment))
                                    return assignment;
                                
                                else
                                    return "No global found for var assignment.";
                            };
                            break;
                    }

                    if(op == null) return node;

                    Next();
                    var rhs = ParseConditional();
                    return new AssignVarNode(varNode.VarName.ToUpper(), rhs, op);
                }
                               
                if(node is KeyNode k)
                {
                    var opValue = Current.Value;
                    
                    Next();
                    var rhs = ParseConditional();
                    return new AssignKeyNode(k, rhs, opValue);                   
                }                                
            }                  
            return node;
        }

        ExpressionNode ParseConditional()
        {           
            var conditional = ParseLogicalAndOr();
            
            if(Current.TokenType == TokenType.If)
            {
                var conditions = new List<ConditionalNode>();

                Next();
                if(Current.TokenType == TokenType.OpenSquig)
                {
                    var statement = ParseStatement();
                    if(Current.TokenType != TokenType.CloseSquig)
                    {
                        return new ErrorNode("Expected `}`");
                    }                       
                    conditions.Add(new ConditionalNode(conditional, statement));
                }
                    
               
                while(true)
                {
                    Next();
                    if(Current.TokenType == TokenType.Separator)
                    {
                        Next();
                        conditional = null!;
                        if(Current.TokenType != TokenType.OpenSquig)
                        {
                            conditional = ParseLogicalAndOr();
                        }                            

                        if(Current.TokenType == TokenType.OpenSquig)
                        {
                            var statement = ParseStatement();
                            if(Current.TokenType != TokenType.CloseSquig)
                            {
                                return new ErrorNode("Expected `}`");
                            }
                               

                            conditions.Add(new ConditionalNode(conditional!, statement));
                        }
                    }
                    else
                        break;
                }

                return new IfNode(conditions);                                    
            }

            while(true)
            {
                Func<dynamic, dynamic, dynamic, dynamic> op = null!;

                if(Current.TokenType == TokenType.Ternary) op = (a, b, c) => a ? b : c;

                if(op == null) return conditional;

                Next();
                var lhs = ParseLogicalAndOr();
                if(Current.TokenType == TokenType.Separator)
                {
                    Next();
                    var rhs = ParseConditional();
                    conditional = new TernaryNode(conditional, lhs, rhs, op);
                }
            }
        }

        ExpressionNode ParseLogicalAndOr()
        {
            var lhs = ParseBitwiseAndOr();

            while(true)
            {
                Func<dynamic, dynamic, dynamic> op = null!;

                if(Current.TokenType == TokenType.LogicalOr) op = (a, b) => (a || b);
                else if(Current.TokenType == TokenType.LogicalAnd) op = (a, b) => (a && b);
                
                if(op == null) return lhs;

                Next();
                var rhs = ParseBitwiseAndOr();
                lhs = new BinaryNode(lhs, rhs, op);
            }
        }

        ExpressionNode ParseBitwiseAndOr()
        {
            var lhs = ParseEquals();

            while(true)
            {
                Func<dynamic, dynamic, dynamic> op = null!;
                
                if(Current.TokenType == TokenType.GetBonus) 
                { 
                    op = (identifier, type) =>
                    { 
                        if(ctx.TryGetVar(identifier.ToString(), out dynamic var))
                        {
                            if(var is Stat s)
                            {
                                return s.GetBonus((BonusType)(int)type);
                            }                            
                            else
                            {
                                return new ErrorNode($"$? cannot be applied to {identifier}");
                            }                              
                        }
                        else
                        {
                            return new ErrorNode($"{identifier} not found.");
                        }                           
                    };                
                }

                if(op == null) return lhs;

                Next();
                var rhs = ParseEquals();
                lhs = new BinaryNode(lhs, rhs, op);
            }
        }

        ExpressionNode ParseEquals()
        {
            var lhs = ParseGreaterLess();

            while(true)
            {
                Func<dynamic, dynamic, dynamic> op = null!;

                if(Current.TokenType == TokenType.Equals) op = (a, b) => a == b;
                else if(Current.TokenType == TokenType.NotEquals) op = (a, b) => a != b;
                else if(Current.TokenType == TokenType.HasFlag) op = (a, b) => (a & b) != 0;
                else if(Current.TokenType == TokenType.Range) op = (lhs, rhs) => new RangeValue(lhs, rhs);
                else if(Current.TokenType == TokenType.GetBonus) op = (lhs, rhs) =>
                {
                    var value = 0;
                    if(lhs is Stat s && rhs is int i)
                    {
                        value = s.GetBonus((BonusType)i);
                    }
                    return value;
                };

                if(op == null) return lhs;

                Next();
                var rhs = ParseGreaterLess();
                lhs = new BinaryNode(lhs, rhs, op);
            }
        }

        ExpressionNode ParseGreaterLess()
        {
            var lhs = ParseShift();

            while(true)
            {
                Func<dynamic, dynamic, dynamic> op = null!;

                if(Current.TokenType == TokenType.Greater)              op = (a, b) => a > b;           
                else if(Current.TokenType == TokenType.GreaterEquals)   op = (a, b) => a >= b;
                else if(Current.TokenType == TokenType.Less)            op = (a, b) => a < b;
                else if(Current.TokenType == TokenType.LessEquals)      op = (a, b) => a <= b;

                if(op == null) return lhs;

                Next();
                var rhs = ParseShift();
                lhs = new BinaryNode(lhs, rhs, op);
            }
        }

        ExpressionNode ParseShift()
        {
            var lhs = ParseAddSub();

            while(true)
            {                            
                Func<dynamic, ExpressionNode, dynamic> op = null!;

                if(Current.TokenType == TokenType.Push) op = (lhs, rhs) =>
                {
                    var rhValue = rhs.Eval(0, caller: this, sb: null!, ctx: ctx);

                    var lhValue = lhs;

                    if(lhValue is ArrayValue a)
                    {
                        dynamic[] array = new dynamic[a.Values.Length];
                        a.Values.CopyTo(array, 0);
                        if(rhValue is FunctionValue f)
                        {
                            if(f.VarNames.Length == 2)
                            {
                                for(int i = 0; i < array.Length; i++)
                                {
                                    array[i] = f.Invoke(0, this, new dynamic[] { array[i], i }, sb, ctx);
                                }
                                    

                            }
                            else
                            {
                                return new StringValue("Expected a function with 2 parameters.");
                            }                               
                        }
                        else
                        {
                            for(int i = 0; i < array.Length; i++)
                                array[i] = rhs.Eval(0, caller: this, sb: sb, ctx: ctx);
                        }

                        return new ArrayValue(array, a.Keys);
                    }
                    return new StringValue("No suitable value found for `<<` operator.");
                };
                else if(Current.TokenType == TokenType.Pull) op = (lhs, rhs) =>
                {
                    var list = new List<dynamic>();
                    var rhValue = rhs.Eval(0, caller: this, sb: sb, ctx: ctx);

                    if(lhs is ArrayValue a)
                    {
                        dynamic[] array = new dynamic[a.Values.Length];
                        a.Values.CopyTo(array, 0);
                        if(rhValue is FunctionValue f)
                        {
                            if(f.VarNames.Length == 2)
                            {

                                for(int i = 0; i < array.Length; i++)
                                {
                                    if(f.Invoke(0, caller, new dynamic[] { array[i], i }, sb, ctx))
                                    {
                                        list.Add(array[i]);
                                    }                                       
                                }

                            }
                            else
                                return new StringValue("Expected a function with 2 parameters.");
                        }
                        else
                        {
                            for(int i = 0; i < array.Length; i++)
                            {
                                if(array[i] == rhValue)
                                {
                                    list.Add(array[i]);
                                }
                            }                                                               
                        }
                        
                        return new ArrayValue(list.ToArray(), a.Keys);
                    }
                    return new StringValue("No suitable value found for `>>` operator.");
                };
                else if(Current.TokenType == TokenType.Arrange) op = (lhs, rhs) =>
                {
                    var rhValue = rhs.Eval(0, caller: this, sb: sb, ctx: ctx);
                    if(lhs is ArrayValue a)
                    {
                        if(rhValue > 0)
                        {
                            Array.Sort(a.Values, (x, y) =>
                            {
                                if(x is KeyValuePairValue kx)
                                {
                                    x = kx.Value;
                                }
                                    
                                if(y is KeyValuePairValue ky)
                                {
                                    y = ky.Value;
                                }                                   
                                return x.CompareTo(y);
                            });
                        }
                        else if(rhValue < 0)
                        {
                            Array.Sort(a.Values, (x, y) =>
                            {
                                if(x is KeyValuePairValue kx)
                                {
                                    x = kx.Value;
                                }
                                   
                                if(y is KeyValuePairValue ky)
                                {
                                    y = ky.Value;
                                }                             

                                return y.CompareTo(x);
                            });
                        }
                        return a;
                    }
                    return "No appropriate value found for this operator.";
                };
                else if(Current.TokenType == TokenType.Append) op = (lhs, rhe) =>
                {
                    var rhs = rhe.Eval(0, this, sb, ctx);

                    dynamic[] array = null!;
                    if(lhs is ArrayValue al)
                    {
                        
                        if(rhs is ArrayValue ar)
                        {
                            array = new dynamic[al.Values.Length + ar.Values.Length];
                            al.Values.CopyTo(array, 0);                           
                            ar.Values.CopyTo(array, al.Values.Length);

                            if(al.Keys != null)
                            {
                                if(ar.Keys != null)
                                {
                                    foreach(var kvp in ar.Keys)
                                    {
                                        al.Keys.TryAdd(kvp.Key, kvp.Value);
                                    }                                                                   
                                }
                                return new ArrayValue(array, al.Keys);
                            }
                            else if(ar.Keys != null)
                            {
                                return new ArrayValue(array, ar.Keys);
                            }
                                
                            else
                            {
                                return new ArrayValue(array);
                            }                              
                        }
                        else
                        {
                            array = new dynamic[al.Values.Length + 1];
                            al.Values.CopyTo(array, 0);

                            if(rhs is KeyValuePairValue kvp)
                            {
                                array[^1] = kvp.Value;
                                var av = new ArrayValue(array, al.Keys);
                                av.AddKey(kvp.Key, array.Length - 1);
                                return av;
                            }
                           
                            array[^1] = rhs;
                            return new ArrayValue(array, al.Keys);
                        }
                        
                    }
                    else if(rhs is ArrayValue ar)
                    {
                        array = new dynamic[ar.Values.Length + 1];
                        ar.Values.CopyTo(array, 1);

                        if(lhs is KeyValuePairValue kvp)
                        {
                            array[0] = kvp.Value;
                            var av = new ArrayValue(array, ar.Keys);
                            av.AddKey(kvp.Key, array.Length - 1);
                            return av;
                        }
                  
                        array[0] = lhs;
                        return new ArrayValue(array, ar.Keys);
                    }                   
                    else
                    {
                        array = new dynamic[2];
                        var dict = new Dictionary<string, int>();
                        if(lhs is KeyValuePairValue kvl)
                        {
                            array[0] = kvl.Value;
                            dict.Add(kvl.Key, 0);
                        }
                        else
                            array[0] = lhs;

                        if(rhs is KeyValuePairValue kvr)
                        {
                            array[1] = kvr.Value;
                            dict.Add(kvr.Key, 1);
                        }
                        else
                            array[1] = rhs;
                        return new ArrayValue(array, dict);
                    }
                };
                else if(Current.TokenType == TokenType.Insert) op = (lhs, rhe) =>
                {
                    var rhs = rhe.Eval(0, this, sb, ctx);

                    if(lhs is ArrayValue al)
                    {                                                                      
                        var array = new dynamic[al.Values.Length + 1];
                        al.Values.CopyTo(array, 0);
                        
                        if(rhs is KeyValuePairValue kvp)
                        {
                            array[^1] = kvp.Value;
                            var av = new ArrayValue(array, al.Keys);
                            av.AddKey(kvp.Key, array.Length - 1);
                            return av;
                        }
                        else
                        {
                            array[^1] = rhs;
                            return new ArrayValue(array, al.Keys);
                        }                        
                    }
                    return "No suitable values found for `>>*` operator.";
                };

                if(op == null) return lhs;
                
                Next();
                var rhs = ParseAddSub();
                lhs = new ShiftNode(lhs, rhs, op);
            }
        }

        ExpressionNode ParseAddSub()
        {
            var lhs = ParseMulDivMod();

            while(true)
            {
                Func<dynamic, dynamic, dynamic> op = null!;

                if(Current.TokenType == TokenType.Add) op = (a, b) => a + b;               
                else if(Current.TokenType == TokenType.Sub) op = (a, b) => a - b;               

                if(op == null) return lhs;

                Next();
                var rhs = ParseMulDivMod();
                lhs = new BinaryNode(lhs, rhs, op);
            }
        }

        ExpressionNode ParseMulDivMod()
        {
            var lhs = ParseKeyValue ();

            while(true)
            {
                Func<dynamic, dynamic, dynamic> op = null!;

                if(Current.TokenType == TokenType.Mul) op = (a, b) => a * b;
                else if(Current.TokenType == TokenType.Div) op = (a, b) => a / b;
                else if(Current.TokenType == TokenType.Percent) op = (a, b) => a % b;

                if(op == null) return lhs;

                Next();
                var rhs = ParseKeyValue();
                lhs = new BinaryNode(lhs, rhs, op);
            }
        }
      
        ExpressionNode ParseKeyValue()
        {
            var lhs = ParseUnary();
         
            if(Current.TokenType == TokenType.Pair)
            {
                static dynamic op(dynamic k, dynamic v) => new KeyValuePairValue(k, v);

                Next();
                var value = ParseUnary();
                return new KeyValueNode(lhs, value, op);
            }
            return lhs;
        }

        ExpressionNode ParseUnary()
        {
            //prefix
            while(true)
            {
                Func<dynamic, dynamic> op = null!;

                if(Current.TokenType == TokenType.Add) Next();
                else if(Current.TokenType == TokenType.Sub)
                {
                    op = (a) =>
                    {
                        if(a is Bonus b)
                        {
                            var count = 0;
                            var stats = ctx.Vars.Values.OfType<Stat>();
                            foreach(Stat s in stats)
                            {
                                if(s.RemoveBonus(b))
                                    count++;
                            }
                            return new StringValue($"{b.Name} removed from {count} stats.");
                        }
                        return -a;
                    };
                }
                else if(Current.TokenType == TokenType.Not) op = (value) => !value;
                else if(Current.TokenType == TokenType.Base)
                {
                    op = (var) =>
                    {
                        if(var is Stat s)
                        {
                            return s.Base;
                        }                           
                        else
                        {
                            return new StringValue("@ cannot be applied to this value.");
                        }
                            
                    };
                }
                else if(Current.TokenType == TokenType.Remove)
                {
                    if(Look().TokenType == TokenType.Var)
                    {
                        var varName = Look().Value.ToUpper();
                        op = (value) =>
                        {
                            if(ctx.RemoveVar(varName))
                            {
                                return new StringValue($"{varName} removed.");
                            }
                              
                            return new StringValue($"{varName} not found.");
                        };
                    }
                }
                else if(Current.TokenType == TokenType.And)
                {
                    op = (value) =>
                    {
                        var d = new DiceNode(1, 20).Eval(0, caller: this, sb: sb, ctx: ctx);
                        return d + value;
                    };
                }
                else if(Current.TokenType == TokenType.Percent) op = (value) =>
                {
                    return value is IString s ? s.ToStr() : value is StringValue str ? str.Display(0, caller, sb, ctx) : value.ToString();
                };
                else if(Current.TokenType == TokenType.ToExpr) op = (value) =>
                {

                    if(value is StringValue s)
                    {
                        value = s.Display(0, this, null!, ctx);
                    }
                        
                    else
                    {
                        value = value.ToString();
                    }
                       
                    return new ExpressionValue(value);
                };

                if(op == null)
                    break;

                Next();
                var rhs = ParseMember();
                return new UnaryNode(rhs, op);
            }
            return ParseMember();
        }

        ExpressionNode ParseMember()
        {
            var lhs = ParseLeaf();

            while(true)
            {
                Func<dynamic, dynamic, dynamic> op = null!;

                if(Current.TokenType == TokenType.OpenSquare)
                {
                    op = (k, v) =>
                    {

                        if(v is ArrayValue a)
                        {
                            if(k is SymbolNode symbol)
                            {
                                if(symbol.Symbol == ".^")
                                {
                                    return a[Random.Shared.Next(0, a.Values.Length)];
                                }
                                    
                                if(symbol.Symbol == "^^")
                                {
                                    return a.Values.Length;
                                }                                 
                            }

                            if(k is StringValue s)
                            {
                                return a[s.String.ToUpper()] is ExpressionValue ev ? ev.Reduce(0, caller, sb, ctx) : a[s.String];
                            }

                            if(k is string str)
                            {
                                return a[str.ToUpper()] is ExpressionValue ev ? ev.Reduce(0, caller, sb, ctx) : a[str];
                            }


                            if(k is RangeValue r)
                            {
                                var start = r.Lower;
                                if(start < 0)
                                {
                                    start = a.Values.Length + start;
                                }
                                   
                                var end = r.Upper;
                                
                                if(end < 0)
                                {
                                    end = a.Values.Length + end;
                                }
                                   

                                if(start < 0 || start >= a.Values.Length || end < 0 || end >= a.Values.Length)
                                {
                                    return new StringValue($"Invalid range `[{r}]` for this array.");
                                }
                                   
                                else
                                {
                                    var list = new List<dynamic>();
                                    if(start > end)
                                    {
                                        for(int i = start; i >= end; i--)
                                            list.Add(a[i] is ExpressionValue ev ? ev.Reduce(0, caller, sb, ctx) : a[i]);
                                    }
                                    else
                                    {
                                        for(int i = start; i <= end; i++)
                                            list.Add(a[i] is ExpressionValue ev ? ev.Reduce(0, caller, sb, ctx) : a[i]);
                                    }
                                    return new ArrayValue(list.ToArray());
                                }
                            }

                            if(k < 0)
                            {
                                k = a.Values.Length + k;
                            }
                               

                            if(k < 0 || k >= a.Values.Length)
                            {
                                return new StringValue($"Index `[{k}]` out of range");
                            }                               
                            return a[k] is ExpressionValue eva ? eva.Reduce(0, caller, sb, ctx) : a[k];
                        }
                        if(k is SymbolNode)
                        {
                            return -1;
                        }                          
                        return new StringValue($"No key found for this value.");
                    };
                }

                if(op == null)
                {
                    break;
                }                 

                Next();
                var key = ParseKey();
                lhs = new KeyNode(key, lhs, op);

            }

            while(true)
            {
                if(Current.TokenType == TokenType.Dot)
                {
                    Next();
                    if(Current.TokenType == TokenType.Var)
                    {
                        var m = Current.Value.ToUpper();
                        
                        Next();
                        if(Current.TokenType == TokenType.OpenPar)
                        {
                            Next();
                            var fargs = new List<ExpressionNode>();

                            if(Current.TokenType != TokenType.ClosePar)
                            {
                                while(true)
                                {
                                    fargs.Add(ParseConditional());
                                    if(Current.TokenType == TokenType.Comma)
                                    {
                                        Next();
                                        continue;
                                    }
                                    break;
                                }
                                if(Current.TokenType != TokenType.ClosePar)
                                {
                                    return new ErrorNode("Expected `)`");
                                }                                   
                            }
                            
                            Next();
                            return new MemberNode(lhs, m, fargs.ToArray());
                        }
                        return new MemberNode(lhs, m);
                    }                       
                }
                else break;
            }

            while(true)
            {
                if(Current.TokenType == TokenType.OpenPar)
                {
                    Next();
                    var fargs = new List<ExpressionNode>();
                    if(Current.TokenType != TokenType.ClosePar)
                    {
                        while(true)
                        {
                            fargs.Add(ParseConditional());
                            if(Current.TokenType == TokenType.Comma)
                            {
                                Next();
                                continue;
                            }
                            break;
                        }
                    }

                    if(Current.TokenType != TokenType.ClosePar)
                    {
                        return new ErrorNode("Expected `)`");
                    }                       

                    Next();
                    return new CallFunctionNode(lhs, fargs);
                }
                else break;
            }

            return lhs;
        }

        ExpressionNode ParseLeaf()
        {
            switch(Current.TokenType)
            {
                case TokenType.OpenPar:
                    Next();
                    var node = ParseAssignment();

                    if(Current.TokenType != TokenType.ClosePar)
                    {
                        return new ErrorNode("%Expected`)`");
                    }                      

                    Next();
                    return node;
                case TokenType.Number:
                    var number = new NumberNode(int.Parse(Current.Value));

                    Next();
                    return number;
                case TokenType.Dice:
                    //^([0-9]{0,3})d([0-9]{1,3})((?:r|h|l)(?:[0-9]{1,3})){0,3}$
                    var match = dRegex.Match(Current.Value);

                    if(match.Success)
                    {
                        var count = match.Groups[1].Captures.Count > 0 ? int.TryParse(match.Groups[1].Captures[0].Value, out int outVal) ? outVal : 1 : 1;
                        var sides = int.Parse(match.Groups[2].Captures[0].Value);
                        var lhs = new DiceNode(count, sides, sb);

                        for(int i = 0; i < match.Groups[3].Captures.Count; i++)
                        {
                            //store the letter in index 0, remove it and parse the number that comes after
                            var letter = match.Groups[3].Captures[i].Value[0];
                            var numb = int.Parse(match.Groups[3].Captures[i].Value.Remove(0, 1));

                            if(letter == 'r')
                            {
                                lhs.Reroll = numb;
                            }
                              
                            if(letter == 'h')
                            {
                                lhs.Highest = numb;
                            }
                               
                            if(letter == 'l')
                            {
                                lhs.Lowest = numb;
                            }                               
                        }

                        Next();
                        if(Current.TokenType == TokenType.Mul || Current.TokenType == TokenType.Div)
                        {
                            var op = Current.TokenType;
                            Next();
                            var rhs = ParseConditional();
                            return new DiceMultiplierNode(lhs, rhs, op);
                        }
                        return lhs;
                    }
                    return new VarNode(Current.Value);
                case TokenType.OpenSquig:
                    var list = new List<ExpressionNode>();

                    Next();
                    if(Current.TokenType == TokenType.CloseSquig)
                    {
                        Next();
                        return new ArrayNode(Array.Empty<ExpressionNode>());
                    }

                    while(true)
                    {
                        list.Add(ParseConditional());
                        if(Current.TokenType == TokenType.Comma)
                        {
                            Next();
                            continue;
                        }
                        break;
                    }

                    if(Current.TokenType != TokenType.CloseSquig)
                    {
                        return new ErrorNode("Expected `}`");
                    }                       

                    Next();
                    return new ArrayNode(list.ToArray());
                case TokenType.Var:
                    var identifier = Current.Value.ToUpper();

                    Next();
                    if(Current.TokenType == TokenType.OpenPar)
                    {
                        Next();
                        if(ctx.TryGetVar(identifier, out var func) && func is FunctionValue)
                        {
                            var fargs = new List<ExpressionNode>();
                            if(Current.TokenType != TokenType.ClosePar)
                            {
                                while(true)
                                {
                                    var a = ParseConditional();
                                    fargs.Add(a);
                                    if(Current.TokenType == TokenType.Comma)
                                    {
                                        Next();
                                        continue;
                                    }
                                    break;
                                }
                            }

                            if(Current.TokenType != TokenType.ClosePar)
                            {
                                return new ErrorNode("Expected `)`");
                            }
                                
                            Next();
                            return new CallFunctionNode(new VarNode(identifier), fargs);
                        }
                        
                        FunctionNode f;
                        if(Current.TokenType == TokenType.ClosePar)
                        {
                            f = new FunctionNode(identifier.ToLower());
                        }
                           
                        else
                        {
                            var fargs = new List<ExpressionNode>();
                            while(true)
                            {
                                fargs.Add(ParseConditional());

                                if(Current.TokenType == TokenType.Comma)
                                {
                                    Next();
                                    continue;
                                }
                                break;
                            }
                            f = new FunctionNode(identifier.ToLower(), fargs.ToArray());
                        }
                        if(Current.TokenType != TokenType.ClosePar)
                        {
                            return new ErrorNode("Expected `)`");
                        }                           

                        Next();
                        return f;
                    }

                    return new VarNode(identifier);
                case TokenType.Dollar:
                    Next();                    
                    
                    var bName = ParseConditional();
                    if(Current.TokenType == TokenType.Separator)
                    {
                        Next();
                        var bType = ParseConditional();
                        if(Current.TokenType == TokenType.Separator)
                        {
                            Next();
                            var bValue = ParseConditional();
                            return new BonusNode(bName, bType, bValue);
                        }
                    }
                    else
                    {
                        return new BonusNode(bName);
                    }
                                           
                    return new ErrorNode("%?");
                case TokenType.String:
                    Next();
                    return new StringNode(Look(-1).Value.Trim('"'));
                case TokenType.Event:
                    Next();
                    var args = ParseLeaf();
                    if(Current.TokenType != TokenType.Event)
                    {
                        return new ErrorNode("Expected `'`");
                    }                      

                    Next();
                    return new EventNode(args);
                case TokenType.Expression:
                    Next();
                    return new StoredExpressionNode(Look(-1).Value.Trim('`'));
                case TokenType.Lambda:
                    Next();
                    if(Current.TokenType == TokenType.OpenPar)
                    {
                        var parameters = ParseParameters();
                        if(Current.TokenType != TokenType.ClosePar)
                        {
                            return new ErrorNode("Expected `)`");
                        }
                           

                        Next();
                        if(Current.TokenType == TokenType.OpenSquig)
                        {
                            var slist = ParseStatement();
                            if(Current.TokenType != TokenType.CloseSquig)
                            {
                                return new ErrorNode("Expected `}`");
                            }
                               
                            Next();
                            return new DefNode(slist, parameters.ToArray());
                        }
                    }

                    else if(Current.TokenType == TokenType.OpenSquig)
                    {
                        var statement = ParseStatement();
                        if(Current.TokenType != TokenType.CloseSquig)
                            return new ErrorNode("Expected `}`");

                        Next();
                        return new DefNode(statement, new string[] { "_", "I" });
                    }
                    return new ErrorNode("Invalid function declaration.");
                case TokenType.Comment:
                    Next();
                    if(Current.TokenType != TokenType.EOF)
                    {
                        return ParseTermination();
                    }                                
                    return new NumberNode(0);               
        }
          
            //SymbolNode
            if(IsSymbol(Current.TokenType))
            {
                Next();
                return new SymbolNode(Look(-1).Value); 
            }

            return new ErrorNode($"Reached token in improper state `{Current.TokenType}:{Current.Value}`.");
        }

        ExpressionNode ParseKey()
        {
            var key = ParseConditional();
            if(Current.TokenType != TokenType.CloseSquare)
            {
                return new ErrorNode("Expected `]`");
            }
                          
            Next();
            return key;
        }

        List<string> ParseParameters()
        {
            Next();
            var parameters = new List<string>();
            if(Current.TokenType != TokenType.ClosePar)
            {
                while(true)
                {
                    var result = ParseConditional();
                    if(result is VarNode v)
                    {
                        parameters.Add(v.VarName);
                    }                   
                    else
                    {
                        return null!;
                    }                      

                    if(Current.TokenType == TokenType.Comma)
                    {
                        Next();
                        continue;
                    }
                    break;
                }
            }
            return parameters;
        }
        
        List<Token> ParseStatement()
        {
            var statement  = new List<Token>();

            int i = 1;

            while(i != 0)
            {
                Next();                          
                if(Current.TokenType == TokenType.OpenSquig)
                    i++;
                if(Current.TokenType == TokenType.CloseSquig)
                {
                    i--;
                    if(i == 0)
                        break;
                }                  
                if(Current.TokenType == TokenType.EOF) 
                    break;
                statement.Add(Current);
            }
        
            statement.Add(new Token(TokenType.EOF, ""));
            return statement;
        }


        bool IsSymbol(TokenType tokenType)
        {
            return (tokenType == TokenType.Self || tokenType == TokenType.Random || tokenType == TokenType.Return);
        }

        public static ExpressionNode Parse(string expr, object caller, StringBuilder sb = null!, IContext ctx = null!, int index = 0) =>
            Parse(new Tokenizer(new StringReader(expr)).Tokens, caller, sb, ctx, index);
       
        public static ExpressionNode Parse(List<Token> tokens, object caller, StringBuilder sb = null!, IContext ctx = null!, int index = 0)
        {
            var parser = new Parser(tokens, caller, sb, ctx, index);
            return parser.ParseExpr();
        }       
    }                 
}   

