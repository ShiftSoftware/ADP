using System.Text.Json;
using ShiftSoftware.ADP.Surveys.Shared.Triggers;
using Xunit;

namespace ShiftSoftware.ADP.Surveys.Shared.Tests;

public class TriggerHasherTests
{
    private static JsonElement Payload(string json) => JsonDocument.Parse(json).RootElement;

    private static TriggerRecipient Recipient(string address = "+964 770 000 0001", string? locale = "ar")
        => new() { Address = address, Locale = locale };

    [Fact]
    public void BuildHashString_CsiGrRecipe_ProducesPathPrefixedJoined()
    {
        var recipe = new[] { "templateId", "recipient.address", "candidate.dealerId", "candidate.wip" };
        var payload = Payload("""{"dealerId":"1","wip":"40956","jobType":"GR"}""");

        var s = TriggerHasher.BuildHashString(recipe, templateId: 42, Recipient(), payload);

        // Path-prefixed; slot-separated by 0x1F (unit separator).
        Assert.Equal(
            "templateId=42recipient.address=+964 770 000 0001candidate.dealerId=1candidate.wip=40956",
            s);
    }

    [Fact]
    public void BuildHashBytes_DeterministicForSameInputs()
    {
        var recipe = new[] { "templateId", "recipient.address", "candidate.wip" };
        var payload = Payload("""{"wip":"40956"}""");

        var a = TriggerHasher.BuildHashBytes(recipe, 1, Recipient(), payload);
        var b = TriggerHasher.BuildHashBytes(recipe, 1, Recipient(), payload);

        Assert.Equal(a, b);
        Assert.Equal(32, a.Length); // SHA-256 = 256 bits = 32 bytes
    }

    [Fact]
    public void BuildHashBytes_DifferentDealerIdProducesDifferentHash()
    {
        var recipe = new[] { "templateId", "recipient.address", "candidate.dealerId", "candidate.wip" };
        var dealer1 = Payload("""{"dealerId":"1","wip":"40956"}""");
        var dealer2 = Payload("""{"dealerId":"2","wip":"40956"}""");

        var a = TriggerHasher.BuildHashBytes(recipe, 1, Recipient(), dealer1);
        var b = TriggerHasher.BuildHashBytes(recipe, 1, Recipient(), dealer2);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void BuildHashBytes_DifferentTemplateIdProducesDifferentHash()
    {
        var recipe = new[] { "templateId", "recipient.address", "candidate.wip" };
        var payload = Payload("""{"wip":"40956"}""");

        var a = TriggerHasher.BuildHashBytes(recipe, templateId: 1, Recipient(), payload);
        var b = TriggerHasher.BuildHashBytes(recipe, templateId: 2, Recipient(), payload);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void BuildHashBytes_DifferentRecipientAddressProducesDifferentHash()
    {
        var recipe = new[] { "templateId", "recipient.address", "candidate.wip" };
        var payload = Payload("""{"wip":"40956"}""");

        var a = TriggerHasher.BuildHashBytes(recipe, 1, Recipient("+964 1"), payload);
        var b = TriggerHasher.BuildHashBytes(recipe, 1, Recipient("+964 2"), payload);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void BuildHashString_MissingCandidatePathResolvesToEmpty()
    {
        var recipe = new[] { "candidate.missing" };
        var payload = Payload("""{"wip":"40956"}""");

        var s = TriggerHasher.BuildHashString(recipe, 1, Recipient(), payload);

        Assert.Equal("candidate.missing=", s);
    }

    [Fact]
    public void BuildHashString_NumericCandidateValueResolvesToRawText()
    {
        var recipe = new[] { "candidate.invoiceNumber" };
        var payload = Payload("""{"invoiceNumber":11045237}""");

        var s = TriggerHasher.BuildHashString(recipe, 1, Recipient(), payload);

        Assert.Equal("candidate.invoiceNumber=11045237", s);
    }

    [Fact]
    public void BuildHashString_BooleanCandidateValueResolves()
    {
        var recipe = new[] { "candidate.isActive" };
        var payload = Payload("""{"isActive":true}""");

        var s = TriggerHasher.BuildHashString(recipe, 1, Recipient(), payload);

        Assert.Equal("candidate.isActive=true", s);
    }

    [Fact]
    public void BuildHashString_RespectsRecipeOrder()
    {
        var payload = Payload("""{"a":"1","b":"2"}""");
        var recipeAB = new[] { "candidate.a", "candidate.b" };
        var recipeBA = new[] { "candidate.b", "candidate.a" };

        Assert.NotEqual(
            TriggerHasher.BuildHashString(recipeAB, 1, Recipient(), payload),
            TriggerHasher.BuildHashString(recipeBA, 1, Recipient(), payload));
    }

    [Fact]
    public void BuildHashString_RecipientLocaleResolves()
    {
        var recipe = new[] { "recipient.locale" };

        var enHash = TriggerHasher.BuildHashString(recipe, 1, Recipient(locale: "en"), Payload("{}"));
        var arHash = TriggerHasher.BuildHashString(recipe, 1, Recipient(locale: "ar"), Payload("{}"));

        Assert.Equal("recipient.locale=en", enHash);
        Assert.Equal("recipient.locale=ar", arHash);
    }

    [Fact]
    public void BuildHashString_UnknownRecipientFieldResolvesToEmpty()
    {
        var recipe = new[] { "recipient.somethingMadeUp" };
        var s = TriggerHasher.BuildHashString(recipe, 1, Recipient(), Payload("{}"));
        Assert.Equal("recipient.somethingMadeUp=", s);
    }

    [Fact]
    public void BuildHashString_UnknownTopLevelPathResolvesToEmpty()
    {
        var recipe = new[] { "doesNotExist" };
        var s = TriggerHasher.BuildHashString(recipe, 1, Recipient(), Payload("""{"wip":"x"}"""));
        Assert.Equal("doesNotExist=", s);
    }
}
