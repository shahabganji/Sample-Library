using System;

namespace Library.Contracts
{
    public interface BookReservationCanceled
    {
        Guid ReservationId { get; }
        Guid BookId { get; }
    }
}
