using bankconnector.moniepoint.Exceptions;

namespace bankconnector.moniepoint.Tests;

public class ExceptionTests
{

    [Fact]
    public void TeamAptEncryptionException_Message_IsPreserved()
    {
        var ex = new TeamAptEncryptionException("PGP key not found");
        Assert.Equal("PGP key not found", ex.Message);
    }

    [Fact]
    public void TeamAptEncryptionException_WithInner_InnerExceptionSet()
    {
        var inner = new IOException("disk error");
        var ex    = new TeamAptEncryptionException("wrap", inner);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void TeamAptEncryptionException_IsException()
    {
        Assert.IsAssignableFrom<Exception>(new TeamAptEncryptionException("x"));
    }


    [Fact]
    public void TeamAptApiException_Message_IsPreserved()
    {
        var ex = new TeamAptApiException("null envelope");
        Assert.Equal("null envelope", ex.Message);
    }

    [Fact]
    public void TeamAptApiException_StatusCode_IsPreserved()
    {
        var ex = new TeamAptApiException("error", statusCode: "96");
        Assert.Equal("96", ex.StatusCode);
    }

    [Fact]
    public void TeamAptApiException_ResponseBody_IsPreserved()
    {
        var ex = new TeamAptApiException("error", responseBody: "{\"err\":\"x\"}");
        Assert.Equal("{\"err\":\"x\"}", ex.ResponseBody);
    }

    [Fact]
    public void TeamAptApiException_NullStatusCodeAndBody_PropertiesAreNull()
    {
        var ex = new TeamAptApiException("plain message");
        Assert.Null(ex.StatusCode);
        Assert.Null(ex.ResponseBody);
    }

    [Fact]
    public void TeamAptApiException_WithInner_InnerExceptionSet()
    {
        var inner = new HttpRequestException("timeout");
        var ex    = new TeamAptApiException("wrap", inner, statusCode: "97");
        Assert.Same(inner, ex.InnerException);
        Assert.Equal("97", ex.StatusCode);
    }

    [Fact]
    public void TeamAptApiException_IsException()
    {
        Assert.IsAssignableFrom<Exception>(new TeamAptApiException("x"));
    }


    [Fact]
    public void TeamAptPendingTimeoutException_UniqueReference_IsPreserved()
    {
        var ex = new TeamAptPendingTimeoutException("APT0001526042412345678", 30);
        Assert.Equal("APT0001526042412345678", ex.UniqueReference);
    }

    [Fact]
    public void TeamAptPendingTimeoutException_AttemptsExhausted_IsPreserved()
    {
        var ex = new TeamAptPendingTimeoutException("REF001", 5);
        Assert.Equal(5, ex.AttemptsExhausted);
    }

    [Fact]
    public void TeamAptPendingTimeoutException_Message_ContainsReferenceAndAttempts()
    {
        var ex = new TeamAptPendingTimeoutException("REF-XYZ", 10);
        Assert.Contains("REF-XYZ", ex.Message);
        Assert.Contains("10", ex.Message);
    }

    [Fact]
    public void TeamAptPendingTimeoutException_IsException()
    {
        Assert.IsAssignableFrom<Exception>(new TeamAptPendingTimeoutException("r", 1));
    }


    [Fact]
    public void ApiException_HttpStatusCode_IsPreserved()
    {
        var ex = new ApiException("Not Found", 404);
        Assert.Equal(404, ex.HttpStatusCode);
    }

    [Fact]
    public void ApiException_Message_IsPreserved()
    {
        var ex = new ApiException("Bad Gateway", 502);
        Assert.Equal("Bad Gateway", ex.Message);
    }

    [Fact]
    public void ApiException_IsException()
    {
        Assert.IsAssignableFrom<Exception>(new ApiException("x", 500));
    }
}
