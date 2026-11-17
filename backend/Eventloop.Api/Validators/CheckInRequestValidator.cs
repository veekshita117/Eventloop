using Eventloop.Core.DTOs.CheckIn;
using FluentValidation;

namespace Eventloop.Api.Validators;

public class CheckInRequestValidator : AbstractValidator<CheckInRequestDto>
{
    public CheckInRequestValidator()
    {
        RuleFor(x => x.TicketCode)
            .NotEmpty().WithMessage("TicketCode is required.");
    }
}
