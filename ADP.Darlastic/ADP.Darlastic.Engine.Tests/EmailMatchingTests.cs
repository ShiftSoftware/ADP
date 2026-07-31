using ShiftSoftware.ADP.Darlastic.Engine;
using Xunit;

namespace ShiftSoftware.ADP.Darlastic.Engine.Tests;

/// <summary>
/// E-mail as a matching signal (2026-07-28). Every fixture is a shape MEASURED on a real corpus and
/// named for the case it stands in for (with placeholder names and domains), so a scoring change that
/// breaks one fails against something that actually happened rather than an invented example.
///
/// The corpus facts these encode: 2,047 of 24,067 goldens carried an e-mail across just 79 distinct
/// addresses; 84.7% of those goldens sat in a group sharing ONE name (a duplicated person, not many
/// people on one mailbox); the sole genuinely-shared-by-many address was a role account.
///
/// <see cref="Flags.EmailMatching"/> is process-global, so every test here sets it through
/// <see cref="EmailMatchingOn"/>, which restores the prior value. Tests within a class run
/// sequentially under xUnit; the other suite in this assembly builds records with no e-mail at all,
/// so it cannot be perturbed by the flag flipping here.
/// </summary>
public class EmailMatchingTests
{
    private const double AutoMerge = 0.90;
    private const double Steward = 0.80;

    private sealed class EmailMatchingOn : IDisposable
    {
        private readonly bool previous = Flags.EmailMatching;
        public EmailMatchingOn(bool on = true) => Flags.EmailMatching = on;
        public void Dispose() => Flags.EmailMatching = previous;
    }

    /// <summary>Name doubles as RawName and NormName (fixtures are pre-normalized, as in the sibling
    /// suite). Phones/ids default to absent — the duplicate-golden shape this feature targets is
    /// precisely a pair with NO phone to merge on.</summary>
    private static RealRecord Rec(int idx, string src, string id, string name,
                                  string? phone = null, string[]? emails = null, string? nationalId = null) =>
        new(idx, src, id, name, name,
            phone is null ? [] : [phone], [], nationalId, null, Emails: emails);

    // ---------------------------------------------------------------- canonicalization

    [Theory]
    // Case and surrounding whitespace are not identity.
    [InlineData("  M.Keller@Distributor.EXAMPLE ", "m.keller@distributor.example")]
    // +tag is universally routed to the base mailbox.
    [InlineData("aza+crm@shift.software", "aza@shift.software")]
    // Gmail documents local-part dots as insignificant...
    [InlineData("aza.asim@gmail.com", "azaasim@gmail.com")]
    [InlineData("azaasim@googlemail.com", "azaasim@gmail.com")]
    // ...and no other provider does, so dots MUST survive elsewhere. Without this gate
    // 'a.b@mail.ru' and 'ab@mail.ru' — two different people — would merge.
    [InlineData("a.b@mail.ru", "a.b@mail.ru")]
    public void Canonicalize_AppliesOnlyProvablySameMailboxTransforms(string raw, string expected) =>
        Assert.Equal(expected, Norm.Email(raw));

    [Fact]
    public void Canonicalize_NeverGuessesAtATypo()
    {
        // The whole argument against domain spell-correction: if 'gmal.com' is itself a real
        // mailbox, rewriting it merges two strangers, and nothing in the corpus says which case
        // you are in. So the typo is preserved and simply fails to match anything.
        Assert.Equal("azaasim@gmal.com", Norm.Email("azaasim@gmal.com"));
        Assert.NotEqual(Norm.Email("azaasim@gmail.com"), Norm.Email("azaasim@gmal.com"));

        // Nor does a near-miss local part collapse: local parts are a dense identifier space.
        Assert.NotEqual(Norm.Email("azaasim@gmail.com"), Norm.Email("azaasm@gmail.com"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("@nolocal.com")]
    [InlineData("nodomain@")]
    [InlineData("x@localhost")]      // no dotted host
    [InlineData("x@a.b")]            // 1-char TLD
    [InlineData("x@a..b.com")]       // malformed
    [InlineData("has space@x.com")]
    [InlineData("+tagonly@x.com")]   // local part is empty once the tag is stripped
    public void Canonicalize_RejectsWhatCannotBeAMailbox(string raw) =>
        Assert.Equal("", Norm.Email(raw));

    // ---------------------------------------------------------------- the off switch

    [Fact]
    public void Disabled_IsInertEverywhere()
    {
        // The zero-delta guarantee: a tenant that never enables e-mail must score and block exactly
        // as it did before the feature existed. This is the regression guard for every other tenant.
        var a = Rec(0, "ticket-gen", "1", "martin keller", emails: ["m.keller@distributor.example"]);
        var b = Rec(1, "activation", "2", "martin keller", emails: ["m.keller@distributor.example"]);

        using var off = new EmailMatchingOn(false);

        double score = RealMatcher.Score(a, b, out var flags);
        Assert.True(score < Steward, "with e-mail off this pair has only a name — it must stay damped");
        Assert.True(flags.HasFlag(MatchFlags.NameOnlyDamp), "the name-only damp still applies when e-mail is off");
        Assert.Equal(MatchFlags.None, flags & (MatchFlags.EmailsBoth | MatchFlags.EmailExact | MatchFlags.EmailMerge));
        Assert.DoesNotContain(RealMatcher.BlockKeysOf(a), k => k.StartsWith("E:", StringComparison.Ordinal));
    }

    // ---------------------------------------------------------------- the case that motivated this

    [Fact]
    public void SharedMailboxAndOneName_AutoMerges_TheDuplicateGoldenShape()
    {
        // A duplicate-golden pair measured in a dev registry: one human, two identities, same name,
        // same address, NO phone on either side. Pre-e-mail this pair could not merge — name was the
        // only signal, so the damp parked it at 0.70, below every band.
        var a = Rec(0, "ticket-gen", "10573", "martin keller", emails: ["m.keller@distributor.example"]);
        var b = Rec(1, "activation", "10574", "martin keller", emails: ["M.Keller@Distributor.Example"]);

        using var on = new EmailMatchingOn();

        double score = RealMatcher.Score(a, b, out var flags);
        Assert.True(score >= AutoMerge, $"one person's two records on one mailbox must merge (was {score:F3})");
        Assert.True(flags.HasFlag(MatchFlags.EmailExact), "casing differences must not defeat the match");
        Assert.True(flags.HasFlag(MatchFlags.EmailMerge));
    }

    [Fact]
    public void SharedMailbox_MeansNameIsNoLongerTheOnlySignal()
    {
        // Directly asserts the name-only-damp exclusion. Remove `&& !emailExact` from the damp
        // condition in ScoreCore and this goes red: the engine would be claiming name is the only
        // evidence while it is simultaneously scoring a shared mailbox.
        var a = Rec(0, "ticket-gen", "1", "martin keller", emails: ["m.keller@distributor.example"]);
        var b = Rec(1, "activation", "2", "martin keller", emails: ["m.keller@distributor.example"]);

        using var on = new EmailMatchingOn();

        RealMatcher.Score(a, b, out var flags);
        Assert.False(flags.HasFlag(MatchFlags.NameOnlyDamp));
    }

    [Fact]
    public void SharedMailbox_MergesNameChainSlices()
    {
        // One dealer mailbox carried both of these in the corpus — one person recorded at two
        // lengths of the same name chain, which NameConsistent already understands.
        var a = Rec(0, "dms-alpha", "1", "karim aliev", emails: ["karim.a@dealer.example"]);
        var b = Rec(1, "ticket-gen", "2", "karimjon rustamovich aliev", emails: ["karim.a@dealer.example"]);

        using var on = new EmailMatchingOn();

        Assert.True(RealMatcher.Score(a, b) >= AutoMerge);
    }

    [Fact]
    public void SharedMailbox_WithNoNameToContradictIt_StillMerges()
    {
        var a = Rec(0, "ticket-gen", "1", "", emails: ["someone@example.com"]);
        var b = Rec(1, "activation", "2", "rustam qodirov", emails: ["someone@example.com"]);

        using var on = new EmailMatchingOn();

        Assert.True(RealMatcher.Score(a, b) >= AutoMerge);
    }

    // ---------------------------------------------------------------- the guards

    [Fact]
    public void SharedMailbox_ButDifferentPeople_IsHeldInTheStewardBand()
    {
        // One dealer mailbox carried three distinct staff names in the corpus — a mailbox typed onto
        // the wrong record. This is the one shape that makes e-mail dangerous, and the name gate is
        // what refuses it. It must NOT auto-merge.
        var a = Rec(0, "ticket-ssc", "1", "nazarov bekzod", emails: ["b.nazarov@dealer.example"]);
        var b = Rec(1, "ticket-ssc", "2", "helena schmidt", emails: ["b.nazarov@dealer.example"]);

        using var on = new EmailMatchingOn();

        double score = RealMatcher.Score(a, b, out var flags);
        Assert.True(score < AutoMerge, $"unrelated names on one mailbox must not merge (was {score:F3})");
        Assert.True(flags.HasFlag(MatchFlags.EmailExact), "the signal still fired — it is the FLOOR that is withheld");
        Assert.False(flags.HasFlag(MatchFlags.EmailMerge));
    }

    [Fact]
    public void RoleMailbox_IsNeverIdentityEvidence()
    {
        // One distributor 'info@' address fronted five different people. A role local part is
        // suppressed outright: no block key, no signal, no floor.
        var a = Rec(0, "ticket-gen", "1", "sergei ivanov", emails: ["info@distributor.example"]);
        var b = Rec(1, "ticket-gen", "2", "dilnoza", emails: ["info@distributor.example"]);

        using var on = new EmailMatchingOn();

        double score = RealMatcher.Score(a, b, out var flags);
        Assert.True(score < Steward);
        Assert.False(flags.HasFlag(MatchFlags.EmailExact));
        Assert.True(flags.HasFlag(MatchFlags.EmailRoleOnly), "suppressed is a different answer from absent");
        Assert.DoesNotContain(RealMatcher.BlockKeysOf(a), k => k.StartsWith("E:", StringComparison.Ordinal));
    }

    [Fact]
    public void RoleSuppression_IsByLocalPart_NotByDomainOrFrequency()
    {
        // Both alternatives were measured against the corpus and both destroy true matches:
        // a corporate DOMAIN hosts real individual mailboxes, and the highest-FREQUENCY addresses
        // are one duplicated person. Only the local part naming a function is safe.
        Assert.True(RealMatcher.IsRoleEmail("info@distributor.example"));
        Assert.False(RealMatcher.IsRoleEmail("m.keller@distributor.example"));
        Assert.False(RealMatcher.IsRoleEmail("personal_user@mail.ru"));
    }

    [Fact]
    public void DifferingMailboxes_NeverPenalize()
    {
        // E-mail is asymmetric evidence, like VIN and address. One person routinely holds several
        // mailboxes (the corpus has one human as both a corporate and a personal address), so a
        // non-match must contribute nothing rather than score zero at full weight — otherwise
        // enabling the feature would SPLIT true matches. Also what makes the change strictly
        // additive: turning e-mail on can raise a pair's confidence, never lower it.
        var aNoMail = Rec(0, "dms-alpha", "1", "omar salim", "9931234567");
        var bNoMail = Rec(1, "activation", "2", "omar salim", "9931234567");
        var aMail = aNoMail with { Emails = ["omar@gmail.com"] };
        var bMail = bNoMail with { Emails = ["o.salim@dealer.example"] };

        using var on = new EmailMatchingOn();

        Assert.Equal(RealMatcher.Score(aNoMail, bNoMail), RealMatcher.Score(aMail, bMail), 9);
    }

    [Fact]
    public void ConflictingNationalIds_StillVetoAnEmailMerge()
    {
        // Hard evidence of two people outranks the e-mail floor, exactly as it does the sold-VIN
        // and same-as floors — the ×0.3 penalty is applied after them by construction.
        var a = Rec(0, "dms-alpha", "1", "rustam qodirov", emails: ["r.qodirov@dealer.example"], nationalId: "12345678901");
        var b = Rec(1, "dms-beta", "2", "rustam qodirov", emails: ["r.qodirov@dealer.example"], nationalId: "99999999999");

        using var on = new EmailMatchingOn();

        double score = RealMatcher.Score(a, b, out var flags);
        Assert.True(flags.HasFlag(MatchFlags.IdConflict));
        Assert.True(score < Steward, $"a national-id conflict must crush the e-mail floor (was {score:F3})");
    }

    // ---------------------------------------------------------------- blocking

    [Fact]
    public void BlockKeys_UseTheCanonicalForm()
    {
        using var on = new EmailMatchingOn();

        // Two spellings of ONE Gmail mailbox must co-block, or the pair is never even scored.
        var a = Rec(0, "app", "1", "aza asim", emails: ["Aza.Asim+crm@googlemail.com"]);
        var b = Rec(1, "crm", "2", "aza asim", emails: ["azaasim@gmail.com"]);

        Assert.Contains("E:azaasim@gmail.com", RealMatcher.BlockKeysOf(a));
        Assert.Contains("E:azaasim@gmail.com", RealMatcher.BlockKeysOf(b));
    }

    [Fact]
    public void SharedMailboxBlocks_AreCappedLikeIdentifierKeys_NotSilently()
    {
        // A shared mailbox that slipped past the role list must not explode the candidate set. The
        // cap is the phone cap, and — the point of the assertion — the drop is COUNTED, so it can
        // never read as "covered everything" in a run log.
        using var on = new EmailMatchingOn();

        // Names are single-token and pairwise distinct in their first 6 characters, so NO name key
        // groups them: the shared mailbox is the only thing that can put these records in one block.
        // (The first cut of this test used "person {i}", whose common 'person' prefix formed an
        // N-block of 60 and hid what was being measured.)
        const string alphabet = "abcdefghijklmnopqrstuvwxyz";
        static string Name(int i) => $"{alphabet[i / 26]}{alphabet[i % 26]}zzzz";
        List<RealRecord> Corpus(int n) => Enumerable.Range(0, n)
            .Select(i => Rec(i, "ticket-ssc", i.ToString(), Name(i), emails: ["shared@dealer.example"]))
            .ToList();

        // Positive control: under the cap the mailbox DOES block, so the test cannot pass merely
        // because e-mail keys were never emitted.
        var under = RealMatcher.BuildBlocks(Corpus(40), phoneBlockCap: 50);
        Assert.Equal(0, under.SkippedEmailBlocks);
        Assert.Contains(under.Blocks, b => b.Count == 40);

        // Over the cap it is dropped — and counted, so a run log can never report it as covered.
        var over = RealMatcher.BuildBlocks(Corpus(60), phoneBlockCap: 50);
        Assert.Equal(1, over.SkippedEmailBlocks);
        Assert.Equal(1, over.SkippedBlocks);
        Assert.Empty(over.Blocks);
    }
}
