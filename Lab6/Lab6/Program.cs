using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

class ParseNode
{
    public string Value { get; set; }
    public List<ParseNode> Children { get; } = new List<ParseNode>();

    public ParseNode(string value) // инициализация узла с заданным значением
    {
        Value = value;
    }

    public void AddChild(ParseNode child) //добавляет дочерний узел к текущему узлу
    {
        Children.Add(child);
    }

    public void Print(string indent = "", bool isLast = true) // выводит дерево разбора в консоль с отступами для визуализации структуры
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
        var tokens = Tokenize(input); //разбивает входную строку на токены на основе определенных шаблонов.
        int i = 0;
        return ParseIfStatement(tokens, ref i); //начинает процесс разбора, начиная с токенов.
    }

    private List<string> Tokenize(string input)
    {
        string pattern = $@"{keywordPattern}|{identifierPattern}|{numberPattern}|{assignPattern}|{comparisonPattern}|{punctuationPattern}";
        MatchCollection matches = Regex.Matches(input, pattern);
        var tokens = new List<string>();
        foreach (Match match in matches)
        {
            tokens.Add(match.Value); //результат сохраняется в списке токенов.
        }
        return tokens;
    }

    private ParseNode ParseIfStatement(List<string> tokens, ref int i)
    {
        ParseNode ifNode = new ParseNode("if"); //создает узел "if".

        if (tokens[i] == "if") //начало с if
        {
            i++; // Пропускаем "if"
            ifNode.AddChild(ParseCondition(tokens, ref i)); //разбиваем выражение

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
        else if (Regex.IsMatch(tokens[i], assignPattern) || Regex.IsMatch(tokens[i], comparisonPattern))
        {
            ParseNode operationNode = new ParseNode(tokens[i]);
            i++;
            operationNode.AddChild(ParseExpression(tokens, ref i));
            operationNode.AddChild(ParseExpression(tokens, ref i));
            return operationNode;
        }

        return new ParseNode("Unknown");
    }

    static void Main()
    {
        string input = "if x > 5 then y := 10; z := 20; else y := 20; z := 40;";

        Lexer lexer = new Lexer();
        ParseNode parseTree = lexer.BuildParseTree(input);

        Console.WriteLine("Дерево разбора:");
        parseTree.Print();
    }
}
