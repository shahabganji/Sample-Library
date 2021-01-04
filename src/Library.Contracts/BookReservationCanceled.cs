using System;

namespace Library.Contracts
{
    public interface BookReservationCanceled
    {
        Guid BookId { get; }
    }
}
