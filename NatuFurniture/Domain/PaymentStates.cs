namespace NatuFurniture.Domain;

public class PendingState : IPaymentState
{
    public string Name => "Pending";

    public void HandleOverdue(CreditPayment payment, DateTime today)
    {
        if (today.Date <= payment.NextDueDate.Date) return;

        payment.SetStatus(PaymentStatus.Overdue);
        payment.LastActionDate = today.Date;
        payment.SetState(new OverdueState());
        payment.Order?.Customer.Update(
            $"Order #{payment.Order.Id}: your instalment due {payment.NextDueDate:dd MMM yyyy} is overdue. " +
            $"You have a {payment.GraceDays}-day grace period before a late fee applies.");
    }
}

public class OverdueState : IPaymentState
{
    public string Name => "Overdue";

    public void HandleOverdue(CreditPayment payment, DateTime today)
    {
        if (today.Date < payment.NextDueDate.Date.AddDays(payment.GraceDays)) return;

        payment.ChargeLateFee();
        payment.LastActionDate = today.Date;
        var warning = new WarningState(1);
        payment.SetState(warning);
        warning.Enter(payment);
    }
}

public class WarningState : IPaymentState
{
    public int Level { get; }
    public string Name => $"Warning {Level}";

    public WarningState(int level) => Level = level;

    // Called when this state is entered: records the warning and tells the customer.
    internal void Enter(CreditPayment payment)
    {
        var customer = payment.Order?.Customer;
        if (customer == null) return;
        customer.AddWarning();
        customer.Update($"Warning {Level} of 3 for order #{payment.Order!.Id}: instalment still unpaid. " +
                        (Level == 3 ? "Missing the next check means the furniture will be repossessed." : "Please pay as soon as possible."));
    }

    public void HandleOverdue(CreditPayment payment, DateTime today)
    {
        if (today.Date < payment.LastActionDate.Date.AddDays(payment.GraceDays)) return;

        if (Level < 3)
        {
            var next = new WarningState(Level + 1);
            payment.LastActionDate = today.Date;
            payment.SetState(next);
            next.Enter(payment);
        }
        else
        {
            var repossessed = new RepossessedState();
            payment.SetState(repossessed);
            repossessed.HandleOverdue(payment, today);
        }
    }
}

public class PaidState : IPaymentState
{
    public string Name => "Paid";
    public void HandleOverdue(CreditPayment payment, DateTime today) { /* final state */ }
}

public class RepossessedState : IPaymentState
{
    public string Name => "Repossessed";

    public void HandleOverdue(CreditPayment payment, DateTime today)
    {
        if (payment.Status == PaymentStatus.Defaulted || payment.Order == null) return;

        var store = Store.Instance;
        var order = payment.Order;

        foreach (var item in order.Items)
        {
            var resalePrice = store.PricingStrategy.CalculatePrice(item);
            item.Repossess(resalePrice);
            store.Inventory.Restock(item, 1);
        }

        order.Customer.Blacklist();
        payment.SetStatus(PaymentStatus.Defaulted);
        order.Status = OrderStatus.Cancelled;
        order.Customer.Update($"Order #{order.Id}: furniture repossessed after the third missed warning. " +
                              "You can no longer buy on credit.");
        AuditLog.Instance.Log(null, $"Repossessed order #{order.Id} ({order.Customer.Name})");
    }
}

public static class PaymentStates
{
    public static IPaymentState Parse(string name)
    {
        if (name.StartsWith("Warning ") && int.TryParse(name.AsSpan(8), out var level))
            return new WarningState(level);

        return name switch
        {
            "Overdue" => new OverdueState(),
            "Paid" => new PaidState(),
            "Repossessed" => new RepossessedState(),
            _ => new PendingState()
        };
    }
}
