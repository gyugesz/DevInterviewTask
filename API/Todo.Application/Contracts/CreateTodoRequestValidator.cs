using FluentValidation;

namespace Todo.Application.Contracts;

public sealed class CreateTodoRequestValidator : AbstractValidator<CreateTodoRequest>
{
    public CreateTodoRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.Description)
            .MaximumLength(256)
            .When(x => x.Description is not null);

        // v12-kompatibilis: byte-ra nem használunk InclusiveBetween-t
        RuleFor(x => x.Priority)
            .Must(p => p is >= 1 and <= 3)
            .WithMessage("Priority must be between 1 and 3.");
        // Alternatíva (szintén jó): 
        // RuleFor(x => x.Priority).Transform(p => (int)p).InclusiveBetween(1, 3);
    }
}
