namespace NatuFurniture.Domain;

public abstract class Payment
{
    public int Id { get; }
    public decimal TotalAmount { get; protected set; }
    public decimal AmountPaid { get; protected set; }
    public decimal Outstanding => TotalAmount - AmountPaid;
    public PaymentStatus Status { get; protected set; } = PaymentStatus.Pending;
    public Order? Order { get; internal set; }

    protected Payment(int id, decimal total)
    {
        Id = id;
        TotalAmount = total;
    }

    public abstract bool Process(decimal amount);
    public abstract string Describe();
}

public class CashPayment : Payment
{
    public decimal ChangeDue { get; private set; }

    public CashPayment(int id, decimal total) : base(id, total) { }

    public override bool Process(decimal amount)
    {
        if (Status == PaymentStatus.Paid || amount < TotalAmount) return false;
        AmountPaid = TotalAmount;
        ChangeDue = amount - TotalAmount;
        Status = PaymentStatus.Paid;
        return true;
    }

    public override string Describe() =>
        $"Cash - RM {TotalAmount:N2} - {Status}{(Status == PaymentStatus.Paid ? $" (change RM {ChangeDue:N2})" : "")}";

    internal void Restore(decimal paid, PaymentStatus status, decimal change)
    {
        AmountPaid = paid; Status = status; ChangeDue = change;
    }
}

public class CreditPayment : Payment
{
    public int Instalments { get; }
    public decimal InstalmentAmount { get; private set; }
    public int InstalmentsPaid { get; private set; }
    public DateTime NextDueDate { get; private set; }
    public decimal LateFeeRate { get; } = 0.05m;
    public decimal LateFeesCharged { get; private set; }
    public int GraceDays { get; } = 7;
    public DateTime LastActionDate { get; internal set; }
    public IPaymentState State { get; private set; }

    public CreditPayment(int id, decimal total, int instalments, DateTime firstDueDate) : base(id, total)
    {
        Instalments = instalments;
        InstalmentAmount = Math.Round(total / instalments, 2);
        NextDueDate = firstDueDate;
        LastActionDate = DateTime.Today;
        State = new PendingState();
    }

    public void SetState(IPaymentState state) => State = state;
    public void CheckDue(DateTime today) => State.HandleOverdue(this, today);

    internal void SetStatus(PaymentStatus status) => Status = status;

    internal void ChargeLateFee()
    {
        var fee = Math.Round(InstalmentAmount * LateFeeRate, 2);
        TotalAmount += fee;
        LateFeesCharged += fee;
    }

    public override bool Process(decimal amount)
    {
        if (Status is PaymentStatus.Defaulted or PaymentStatus.Paid || amount <= 0) return false;

        var minimum = Math.Min(InstalmentAmount, Outstanding);
        if (amount < minimum) return false;

        AmountPaid += Math.Min(amount, Outstanding);
        InstalmentsPaid++;
        NextDueDate = NextDueDate.AddMonths(1);

        if (Outstanding <= 0)
        {
            Status = PaymentStatus.Paid;
            SetState(new PaidState());
        }
        else if (State is not PendingState)
        {
            Status = PaymentStatus.Pending; // customer caught up
            SetState(new PendingState());
        }
        return true;
    }

    public override string Describe() =>
        $"Credit - {InstalmentsPaid}/{Instalments} instalments of RM {InstalmentAmount:N2}, state: {State.Name}, " +
        $"next due {NextDueDate:dd MMM yyyy}, outstanding RM {Outstanding:N2}";

    internal void Restore(decimal total, decimal paid, PaymentStatus status, int instalmentsPaid,
        DateTime nextDue, DateTime lastAction, decimal lateFees, IPaymentState state)
    {
        TotalAmount = total;
        AmountPaid = paid;
        Status = status;
        InstalmentsPaid = instalmentsPaid;
        NextDueDate = nextDue;
        LastActionDate = lastAction;
        LateFeesCharged = lateFees;
        InstalmentAmount = Math.Round((total - lateFees) / Instalments, 2);
        State = state;
    }
}
