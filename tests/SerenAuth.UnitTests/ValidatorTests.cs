using FluentAssertions;
using SerenAuth.Application.PriorAuthorizations;
using Xunit;

namespace SerenAuth.UnitTests;

public class ValidatorTests
{
    [Fact]
    public void Create_command_rejects_unknown_cpt()
    {
        var validator = new CreatePriorAuthorizationCommandValidator();
        var cmd = new CreatePriorAuthorizationCommand("p1", "pr1", "99999", "N18.6", "BCBS", 0.9);
        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProcedureCpt");
    }

    [Fact]
    public void Create_command_rejects_unknown_icd10()
    {
        var validator = new CreatePriorAuthorizationCommandValidator();
        var cmd = new CreatePriorAuthorizationCommand("p1", "pr1", "90935", "Z99.0", "BCBS", 0.9);
        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DiagnosisIcd10");
    }

    [Fact]
    public void Create_command_rejects_out_of_range_confidence()
    {
        var validator = new CreatePriorAuthorizationCommandValidator();
        var cmd = new CreatePriorAuthorizationCommand("p1", "pr1", "90935", "N18.6", "BCBS", 1.5);
        var result = validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Create_command_accepts_valid_payload()
    {
        var validator = new CreatePriorAuthorizationCommandValidator();
        var cmd = new CreatePriorAuthorizationCommand("p1", "pr1", "90935", "N18.6", "BCBS", 0.9);
        validator.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Submit_command_requires_id()
    {
        var validator = new SubmitPriorAuthorizationCommandValidator();
        validator.Validate(new SubmitPriorAuthorizationCommand("")).IsValid.Should().BeFalse();
        validator.Validate(new SubmitPriorAuthorizationCommand("ok")).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public void List_query_rejects_out_of_range_limit(int badLimit)
    {
        var validator = new GetPriorAuthorizationsQueryValidator();
        validator.Validate(new GetPriorAuthorizationsQuery(null, null, badLimit)).IsValid.Should().BeFalse();
    }
}
