namespace ShiftSoftware.ADP.Surveys.Shared.Evaluation.ExpressionSandbox;

/// <summary>
/// Hand-rolled recursive-descent parser. Grammar is described on <see cref="ExprNode"/>.
/// </summary>
internal sealed class Parser(List<Token> tokens)
{
    private readonly List<Token> tokens = tokens;
    private int pos;

    public ExprNode Parse()
    {
        var expr = ParseOr();
        Expect(TokenKind.EndOfInput);
        return expr;
    }

    private ExprNode ParseOr()
    {
        var left = ParseAnd();
        while (Match(TokenKind.Or))
            left = new BinaryOpNode("||", left, ParseAnd());
        return left;
    }

    private ExprNode ParseAnd()
    {
        var left = ParseEquality();
        while (Match(TokenKind.And))
            left = new BinaryOpNode("&&", left, ParseEquality());
        return left;
    }

    private ExprNode ParseEquality()
    {
        var left = ParseComparison();
        while (true)
        {
            string? op = Peek().Kind switch
            {
                TokenKind.Eq or TokenKind.StrictEq => "==",
                TokenKind.NotEq or TokenKind.StrictNotEq => "!=",
                _ => null,
            };
            if (op is null) break;
            Advance();
            left = new BinaryOpNode(op, left, ParseComparison());
        }
        return left;
    }

    private ExprNode ParseComparison()
    {
        var left = ParseUnary();
        while (true)
        {
            string? op = Peek().Kind switch
            {
                TokenKind.Lt => "<",
                TokenKind.Gt => ">",
                TokenKind.LtEq => "<=",
                TokenKind.GtEq => ">=",
                _ => null,
            };
            if (op is null) break;
            Advance();
            left = new BinaryOpNode(op, left, ParseUnary());
        }
        return left;
    }

    private ExprNode ParseUnary()
    {
        if (Match(TokenKind.Not))
            return new UnaryOpNode("!", ParseUnary());
        return ParsePrimary();
    }

    private ExprNode ParsePrimary()
    {
        var tok = Peek();
        switch (tok.Kind)
        {
            case TokenKind.Number:
            case TokenKind.String:
            case TokenKind.True:
            case TokenKind.False:
            case TokenKind.Null:
                Advance();
                return new LiteralNode(tok.Literal);

            case TokenKind.LParen:
                Advance();
                var inner = ParseOr();
                Expect(TokenKind.RParen);
                return inner;

            case TokenKind.LBracket:
                return ParseArray();

            case TokenKind.Identifier:
                return ParseIdentifierExpression();
        }

        throw new ExpressionSyntaxException($"unexpected token '{tok.Text}'.", tok.Position);
    }

    private ExprNode ParseArray()
    {
        Expect(TokenKind.LBracket);
        var items = new List<ExprNode>();
        if (Peek().Kind != TokenKind.RBracket)
        {
            items.Add(ParseOr());
            while (Match(TokenKind.Comma)) items.Add(ParseOr());
        }
        Expect(TokenKind.RBracket);
        return new ArrayNode(items);
    }

    private ExprNode ParseIdentifierExpression()
    {
        var ident = Advance();
        if (ident.Text == "answers")
            return ParseAnswersAccess(ident.Position);

        // Otherwise, must be a function call: ident(...).
        Expect(TokenKind.LParen);
        var args = new List<ExprNode>();
        if (Peek().Kind != TokenKind.RParen)
        {
            args.Add(ParseOr());
            while (Match(TokenKind.Comma)) args.Add(ParseOr());
        }
        Expect(TokenKind.RParen);
        return new CallNode(ident.Text, args);
    }

    private ExprNode ParseAnswersAccess(int position)
    {
        string key;
        if (Match(TokenKind.Dot))
        {
            var ident = Expect(TokenKind.Identifier);
            key = ident.Text;
        }
        else if (Match(TokenKind.LBracket))
        {
            var str = Expect(TokenKind.String);
            Expect(TokenKind.RBracket);
            key = (string)str.Literal!;
        }
        else
        {
            throw new ExpressionSyntaxException("'answers' must be followed by .key or ['key'].", position);
        }
        return new AnswersAccessNode(key);
    }

    private Token Peek() => tokens[pos];

    private Token Advance()
    {
        var t = tokens[pos];
        if (t.Kind != TokenKind.EndOfInput) pos++;
        return t;
    }

    private bool Match(TokenKind kind)
    {
        if (Peek().Kind != kind) return false;
        Advance();
        return true;
    }

    private Token Expect(TokenKind kind)
    {
        var t = Peek();
        if (t.Kind != kind)
            throw new ExpressionSyntaxException($"expected {kind}, got '{t.Text}'.", t.Position);
        Advance();
        return t;
    }
}
