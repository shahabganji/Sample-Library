using System;

namespace Library.Contracts
{
    public interface BookReserved
    {
        Guid ReservationId { get; }
        DateTime Timestamp { get; }
        Guid MemberId { get; }
        Guid BookId { get; }
    }
}