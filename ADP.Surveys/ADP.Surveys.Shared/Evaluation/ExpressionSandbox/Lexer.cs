using System.Globalization;
using System.Text;

namespace ShiftSoftware.ADP.Surveys.Shared.Evaluation.ExpressionSandbox;

public enum TokenKind
{
    Number, String, True, False, Null,
    Identifier,
    Eq, StrictEq, NotEq, StrictNotEq,
    Lt, Gt, LtEq, GtEq,
    And, Or, Not,
    LParen, RParen, LBracket, RBracket,
    Comma, Dot,
    EndOfInput,
}

public readonly record struct Token(TokenKind Kind, string Text, object? Literal, int Position);

public class ExpressionSyntaxException(string message, int position)
    : Exception($"Expression syntax error at column {position}: {message}")
{
    public int Position { get; } = position;
}

/// <summary>
/// Hand-rolled lexer for the survey expression mini-language. Kept intentionally
/// terse — this is the shape the TypeScript SDK will mirror.
/// </summary>
internal sealed class Lexer(string source)
{
    private readonly string source = source;
    private int pos;

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();
        while (true)
        {
            SkipWhitespace();
            if (pos >= source.Length)
            {
                tokens.Add(new Token(TokenKind.EndOfInput, "", null, pos));
                return tokens;
            }
            tokens.Add(NextToken());
        }
    }

    private void SkipWhitespace()
    {
        while (pos < source.Length && char.IsWhiteSpace(source[pos])) pos++;
    }

    private Token NextToken()
    {
        var start = pos;
        var c = source[pos];

        if (char.IsDigit(c)) return ReadNumber(start);
        if (c == '\'' || c == '"') return ReadString(start, c);
        if (c == '_' || char.IsLetter(c)) return ReadIdentifier(start);

        switch (c)
        {
            case '(': pos++; return new(TokenKind.LParen, "(", null, start);
            case ')': pos++; return new(TokenKind.RParen, ")", null, start);
            case '[': pos++; return new(TokenKind.LBracket, "[", null, start);
            case ']': pos++; return new(TokenKind.RBracket, "]", null, start);
            case ',': pos++; return new(TokenKind.Comma, ",", null, start);
            case '.': pos++; return new(TokenKind.Dot, ".", null, start);
            case '=':
                if (Match("===")) return new(TokenKind.StrictEq, "===", null, start);
                if (Match("==")) return new(TokenKind.Eq, "==", null, start);
                throw new ExpressionSyntaxException("bare '=' is not a valid operator (use '==' or '===').", start);
            case '!':
                if (Match("!==")) return new(TokenKind.StrictNotEq, "!==", null, start);
                if (Match("!=")) return new(TokenKind.NotEq, "!=", null, start);
                pos++; return new(TokenKind.Not, "!", null, start);
            case '<':
                if (Match("<=")) return new(TokenKind.LtEq, "<=", null, start);
                pos++; return new(TokenKind.Lt, "<", null, start);
            case '>':
                if (Match(">=")) return new(TokenKind.GtEq, ">=", null, start);
                pos++; return new(TokenKind.Gt, ">", null, start);
            case '&':
                if (Match("&&")) return new(TokenKind.And, "&&", null, start);
                throw new ExpressionSyntaxException("expected '&&'.", start);
            case '|':
                if (Match("||")) return new(TokenKind.Or, "||", null, start);
                throw new ExpressionSyntaxException("expected '||'.", start);
        }

        throw new ExpressionSyntaxException($"unexpected character '{c}'.", start);
    }

    private bool Match(string s)
    {
        if (pos + s.Length > source.Length) return false;
        for (var i = 0; i < s.Length; i++)
            if (source[pos + i] != s[i]) return false;
        pos += s.Length;
        return true;
    }

    private Token ReadNumber(int start)
    {
        while (pos < source.Length && char.IsDigit(source[pos])) pos++;
        if (pos < source.Length && source[pos] == '.')
        {
            pos++;
            while (pos < source.Length && char.IsDigit(source[pos])) pos++;
        }
        var text = source.Substring(start, pos - start);
        var value = decimal.Parse(text, CultureInfo.InvariantCulture);
        return new(TokenKind.Number, text, value, start);
    }

    private Token ReadString(int start, char quote)
    {
        pos++; // opening quote
        var sb = new StringBuilder();
        while (pos < source.Length && source[pos] != quote)
        {
            var c = source[pos];
            if (c == '\\' && pos + 1 < source.Length)
            {
                var next = source[pos + 1];
                sb.Append(next switch
                {
                    'n' => '\n',
                    't' => '\t',
                    'r' => '\r',
                    '\\' => '\\',
                    '\'' => '\'',
                    '"' => '"',
                    _ => throw new ExpressionSyntaxException($"unknown escape '\\{next}'.", pos),
                });
                pos += 2;
            }
            else
            {
                sb.Append(c);
                pos++;
            }
        }
        if (pos >= source.Length)
            throw new ExpressionSyntaxException("unterminated string literal.", start);
        pos++; // closing quote
        var text = sb.ToString();
        return new(TokenKind.String, text, text, start);
    }

    private Token ReadIdentifier(int start)
    {
        while (pos < source.Length && (source[pos] == '_' || char.IsLetterOrDigit(source[pos]) || source[pos] == '-'))
            pos++;
        var text = source.Substring(start, pos - start);
        return text switch
        {
            "true" => new(TokenKind.True, text, true, start),
            "false" => new(TokenKind.False, text, false, start),
            "null" => new(TokenKind.Null, text, null, start),
            _ => new(TokenKind.Identifier, text, null, start),
        };
    }
}
