namespace NatuFurniture.Domain;

// Plain data shapes used only for saving/loading the store as JSON.
internal record StaffDto(int Id, string Name, string Email, string PasswordHash, StaffRole Role, int FailedLogins);
internal record CustomerDto(int Id, string Name, string Email, string Address, string Phone, int Warnings, bool Blacklisted);
internal record FurnitureDto(int Id, string Type, string Name, decimal BasePrice, decimal CurrentPrice,
    FurnitureCondition Condition, FurnitureStatus Status, DateTime AddedOn, int Stock, int A, int B, string S);
internal record PaymentDto(int Id, string Kind, decimal Total, decimal Paid, PaymentStatus Status, decimal ChangeDue,
    int Instalments, int InstalmentsPaid, DateTime NextDue, DateTime LastAction, decimal LateFees, string StateName);
internal record OrderDto(int Id, int CustomerId, OrderStatus Status, DateTime PlacedOn,
    List<FurnitureDto> Items, PaymentDto Payment);
internal record SnapshotDto(List<StaffDto> Staff, List<CustomerDto> Customers, List<FurnitureDto> Catalogue,
    List<OrderDto> Orders, string Pricing);
