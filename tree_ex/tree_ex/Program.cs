using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LabWorks_For_10_
{
    internal class Program
    {
        readonly static string patternIsFor = @"^for\s*\(\s*([a-zA-Z]+[0-9]*)\s*=\s*(\d+)\s*;\s*([a-zA-Z]+[0-9]*)\s*<\s*(\d+)\s*;\s*([a-zA-Z]+[0-9]*)\s*=\s*([a-zA-Z]+[0-9]*)\s*([\+\-\*\/])\s*(\d+)\s*\)\s*\{\s*((?:[a-zA-Z]+[0-9]*\s*=\s*[a-zA-Z0-9]*\s*[\+\-\*\/]\s*[a-zA-Z0-9]+\s*;\s*)*)\s*\}";
        readonly static string patternExpr = @"([a-zA-Z]+[0-9]*)\s*=\s*([a-zA-Z]+[0-9]*|\d+)\s*([\+\-\*\/])\s*([a-zA-Z]+[0-9]*|\d+)";
        readonly static string patternMath = @"[+\-*/]";
        readonly static string patternsravn = @"(==|>|<)";
        readonly static string patternVar = @"[a-zA-Z]+[0-9]*";
        readonly static string patternAssign = @"(=)";
        readonly static string patternCondition = @"([a-zA-Z]+[0-9]*)\s*([<>=!]+)\s*([0-9]+|[a-zA-Z]+[0-9]*)";

        static string input = "for ( i = 0; i < 10; i = i + 2) { x = x + i; a = x * 2; b = x * a; }";

        static void Main(string[] args)
        {
            bool isValid = Regex.IsMatch(input, patternIsFor);

            if (isValid && input != null)
            {
                int startCondition = input.IndexOf('(');
                int endCondition = input.IndexOf(')') + 1;
                int startBody = input.IndexOf('{');

                string root = input.Substring(0, startCondition).Trim();
                string condition = input.Substring(startCondition + 1, endCondition - startCondition - 2).Trim(); // Убираем скобки
                string body = input.Substring(startBody + 1, input.Length - startBody - 2).Trim(); // Убираем фигурные скобки

                // Создаем корень дерева
                Node rootTree = new Node(root);

                // Добавляем условия
                string[] conditions = condition.Split(';');
                foreach (var cond in conditions)
                {
                    if (!string.IsNullOrWhiteSpace(cond))
                    {
                        rootTree.Leafs.Add(ParseCondition(cond.Trim()));
                    }
                }

                // Добавляем выражения
                string[] statements = body.Split(';');
                foreach (var statement in statements)
                {
                    if (!string.IsNullOrWhiteSpace(statement))
                    {
                        rootTree.Leafs.Add(ParseExpression(statement.Trim()));
                    }
                }

                PrintTreeDFS(rootTree);


                string assemblyCode = GenerateExpression(rootTree);
                Console.WriteLine(assemblyCode);

            }
            else
            {
                Console.WriteLine("Невалидная строка");
            }
        }

        static Node ParseCondition(string condition)
        {
            // Условие в формате: переменная = значение или переменная < значение
            var matchAssign = Regex.Match(condition, patternAssign);
            if (matchAssign.Success)
            {
                string[] parts = condition.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                Node assignNode = new Node("=");
                assignNode.Leafs.Add(new Node(parts[0].Trim())); // Переменная
                assignNode.Leafs.Add(ParseMathExpression(parts[1].Trim())); // Значение (число или выражение)
                return assignNode;
            }

            // Проверка на другие условия, например, i < 10
            var matchCondition = Regex.Match(condition, patternCondition);
            if (matchCondition.Success)
            {
                Node conditionNode = new Node(matchCondition.Groups[2].Value.Trim()); // Оператор (например, <)
                conditionNode.Leafs.Add(new Node(matchCondition.Groups[1].Value.Trim())); // Левый операнд (переменная)
                conditionNode.Leafs.Add(new Node(matchCondition.Groups[3].Value.Trim())); // Правый операнд (значение)
                return conditionNode;
            }

            return null; // Если не удалось распарсить, возвращаем null
        }

        static Node ParseExpression(string expression)
        {
            // Условие в формате: переменная = выражение
            var match = Regex.Match(expression, patternExpr);
            if (match.Success)
            {
                Node assignNode = new Node("=");
                assignNode.Leafs.Add(new Node(match.Groups[1].Value.Trim())); // Переменная
                assignNode.Leafs.Add(ParseMathExpression(match.Groups[2].Value.Trim(), match.Groups[3].Value.Trim(), match.Groups[4].Value.Trim()));
                return assignNode;
            }

            return null; // Если не удалось распарсить, возвращаем null
        }

        static Node ParseMathExpression(string left, string op, string right)
        {
            Node opNode = new Node(op);
            opNode.Leafs.Add(new Node(left)); // Левый операнд
            opNode.Leafs.Add(new Node(right)); // Правый операнд
            return opNode;
        }

        static Node ParseMathExpression(string expression)
        {
            // Здесь мы можем расширить функциональность для обработки арифметических выражений
            var parts = Regex.Split(expression, patternMath);
            var ops = Regex.Matches(expression, patternMath);
            Node rootNode = null;

            for (int i = 0; i < parts.Length; i++)
            {
                if (i == 0)
                {
                    rootNode = new Node(parts[i].Trim());
                }
                else
                {
                    Node opNode = new Node(ops[i - 1].Value.Trim());
                    opNode.Leafs.Add(rootNode);
                    opNode.Leafs.Add(new Node(parts[i].Trim()));
                    rootNode = opNode; // Обновляем корень для следующего цикла
                }
            }

            return rootNode;
        }

        static string GenerateExpression(Node exprNode)
        {
            int k = exprNode.Leafs.Count()-1;
            string str = "";
            string final = "LABEL_FALSE\n";
            for (int i=0;i<exprNode.Leafs.Count();i++)
            {
                str = HelpTree(exprNode.Leafs[k]);
                final += str;
                k--;
            }
            final += "LABEL_TRUE\n";
            return final;
        }

        static string HelpTree(Node node)
        {
            string str = "";
            
            if(node.Leafs.Count()!=0)
            {
                if(Regex.IsMatch(node.Value,patternAssign))
                {
                    str=Help_ravno(node.Leafs[1]);
                    str += "STORE " + node.Leafs[0].Value + "\n";
                }
                else if(Regex.IsMatch(node.Value, patternsravn))
                {
                    str="LOAD "+ node.Leafs[0].Value+"\n";
                    str += "SUB " + node.Leafs[1].Value + "\n";
                    if(node.Value=="==")
                    {
                        str += "BRANCH_ZERO LABEL_TRUE " + "\n";
                        
                    }
                    else if (node.Value == ">")
                    {
                        str += "BRANCH_POSITIVE LABEL_TRUE " + "\n";

                    }
                    else if (node.Value == "<")
                    {
                        str += "BRANCH_NEGATIVE  LABEL_TRUE " + "\n";

                    }
                    str += "JUMP LABEL_FALSE\n";
                }
                
            }
            
            return str;
        }


        static string Help_ravno(Node node)
        {
            string str = "";
            if (Regex.IsMatch(node.Value, patternMath))
            {
                str = "LOAD " + node.Leafs[0].Value + "\n";
                if (node.Value == "+")
                {

                    str += "ADD " + node.Leafs[1].Value + "\n";
                }
                else if (node.Value == "*")
                {

                    str += "MPY " + node.Leafs[1].Value + "\n";
                }

                else if (node.Value == "-")
                {

                    str += "SUB " + node.Leafs[1].Value + "\n";
                }

                else if (node.Value == "/")
                {

                    str += "DIV " + node.Leafs[1].Value + "\n";
                }
            }
            else
            {
                str="LOAD " + node.Value + "\n";    
            }


            return str;

        }



        static void PrintTreeDFS(Node node, string prefix = "", bool isLast = true)
        {
            if (node == null) return;

            Console.WriteLine(prefix + (isLast ? "└── " : "├── ") + node.Value);

            prefix += isLast ? "    " : "│   ";

            for (int i = 0; i < node.Leafs.Count; i++)
            {
                PrintTreeDFS(node.Leafs[i], prefix, i == node.Leafs.Count - 1);
            }
        }
    }

    class Node
    {
        public string Value { get; set; }
        public List<Node> Leafs { get; set; }

        public Node(string value)
        {
            Value = value;
            Leafs = new List<Node>();
        }
    }
}
