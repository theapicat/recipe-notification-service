using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Service.Consumers.UserActions;
using Service.Extensions;
using Shouldly;

namespace Tests.Consumers;

// Kønavnene er en kontrakt mot RabbitMQ: endres de, opprettes nye køer og de gamle blir liggende og samle meldinger.
// Testene bruker det ekte oppsettet i MassTransitExtensions, ikke en egen kopi av formatteren.
public class EndpointNamingTests
{
    private static readonly Type[] ConsumerTypes = typeof(UserRegisteredConsumer).Assembly.GetTypes()
        .Where(t => t is { IsClass: true, IsAbstract: false } &&
                    t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>)))
        .OrderBy(t => t.Name)
        .ToArray();

    private readonly IEndpointNameFormatter _formatter;

    public EndpointNamingTests()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var services = new ServiceCollection();
        services.AddMassTransitServices(configuration);

        _formatter = services.BuildServiceProvider().GetRequiredService<IEndpointNameFormatter>();
    }

    public static TheoryData<Type> AllConsumers => new(ConsumerTypes);

    private string QueueNameFor(Type consumerType)
    {
        return (string)typeof(IEndpointNameFormatter)
            .GetMethod(nameof(IEndpointNameFormatter.Consumer))!
            .MakeGenericMethod(consumerType)
            .Invoke(_formatter, null)!;
    }

    [Fact]
    public void QueuePrefix_ShouldBeLowercase()
    {
        MassTransitExtensions.QueuePrefix.ShouldBe("notification-");
        MassTransitExtensions.QueuePrefix.ShouldBe(MassTransitExtensions.QueuePrefix.ToLowerInvariant());
    }

    [Fact]
    public void ServiceAssembly_ShouldContainAllConsumers()
    {
        // Vakt: finner refleksjonen ikke consumerne, ville testene under passert uten å sjekke noe
        ConsumerTypes.Length.ShouldBe(22);
    }

    [Theory]
    [MemberData(nameof(AllConsumers))]
    public void EveryConsumerQueue_ShouldStartWithNotificationPrefix(Type consumerType)
    {
        QueueNameFor(consumerType).ShouldStartWith(MassTransitExtensions.QueuePrefix, Case.Sensitive);
    }

    [Fact]
    public void ConsumerQueueNames_ShouldBeUnique()
    {
        var names = ConsumerTypes.Select(QueueNameFor).ToList();

        names.Distinct().Count().ShouldBe(names.Count);
    }

    [Theory]
    // Slettehendelsene konsumeres også av recipe-core-api (køer "CoreApi-..."), så notification må ha egne køer
    [InlineData(typeof(AccountDeletedByUserConsumer), "notification-AccountDeletedByUser")]
    [InlineData(typeof(Service.Consumers.SystemActions.AccountDeletedBySystemConsumer),
        "notification-AccountDeletedBySystem")]
    [InlineData(typeof(Service.Consumers.AdminActions.UserAccountDeletedByAdminConsumer),
        "notification-UserAccountDeletedByAdmin")]
    [InlineData(typeof(Service.Consumers.AdminActions.UserDeletedAndBlacklistedByAdminConsumer),
        "notification-UserDeletedAndBlacklistedByAdmin")]
    // Et utvalg av de øvrige
    [InlineData(typeof(UserRegisteredConsumer), "notification-UserRegistered")]
    [InlineData(typeof(ContactFormSubmittedConsumer), "notification-ContactFormSubmitted")]
    [InlineData(typeof(Service.Consumers.NotificationManagement.RetryFailedNotificationCommandConsumer),
        "notification-RetryFailedNotificationCommand")]
    public void ConsumerQueue_ShouldHaveExpectedName(Type consumerType, string expectedQueueName)
    {
        QueueNameFor(consumerType).ShouldBe(expectedQueueName);
    }
}
