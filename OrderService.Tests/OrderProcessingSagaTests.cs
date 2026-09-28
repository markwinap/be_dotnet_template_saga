using FluentAssertions;
using OrderService.Application.Messages.Saga;
using OrderService.Application.Sagas;

namespace OrderService.Tests;

public class OrderProcessingSagaTests
{
    [Fact]
    public void Start_ShouldInitializeSaga_AndCascadeReserveInventory()
    {
        var orderId = Guid.NewGuid();

        var (saga, messages) = OrderProcessingSaga.Start(new OrderSubmitted(orderId));

        saga.Id.Should().Be(orderId);
        saga.Status.Should().Be("InProgress");
        saga.CurrentStep.Should().Be("ReservingInventory");
        messages.Should().HaveCount(1);
        messages[0].Should().BeOfType<ReserveInventory>().Which.OrderId.Should().Be(orderId);
    }

    [Fact]
    public void Handle_InventoryReserved_ShouldAdvanceToPayment()
    {
        var saga = CreateStartedSaga();
        var orderId = saga.Id;

        var message = saga.Handle(new InventoryReserved(orderId));

        saga.CurrentStep.Should().Be("ProcessingPayment");
        message.Should().BeOfType<ProcessPayment>().Which.OrderId.Should().Be(orderId);
    }

    [Fact]
    public void Handle_PaymentProcessed_ShouldAdvanceToShipment()
    {
        var saga = CreateStartedSaga();
        var orderId = saga.Id;
        saga.Handle(new InventoryReserved(orderId));

        var message = saga.Handle(new PaymentProcessed(orderId));

        saga.CurrentStep.Should().Be("CreatingShipment");
        message.Should().BeOfType<CreateShipment>().Which.OrderId.Should().Be(orderId);
    }

    [Fact]
    public void Handle_ShipmentCreated_ShouldCompleteSaga()
    {
        var saga = CreateStartedSaga();
        var orderId = saga.Id;
        saga.Handle(new InventoryReserved(orderId));
        saga.Handle(new PaymentProcessed(orderId));

        saga.Handle(new ShipmentCreated(orderId));

        saga.Status.Should().Be("Completed");
        saga.CurrentStep.Should().Be("Completed");
        saga.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Handle_InventoryReservationFailed_ShouldFailSaga()
    {
        var saga = CreateStartedSaga();
        var orderId = saga.Id;

        saga.Handle(new InventoryReservationFailed(orderId, "Out of stock"));

        saga.Status.Should().Be("Failed");
        saga.ErrorReason.Should().Be("Out of stock");
        saga.CompletedAtUtc.Should().NotBeNull();
    }

    private static OrderProcessingSaga CreateStartedSaga()
    {
        var orderId = Guid.NewGuid();
        var (saga, _) = OrderProcessingSaga.Start(new OrderSubmitted(orderId));
        return saga;
    }
}
