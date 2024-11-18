using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

class ParseNode
{
    public string Value { get; set; }
    public List<ParseNode> Children { get; } = new List<ParseNode>();

    public ParseNode(string value) // Инициализация узла с заданным значением
    {
        Value = value;
    }

    public void AddChild(ParseNode child) // Добавляет дочерний узел к текущему узлу
    {
        Children.Add(child);
    }

    public void Print(string indent = "", bool isLast = true) // Выводит дерево разбора в консоль
    {
        Console.Write(indent);
        Console.Write(isLast ? "└── " : "├── ");
        Console.WriteLine(Value);

        indent += isLast ? "    " : "│   ";
        for (int i = 0; i < Children.Count; i++)
            Children[i].Print(indent, i == Children.Count - 1);
    }
}

class Lexer
{
    private static readonly string identifierPattern = @"[a-zA-Z_][a-zA-Z0-9_]*";
    private static readonly string numberPattern = @"\d+";
    private static readonly string assignPattern = @":=";
    private static readonly string comparisonPattern = @"[<>]=?|==";
    private static readonly string keywordPattern = @"\b(if|then|else)\b";
    private static readonly string punctuationPattern = @";";

    public ParseNode BuildParseTree(string input)
    {
        var tokens = Tokenize(input); // Разбивает входную строку на токены
        int i = 0;
        return ParseIfStatement(tokens, ref i); // Начинает процесс разбора
    }

    private List<string> Tokenize(string input)
    {
        string pattern = $@"{keywordPattern}|{identifierPattern}|{numberPattern}|{assignPattern}|{comparisonPattern}|{punctuationPattern}";
        MatchCollection matches = Regex.Matches(input, pattern);
        var tokens = new List<string>();
        foreach (Match match in matches)
        {
            tokens.Add(match.Value); // Результат сохраняется в списке токенов
        }
        return tokens;
    }

    private ParseNode ParseIfStatement(List<string> tokens, ref int i)
    {
        ParseNode ifNode = new ParseNode("if");

        if (tokens[i] == "if") // Начало с "if"
        {
            i++; // Пропускаем "if"
            ifNode.AddChild(ParseCondition(tokens, ref i)); // Разбиваем выражение

            if (tokens[i] == "then")
            {
                i++; // Пропускаем "then"
                ParseNode thenNode = new ParseNode("then");
                ParseMultipleAssignments(tokens, ref i, thenNode);
                ifNode.AddChild(thenNode);
            }

            if (i < tokens.Count && tokens[i] == "else")
            {
                i++; // Пропускаем "else"
                ParseNode elseNode = new ParseNode("else");
                ParseMultipleAssignments(tokens, ref i, elseNode);
                ifNode.AddChild(elseNode);
            }
        }

        return ifNode;
    }

    private ParseNode ParseCondition(List<string> tokens, ref int i)
    {
        ParseNode conditionNode = new ParseNode("Condition");
        conditionNode.AddChild(new ParseNode(tokens[i])); // Левый операнд
        i++;
        conditionNode.AddChild(new ParseNode(tokens[i])); // Оператор
        i++;
        conditionNode.AddChild(new ParseNode(tokens[i])); // Правый операнд
        i++;
        return conditionNode;
    }

    private void ParseMultipleAssignments(List<string> tokens, ref int i, ParseNode parentNode)
    {
        while (i < tokens.Count && tokens[i] != "else" && tokens[i] != "if")
        {
            parentNode.AddChild(ParseAssignment(tokens, ref i));
            if (i < tokens.Count && tokens[i] == ";")
            {
                i++; // Пропускаем ";"
            }
            else
            {
                break; // Прекращаем, если нет ";" или достигнут конец блока
            }
        }
    }

    private ParseNode ParseAssignment(List<string> tokens, ref int i)
    {
        ParseNode assignNode = new ParseNode(":=");
        assignNode.AddChild(new ParseNode(tokens[i])); // Переменная
        i++;
        i++; // Пропускаем ":="
        assignNode.AddChild(ParseExpression(tokens, ref i)); // Значение или выражение
        return assignNode;
    }

    private ParseNode ParseExpression(List<string> tokens, ref int i)
    {
        if (Regex.IsMatch(tokens[i], identifierPattern) || Regex.IsMatch(tokens[i], numberPattern))
        {
            return new ParseNode(tokens[i++]);
        }

        return new ParseNode("Unknown");
    }

    static string GenerateAssambly(ParseNode expr)
    {
        string result = ""; //пустой результат

        for (int i = expr.Children.Count - 1; i >= 0; i--) //обход эл-ов с конца
        {
            string childResult = Generate_condition(expr.Children[i]); //генерация ас.кода для тек. доч узла
            result += childResult;
        }

        result += GenerateThenElseCode(expr);//генерация ас.кода для самого узла

        return result;
    }

    static string GenerateThenElseCode(ParseNode expr)
    {
        string result = "";

        foreach (var child in expr.Children) //перебор всех доч. эл-ты узла
        {
            if (child.Value == "then" || child.Value == "else")
            {
                foreach (var grandChild in child.Children)
                {
                    if (grandChild.Value == ":=")
                    {
                        // Обрабатываем присваивания
                        result += Generate_add(grandChild.Children[1]);
                        result += "STORE " + grandChild.Children[0].Value + "\n";
                    }
                }
            }
        }

        return result;
    }

    static string Generate_condition(ParseNode node)
    {
        string str = "";

        if (node.Children.Count != 0) // есть доч. эл-ты
        {
            if (node.Value == ":=")
            {
                // Обрабатываем операцию присваивания
                str = Generate_add(node.Children[1]);
                str += "STORE " + node.Children[0].Value + "\n";
            }
            else if (node.Value == "Condition")
            {
                // Обрабатываем условие
                str = "LOAD " + node.Children[0].Value + "\n";
                str += "CMP " + node.Children[2].Value + "\n";

                string condition = node.Children[1].Value switch
                {
                    "==" => "BRANCH_ZERO LABEL_TRUE\n",
                    ">" => "BRANCH_POSITIVE LABEL_TRUE\n",
                    "<" => "BRANCH_NEGATIVE LABEL_TRUE\n",
                    _ => throw new Exception("Неизвестный оператор")
                };

                str += condition;
                str += "JUMP LABEL_FALSE\n";
            }
        }

        return str;
    }

    static string Generate_add(ParseNode node)
    {
        // Генерация кода для присваивания
        return "LOAD " + node.Value + "\n";
    }


    static void Main()
    {
        string input = "if x < 5 then y := 10; z := 20; else y := 20; z := 40;";

        Lexer lexer = new Lexer();
        ParseNode parseTree = lexer.BuildParseTree(input);

        Console.WriteLine("Дерево разбора:");
        parseTree.Print();

        Console.WriteLine("\nГенерация ассемблерного кода:");
        string generatedCode = GenerateAssambly(parseTree);
        Console.WriteLine(generatedCode);
    }
}
