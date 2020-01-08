﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using SymbolicComputation.Model;
using Expression = SymbolicComputation.Model.Expression;

namespace SymbolicComputation
{
    public static class BuildInFunctions
    {
        private static Scope context = new Scope();

        private static Dictionary<string, Func<Expression, Symbol>> functionsDictionary =
            new Dictionary<string, Func<Expression, Symbol>>
            {
                {"Sum", Sum},
                {"Sub", Sub},
                {"Mul", Mul},
                {"Div", Div},
                {"Rem", Rem},
                {"Divide", Divide},
                {"Pow", Pow},
                {"List", List},
                {"Equal", Equal},
                {"Greater", Greater},
                {"GreaterOrEqual", GreaterOrEqual},
                {"Less", Less},
                {"LessOrEqual", LessOrEqual}
            };

        private static Dictionary<string, Tuple<StringSymbol, Expression>> customFunctions =
            new Dictionary<string, Tuple<StringSymbol, Expression>>();

        private static Symbol MathEval(Expression exp, Func<decimal, decimal, decimal> func)
        {
            var arg1 = exp.Args[0];
            var arg2 = exp.Args[1];

            if (arg1 is Constant constant1 && arg2 is Constant constant2)
            {
                return new Constant(func(constant1.Value, constant2.Value));
            }

            return exp;
        }

        private static Symbol LogicEval(Expression exp, Func<decimal, decimal, bool> func)
        {
            var arg1 = exp.Args[0];
            var arg2 = exp.Args[1];
            if (arg1 is Constant constant1 && arg2 is Constant constant2)
            {
                return (func(constant1.Value, constant2.Value)) ? new StringSymbol("True") : new StringSymbol("False");
            }

            return new StringSymbol("False");
        }

        public static Symbol Evaluate(Expression exp)
        {
            Console.WriteLine($"\nEvaluating expression {exp}:");
            if (exp.Action.ToString() == "Set")
            {
                return Set(exp, context);
            }

            if (exp.Action.ToString() == "Delayed")
            {
                return Delayed(exp);
            }

            List<Symbol> newArgs = new List<Symbol>();
            foreach (var arg in exp.Args)
            {
                if (arg is Expression argExpression)
                {
                    newArgs.Add(Evaluate(argExpression));
                }
                else if (arg is StringSymbol symbol)
                {
                    newArgs.Add(Substitute(symbol, context));
                }
                else
                {
                    newArgs.Add(arg);
                }
            }

            Expression newExp = new Expression(exp.Action, newArgs.ToArray());
            Console.WriteLine($"\n  Evaluating expression {newExp}:");
            Symbol result = functionsDictionary[exp.Action.ToString()](newExp);
            Console.WriteLine($"The result of {newExp} is {result}");
            return result;
        }

        private static Symbol Equal(Expression exp)
        {
            return exp.Args[0].Equals(exp.Args[1]) ? new StringSymbol("True") : new StringSymbol("False");
        }

        private static Symbol Greater(Expression exp)
        {
            return LogicEval(exp, (a, b) => a > b);
        }

        private static Symbol GreaterOrEqual(Expression exp)
        {
            return LogicEval(exp, (a, b) => a >= b);
        }

        private static Symbol LessOrEqual(Expression exp)
        {
            return LogicEval(exp, (a, b) => a <= b);
        }

        private static Symbol Less(Expression exp)
        {
            return LogicEval(exp, (a, b) => a < b);
        }

        private static Symbol Sum(Expression exp)
        {
            StringSymbol[] symbols = exp.Args.Select(x => x is StringSymbol symbol ? symbol : null)
                .Where(x => x != null).ToArray();
            decimal sum = exp.Args.Where(x => x is Constant).Aggregate(0m, (acc, x) => acc + ((Constant) x).Value);
            Symbol constant = new Constant(sum);
            if (symbols.Length == 0)
            {
                return constant;
            }

            if (sum == 0)
            {
                return new Expression(exp.Action, symbols);
            }

            Symbol[] args = new Symbol[symbols.Length + 1];
            args[0] = constant;
            Array.Copy(symbols, 0, args, 1, symbols.Length);
            return new Expression(exp.Action, args);
        }

        private static Symbol Sub(Expression exp)
        {
            return MathEval(exp, (a, b) => a - b);
        }

        private static Symbol Mul(Expression exp)
        {
            StringSymbol[] symbols = exp.Args.Select(x => x is StringSymbol symbol ? symbol : null)
                .Where(x => x != null).ToArray();
            decimal sum = exp.Args.Where(x => x is Constant).Aggregate(1m, (acc, x) => acc * ((Constant) x).Value);
            Symbol constant = new Constant(sum);
            if (symbols.Length == 0)
            {
                return constant;
            }

            if (sum == 0)
            {
                return new Expression(exp.Action, symbols);
            }

            Symbol[] args = new Symbol[symbols.Length + 1];
            args[0] = constant;
            Array.Copy(symbols, 0, args, 1, symbols.Length);
            return new Expression(exp.Action, args);
        }

        private static Symbol Divide(Expression exp)
        {
            return MathEval(exp, (a, b) => a / b);
        }

        private static Symbol Div(Expression exp)
        {
            return MathEval(exp, (a, b) => (int) (a / b));
        }

        private static Symbol Rem(Expression exp)
        {
            return MathEval(exp, (a, b) => a % b);
        }

        private static Symbol Pow(Expression exp)
        {
            return MathEval(exp, (a, b) => (decimal) Math.Pow((double) a, (double) b));
        }

        private static Symbol List(Expression exp)
        {
            return exp.Args.Last();
        }

        private static Symbol Set(Expression exp, Scope localContext)
        {
            var arg1 = exp.Args[0];
            var arg2 = exp.Args[1];
            if (arg1 is StringSymbol symbol)
            {
                Symbol newArg;
                if (arg2 is Expression exp2)
                {
                    newArg = Evaluate(exp2);
                }
                else if (arg2 is StringSymbol stringSymbol)
                {
                    newArg = Substitute(stringSymbol, localContext);
                }
                else
                {
                    newArg = arg2;
                }

                localContext.SymbolRules.Add(symbol.Name, newArg);
                Console.WriteLine($"{symbol.Name} is initialized by {arg2}");
                return arg1;
            }

            throw new Exception("First parameter of Set isn't string symbol");
        }

        private static Symbol Substitute(StringSymbol symbol, Scope localContext)
        {
            return localContext.SymbolRules.TryGetValue(symbol.Name, out var resultSymbol) ? resultSymbol : symbol;
        }

        private static Symbol Delayed(Expression exp)
        {
            Symbol name = exp.Args[0];
            StringSymbol variable = (StringSymbol) exp.Args[1];
            Expression function = (Expression) exp.Args[2];
            functionsDictionary.Add(name.ToString(), ComputeDelayed);
            customFunctions.Add(name.ToString(), new Tuple<StringSymbol, Expression>(variable, function));
            Console.WriteLine($"{name}({variable}) is defined as {function}");
            return exp;
        }

        private static Symbol ComputeDelayed(Expression exp)
        {
            string name = exp.Action.ToString();
            var (variable, function) = customFunctions[name];
            Expression newExpression = ReplaceVariable(function, variable, exp.Args[0]);
            return Evaluate(newExpression);
        }

        public static Expression ReplaceVariable(Expression exp, StringSymbol localVariable, Symbol value)
        {
            Console.Write($"Replacing {localVariable} with {value}... ");
            Scope localScope = new Scope();
            localScope.SymbolRules.Add(localVariable.Name, value);
            List<Symbol> newArgs = new List<Symbol>();
            foreach (var arg in exp.Args)
            {
                if (arg is Expression argExp)
                {
                    if (argExp.Action.ToString() == "Set")
                    {
                        newArgs.Add(argExp);
                    }
                    else
                    {
                        newArgs.Add(ReplaceVariable(argExp, localVariable, value));
                    }
                }
                else if (arg is StringSymbol symbol)
                {
                    newArgs.Add(Substitute(symbol, localScope));
                }
                else
                {
                    newArgs.Add(arg);
                }
            }

            Expression newExp = new Expression(exp.Action, newArgs.ToArray());
            Console.WriteLine($"Result: {newExp}");
            return newExp;
        }
    }
}