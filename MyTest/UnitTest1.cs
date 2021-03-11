using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace MyTest
{
    public interface IExpressionVisitor
    {
        void Visit(Literal expression);
        void Visit(Variable expression);
        void Visit(BinaryExpression expression);
        void Visit(ParenExpression expression);
    }
    
    public interface IExpression
    {
        void Accept(IExpressionVisitor visitor);
    }

    public class Literal : IExpression
    {
        public Literal(string value)
        {
            Value = value;
        }

        public readonly string Value;
        
        public void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class Variable : IExpression
    {
        public Variable(string name)
        {
            Name = name;
        }

        public readonly string Name;
        public void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    public class BinaryExpression : IExpression
    {
        public readonly IExpression FirstOperand;
        public readonly IExpression SecondOperand;
        public readonly string Operator;

        public BinaryExpression(IExpression firstOperand, IExpression secondOperand, string @operator)
        {
            FirstOperand = firstOperand;
            SecondOperand = secondOperand;
            Operator = @operator;
        }

        public void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
    
    public class ParenExpression : IExpression
    {
        public ParenExpression(IExpression operand)
        {
            Operand = operand;
        }

        public readonly IExpression Operand;
        public void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class DumpVisitor : IExpressionVisitor
    {
        private readonly StringBuilder myBuilder;

        public DumpVisitor()
        {
            myBuilder = new StringBuilder();
        }

        public void Visit(Literal expression)
        {
            myBuilder.Append("Literal(" + expression.Value + ")");
        }

        public void Visit(Variable expression)
        {
            myBuilder.Append("Variable(" + expression.Name + ")");
        }

        public void Visit(BinaryExpression expression)
        {
            myBuilder.Append("Binary(");
            expression.FirstOperand.Accept(this);
            myBuilder.Append(expression.Operator);
            expression.SecondOperand.Accept(this);
            myBuilder.Append(")");
        }

        public void Visit(ParenExpression expression)
        {
            myBuilder.Append("Paren(");
            expression.Operand.Accept(this);
            myBuilder.Append(")");
        }

        public override string ToString()
        {
            return myBuilder.ToString();
        }
    }


    public class SimpleParser
    {
        private static Dictionary<char, int> priorTable = new Dictionary<char, int>()
        {
            {'+', 1},
            {'-', 1},
            {'*', 2},
            {'/', 2},
        };

    public IExpression Parse(string text)
        {
            Stack<IExpression> exprStack = new Stack<IExpression>();
            Stack<char> literalStack = new Stack<char>();
            
            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (priorTable.ContainsKey(ch))
                {
                    char lastOper = (literalStack.Count == 0) ? 'e' : literalStack.Peek();
                    if (priorTable.ContainsKey(lastOper))
                    {
                        if (priorTable[ch] > priorTable[lastOper])
                        {
                            literalStack.Push(ch);
                        }
                        else
                        {
                            IExpression right = exprStack.Pop();
                            IExpression left = exprStack.Pop();
                            exprStack.Push(new BinaryExpression(left, right, new String(lastOper,1)));
                            literalStack.Pop();
                            --i;
                        }
                    }
                    else
                    {
                        literalStack.Push(ch);
                    }
                }
                else if (ch == '(')
                {
                    literalStack.Push(ch);
                }
                else if (ch == ')')
                {
                    char lastOper = literalStack.Peek();
                    while (!lastOper.Equals('('))
                    {
                        IExpression right = exprStack.Pop();
                        IExpression left = exprStack.Pop();
                        exprStack.Push(new BinaryExpression(left, right, new String(lastOper,1)));
                        literalStack.Pop();
                        lastOper = literalStack.Peek();
                    }

                    IExpression topExpr = exprStack.Pop();
                    exprStack.Push(new ParenExpression(topExpr));
                    literalStack.Pop();
                }
                else if (char.IsDigit(ch))
                {
                    exprStack.Push(new Literal(new String(ch,1)));
                }
                else if (char.IsLetter(ch))
                {
                    exprStack.Push(new Variable(new String(ch,1)));
                }
            }

            while (literalStack.Count > 0 && exprStack.Count > 1)
            {
                IExpression right = exprStack.Pop();
                IExpression left = exprStack.Pop();
                char lastOper = literalStack.Pop();
                exprStack.Push(new BinaryExpression(left, right, new String(lastOper,1)));
            }
            
            return exprStack.Pop();
        }
    }
    
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            SimpleParser parser = new SimpleParser();
            var dumpVisitor = new DumpVisitor();
            IExpression result = parser.Parse("2+2");
            result.Accept(dumpVisitor);
            Assert.AreEqual("Binary(Literal(2)+Literal(2))", dumpVisitor.ToString());
            Assert.Pass();
        }
        
        [Test]
        public void Test2()
        {
            SimpleParser parser = new SimpleParser();
            var dumpVisitor = new DumpVisitor();
            IExpression result = parser.Parse("1+2+3+4");
            result.Accept(dumpVisitor);
            Assert.AreEqual("Binary(Binary(Binary(Literal(1)+Literal(2))+Literal(3))+Literal(4))", dumpVisitor.ToString());
            Assert.Pass();
        }
        
        [Test]
        public void Test3()
        {
            SimpleParser parser = new SimpleParser();
            var dumpVisitor = new DumpVisitor();
            IExpression result = parser.Parse("1+2+(3+4)");
            result.Accept(dumpVisitor);
            Assert.AreEqual("Binary(Binary(Literal(1)+Literal(2))+Paren(Binary(Literal(3)+Literal(4))))", dumpVisitor.ToString());
            Assert.Pass();
        }
        
        [Test]
        public void Test4()
        {
            SimpleParser parser = new SimpleParser();
            var dumpVisitor = new DumpVisitor();
            IExpression result = parser.Parse("1+(3+4)*5");
            result.Accept(dumpVisitor);
            Assert.AreEqual("Binary(Literal(1)+Binary(Paren(Binary(Literal(3)+Literal(4)))*Literal(5)))", dumpVisitor.ToString());
            Assert.Pass();
        }
        
        [Test]
        public void Test5()
        {
            SimpleParser parser = new SimpleParser();
            var dumpVisitor = new DumpVisitor();
            IExpression result = parser.Parse("(2*2/(k+v)-1)");
            result.Accept(dumpVisitor);
            Assert.AreEqual("Paren(Binary(Binary(Binary(Literal(2)*Literal(2))/Paren(Binary(Variable(k)+Variable(v))))-Literal(1)))", dumpVisitor.ToString());
            Assert.Pass();
        }
        
        [Test]
        public void Test6()
        {
            SimpleParser parser = new SimpleParser();
            var dumpVisitor = new DumpVisitor();
            IExpression result = parser.Parse("((((v))))");
            result.Accept(dumpVisitor);
            Assert.AreEqual("Paren(Paren(Paren(Paren(Variable(v)))))", dumpVisitor.ToString());
            Assert.Pass();
        }
        
        [Test]
        public void Test7()
        {
            SimpleParser parser = new SimpleParser();
            var dumpVisitor = new DumpVisitor();
            IExpression result = parser.Parse("2*5-5/3");
            result.Accept(dumpVisitor);
            Assert.AreEqual("Binary(Binary(Literal(2)*Literal(5))-Binary(Literal(5)/Literal(3)))", dumpVisitor.ToString());
            Assert.Pass();
        }
        [Test]
        public void Test8()
        {
            SimpleParser parser = new SimpleParser();
            var dumpVisitor = new DumpVisitor();
            IExpression result = parser.Parse("(v*(3+5))-7");
            result.Accept(dumpVisitor);
            Assert.AreEqual("Binary(Paren(Binary(Variable(v)*Paren(Binary(Literal(3)+Literal(5)))))-Literal(7))", dumpVisitor.ToString());
            Assert.Pass();
        }
        
        [Test]
        public void Test9()
        {
            SimpleParser parser = new SimpleParser();
            var dumpVisitor = new DumpVisitor();
            IExpression result = parser.Parse("1+2*3-v/2");
            result.Accept(dumpVisitor);
            Assert.AreEqual("Binary(Binary(Literal(1)+Binary(Literal(2)*Literal(3)))-Binary(Variable(v)/Literal(2)))", dumpVisitor.ToString());
            Assert.Pass();
        }
        
        [Test]
        public void Test10()
        {
            SimpleParser parser = new SimpleParser();
            var dumpVisitor = new DumpVisitor();
            IExpression result = parser.Parse("((v)*(7))");
            result.Accept(dumpVisitor);
            Assert.AreEqual("Paren(Binary(Paren(Variable(v))*Paren(Literal(7))))", dumpVisitor.ToString());
            Assert.Pass();
        }
    }
    
}