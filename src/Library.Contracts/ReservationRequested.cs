using System;

namespace Library.Contracts
{
    public interface ReservationRequested
    {
        Guid ReservationId { get; }
        DateTime Timestamp { get; }
        Guid MemberId { get; }
        Guid BookId { get; }
    }
    
    public interface BookReserved
    {
        Guid ReservationId { get; }
        DateTime Timestamp { get; }
        Guid MemberId { get; }
        Guid BookId { get; }
    }

}
