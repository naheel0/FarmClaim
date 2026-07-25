using FarmClaim.Application.Features.Payments.DTOs;
using MediatR;

namespace FarmClaim.Application.Features.Payments.Commands.ProcessWebhookEvent
{
    public record ProcessWebhookEventCommand(
        RazorpayWebhookEventDto Event,
        string RawPayload,
        string Signature
    ) : IRequest<bool>;
}