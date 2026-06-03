using bankconnector.moniepoint.Helpers;

namespace bankconnector.moniepoint.Tests;

public class UniqueReferenceGeneratorTests
{
    private const string ValidPrefix = "APT00015";


    [Theory]
    [InlineData("")]
    [InlineData("SHORT")]
    [InlineData("TOOLONGPREFIX")]
    [InlineData("1234567")]    
    [InlineData("123456789")]  
    public void Generate_InvalidPrefixLength_ThrowsArgumentException(string prefix)
    {
        var ex = Assert.Throws<ArgumentException>(() => UniqueReferenceGenerator.Generate(prefix));
        Assert.Contains("8 characters", ex.Message);
    }

    [Fact]
    public void Generate_ValidPrefix_DoesNotThrow()
    {
        var result = UniqueReferenceGenerator.Generate(ValidPrefix);
        Assert.NotNull(result);
    }


    [Fact]
    public void Generate_Result_StartsWithPrefix()
    {
        var result = UniqueReferenceGenerator.Generate(ValidPrefix);
        Assert.StartsWith(ValidPrefix, result);
    }

    [Fact]
    public void Generate_Result_HasCorrectTotalLength()
    {
        var result = UniqueReferenceGenerator.Generate(ValidPrefix);
        Assert.Equal(32, result.Length);
    }

    [Fact]
    public void Generate_TimestampSegment_IsNumeric()
    {
        var result = UniqueReferenceGenerator.Generate(ValidPrefix);
        var timestamp = result.Substring(ValidPrefix.Length, 12);
        Assert.True(long.TryParse(timestamp, out _), $"Timestamp '{timestamp}' is not numeric");
    }

    [Fact]
    public void Generate_SuffixSegment_ContainsOnlyAllowedChars()
    {
        const string allowed = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var result = UniqueReferenceGenerator.Generate(ValidPrefix);
        var suffix = result.Substring(ValidPrefix.Length + 12, 12);
        Assert.All(suffix, c => Assert.Contains(c, allowed));
    }


    [Fact]
    public void Generate_CalledMultipleTimes_ProducesUniqueValues()
    {
        var results = Enumerable.Range(0, 200)
            .Select(_ => UniqueReferenceGenerator.Generate(ValidPrefix))
            .ToHashSet();

        Assert.True(results.Count >= 199, $"Too many collisions; unique count={results.Count}");
    }


    [Theory]
    [InlineData("ABCDEFGH")]
    [InlineData("12345678")]
    [InlineData("ZONE0001")]
    public void Generate_DifferentValidPrefixes_AllWork(string prefix)
    {
        var result = UniqueReferenceGenerator.Generate(prefix);
        Assert.StartsWith(prefix, result);
        Assert.Equal(32, result.Length);
    }
}
