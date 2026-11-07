namespace NatuFurniture.Domain;

public class StoreException : Exception
{
    public StoreException(string message) : base(message) { }
}

public class OutOfStockException : StoreException
{
    public OutOfStockException(string itemName) : base($"'{itemName}' is out of stock.") { }
}

public class AuthenticationException : StoreException
{
    public AuthenticationException(string message) : base(message) { }
}
