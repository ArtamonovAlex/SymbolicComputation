﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using SymbolicComputation.Model;

namespace SymbolicComputation
{
	class Program
	{
		private static void Main(string[] args)
		{
			string filepath = "../../../input.json";

			using (StreamReader sr = new StreamReader(filepath))
			{
				Expression asd = (Expression)(Parser.ParseInput(sr.ReadToEnd()));
				Console.WriteLine($"Got expression: {asd}");
				Console.WriteLine(asd.Evaluate());
			}

			Symbol Sum = new StringSymbol("Sum");
			Symbol Mul = new StringSymbol("Mul");
			Symbol List = new StringSymbol("List");
			Symbol Set = new StringSymbol("Set");
			Symbol Pow = new StringSymbol("Pow");
			Symbol Delayed = new StringSymbol("Delayed");
			Symbol Equal = new StringSymbol("Equal");
			Symbol Or = new StringSymbol("Or");
			Symbol And = new StringSymbol("And");
			Symbol Xor = new StringSymbol("Xor");
			Symbol Not = new StringSymbol("Not");
            Symbol Greater = new StringSymbol("Greater");
            Symbol GreaterOrEqual = new StringSymbol("GreaterOrEqual");
            Symbol Less = new StringSymbol("Less");
            Symbol LessOrEqual = new StringSymbol("LessOrEqual");
            Symbol If = new StringSymbol("If");
			Expression p1Func = Sum["t", 1];

			Symbol P1 = new StringSymbol("P1");

			Expression delExp = Delayed["P1", "t", p1Func];
			Expression testDelExp = List[delExp, P1[2]];

			Expression exp1 = If[And["True","False"], Mul[2,2], Pow[2,3]];
			Expression exp2 = Mul[5, 2];
			Expression exp4 = Sum[5, 2];
			Expression exp3 = List[Set["y", 10], Mul["x", Sum["y", 1]]];

			Expression setExp = Set["x", 2];
			Expression setExp2 = Set["y", Mul["x", 4]];

			Console.WriteLine(exp1.Evaluate().ToString());


            Expression testExpression = Greater[4,5];
			Console.WriteLine(testExpression.Evaluate());
        }
	}
}