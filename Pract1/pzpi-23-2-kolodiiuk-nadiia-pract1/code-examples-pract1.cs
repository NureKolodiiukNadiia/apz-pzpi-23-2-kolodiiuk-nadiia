using System;
using System.Text;

namespace Solution;

public class Token
{
    public string Text { get; }
    public string Type { get; }

    public Token(string text, string type)
    {
        Text = text;
        Type = type;
    }
}

public interface IIterator<T>
{
    bool MoveNext();
    T Current { get; }
    void Reset();
}

public interface IIterableCollection<T>
{
    IIterator<T> CreateIterator();
}

public class LexerIterator : IIterator<Token>
{
    private const string IntegerCode = "integer";
    private const string BooleanCode = "boolean";
    private const string OperatorCode = "operator";
    private const string StringCode = "string";
    private const string KeywordCode = "keyword";
    private const string WhitespaceCode = "whitespace";
    private const string IdentifierCode = "identifier";
    private const string True = "true";
    private const string False = "false";
    private const string Return = "return";
    private const string If = "if";
    private const string Else = "else";
    private const string For = "for";
    private const string While = "while";
    private const string Func = "func";
    private const string Break = "break";

    private readonly char[] _str;
    private int _currIndex;
    private Token _current;
    private readonly int _len;
    private bool _hasNotMovedNext;

    public LexerIterator(string buffer)
    {
        if (buffer is null)
        {
            buffer = "";
        }
        _str = new char[buffer.Length];
        _len = buffer.Length;
        for (var i = 0; i < buffer.Length; ++i)
        {
            _str[i] = buffer[i];
        }
    }

    public Token Current
    {
        get
        {
            if (_hasNotMovedNext)
            {
                throw new InvalidOperationException("No current token available.");
            }
            return _current;
        }
    }

    public void Reset()
    {
        _currIndex = 0;
    }

    public bool MoveNext()
    {
        if (_currIndex >= _len)
        {
            _hasNotMovedNext = true;

            return false;
        }

        var res = false;
        switch (_str[_currIndex])
        {
            case '+' or '-' or '*' or '/' or '%' or '(' or ')' or '=':
                {
                    res = LexOp();
                    break;
                }
            case '0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9':
                {
                    res = LexInteger();
                    break;
                }
            case '"':
                {
                    res = LexStr();
                    break;
                }
            case ' ' or '\t' or '\n':
                {
                    res = LexWhitespace();
                    break;
                }
            default:
                {
                    res = LexBooleanIdentifierKeyword();
                    break;
                }
        }

        _hasNotMovedNext = !res;

        return res;
    }

    private bool LexOp()
    {
        _current = new Token(_str[_currIndex++].ToString(), OperatorCode);

        return true;
    }

    private bool LexInteger()
    {
        var sb = new StringBuilder();
        var i = _currIndex;
        while (i < _len && _str[i] >= '0' && _str[i] <= '9')
        {
            sb.Append(_str[i]);
            i++;
        }

        _currIndex = i;
        _current = new Token(sb.ToString(), IntegerCode);

        return true;
    }

    private bool LexStr()
    {
        var sb = new StringBuilder();
        sb.Append('"');
        var i = ++_currIndex;
        while (i < _len && _str[i] != '"')
        {
            sb.Append(_str[i]);
            i++;
        }

        if (i < _len && _str[i] == '"')
        {
            sb.Append('"');
            _currIndex = i + 1;
            _current = new Token(sb.ToString(), StringCode);

            return true;
        }
        else
        {
            return false;
        }
    }

    private bool LexWhitespace()
    {
        var sb = new StringBuilder();
        var i = _currIndex;
        while (i < _len && (_str[i] == ' ' || _str[i] == '\t' || _str[i] == '\n'))
        {
            sb.Append(_str[i]);
            i++;
        }

        _currIndex = i;
        _current = new Token(sb.ToString(), WhitespaceCode);

        return true;
    }

    private Token LexBooleans()
    {
        if (_len - _currIndex >= 4)
        {
            var span = new ReadOnlySpan<char>(_str, _currIndex, 4);
            if (span is True)
            {
                _currIndex += 4;

                return new Token(True, BooleanCode);
            }
        }

        if (_len - _currIndex >= 5)
        {
            var span = new ReadOnlySpan<char>(_str, _currIndex, 5);
            if (span is False)
            {
                _currIndex += 5;

                return new Token(False, BooleanCode);
            }
        }

        return null;
    }

    private Token LexKeywords()
    {
        for (int i = 2; i <= 6; ++i)
        {
            if (_len - _currIndex >= i)
            {
                var span = new ReadOnlySpan<char>(_str, _currIndex, i);
                if (span is Return or If or Else or For or While or Func or Break)
                {
                    _currIndex += i;

                    return new Token(span.ToString(), KeywordCode);
                }
            }
        }

        return null;
    }

    private Token LexIdentifiers()
    {
        var sb = new StringBuilder();
        while (_currIndex < _len && !IsStopIdentifierLexing(_str[_currIndex]))
        {
            sb.Append(_str[_currIndex]);
            _currIndex++;
        }

        return new Token(sb.ToString(), IdentifierCode);

        bool IsStopIdentifierLexing(char c)
        {
            var isOp = c == '+' || c == '-' || c == '*' || c == '/' || c == '%' || c == '(' || c == ')' || c == '=';
            var isWhiteSpace = c == ' ' || c == '\n' || c == '\t';
            var isNotAlphaNumericExtended = !(char.IsLetterOrDigit(c) || c == '_' || c == '$');

            return isOp || isWhiteSpace || isNotAlphaNumericExtended;
        }
    }

    bool LexBooleanIdentifierKeyword()
    {
        var token = LexBooleans();
        if (token is not null)
        {
            _current = token;

            return true;
        }

        token = LexKeywords();
        if (token is not null)
        {
            _current = token;

            return true;
        }

        token = LexIdentifiers();
        if (token is not null)
        {
            _current = token;

            return true;
        }

        return false;
    }
}

public class LexingCollection : IIterableCollection<Token>
{
    private readonly string _inputBuffer;

    public LexingCollection(string buffer)
    {
        _inputBuffer = buffer ?? "";
    }

    public IIterator<Token> CreateIterator()
    {
        return new LexerIterator(_inputBuffer);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var code = "func calculate(int x)\nreturn x + 2";
        var collection = new LexingCollection(code);
        var lexerIterator = collection.CreateIterator();
        Console.WriteLine("--- Lexing Tokens ---");
        var tokens = ToArray(lexerIterator);
        foreach (var token in tokens)
        {
            Console.WriteLine($"{token.Text} - {token.Type}");
        }

    }
    private static Token[] ToArray(IIterator<Token> lexer)
    {
        List<Token> tokens = new List<Token>();
        while (lexer.MoveNext())
        {
            tokens.Add(lexer.Current);
        }

        return tokens.ToArray();
    }
}
