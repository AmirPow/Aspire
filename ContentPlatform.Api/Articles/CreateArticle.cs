using Carter;
using ContentPlatform.Api.AspireHub;
using ContentPlatform.Api.Database;
using ContentPlatform.Api.Entities;
using Contracts;
using FluentValidation;
using Mapster;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Shared;

namespace ContentPlatform.Api.Articles;

public static class CreateArticle
{
    public class Request
    {
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new();
    }

    public class Command : IRequest<Result<Guid>>
    {
        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new();
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(c => c.Title).NotEmpty();
            RuleFor(c => c.Content).NotEmpty();
        }
    }

    internal sealed class Handler(
	    ApplicationDbContext dbContext,
	    IValidator<Command> validator,
	    IPublishEndpoint publishEndpoint)
	    : IRequestHandler<Command, Result<Guid>>
    {
	    public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var validationResult = validator.Validate(request);
            if (!validationResult.IsValid)
            {
                return Result.Failure<Guid>(new Error(
                    "CreateArticle.Validation",
                    validationResult.ToString()));
            }

            var article = new Article
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Content = request.Content,
                Tags = request.Tags,
                CreatedOnUtc = DateTime.UtcNow
            };

            dbContext.Add(article);

            await dbContext.SaveChangesAsync(cancellationToken);

            await publishEndpoint.Publish(
                new ArticleCreatedEvent
                {
                    Id = article.Id,
                    CreatedOnUtc = article.CreatedOnUtc
                },
                cancellationToken);

            return article.Id;
        }
    }
}

public class CreateArticleEndpoint(IHubContext<NotificationHub> hubContext) : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/articles", async (CreateArticle.Request request, ISender sender) =>
        {
            var command = request.Adapt<CreateArticle.Command>();

            var result = await sender.Send(command);

            if (result.IsFailure)
            {
                return Results.BadRequest(result.Error);
            }
            await hubContext.Clients.All.SendAsync("Update");
			return Results.Ok(result.Value);
        });
    }
}
