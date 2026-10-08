using FluentAssertions;
using FunBooksAndVideos.Api.ErrorHandling;

namespace FunBooksAndVideos.Api.Tests.ErrorHandling;

public sealed class ProblemCodesTests
{
    [Theory]
    [InlineData(400, "request.invalid")]
    [InlineData(404, "resource.not_found")]
    [InlineData(405, "method.not_allowed")]
    [InlineData(409, "conflict")]
    [InlineData(413, "request.too_large")]
    [InlineData(415, "media_type.unsupported")]
    [InlineData(422, "unprocessable")]
    [InlineData(500, "internal_error")]
    public void Known_statuses_map_to_stable_codes(int status, string expected)
    {
        ProblemCodes.ForStatus(status).Should().Be(expected);
    }

    [Theory]
    [InlineData(402, "http.402")]
    [InlineData(418, "http.418")]
    [InlineData(503, "http.503")]
    public void Other_statuses_fall_back_to_a_generic_code(int status, string expected)
    {
        ProblemCodes.ForStatus(status).Should().Be(expected);
    }
}
