using System;

namespace Library.Contracts
{
    public interface ReservationExpired
    {
        Guid ReservationId { get; }
    }
}
