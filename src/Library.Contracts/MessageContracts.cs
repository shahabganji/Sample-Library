namespace Library.Contracts
{
    using MassTransit;
    using MassTransit.Topology.Topologies;


    public static class MessageContracts
    {
        static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
                return;

            GlobalTopology.Send.UseCorrelationId<BookAdded>(x => x.BookId);
            GlobalTopology.Send.UseCorrelationId<BookReservationCanceled>(x => x.BookId);
            GlobalTopology.Send.UseCorrelationId<BookCheckedOut>(x => x.BookId);
            
            GlobalTopology.Send.UseCorrelationId<ReservationRequested>(x => x.ReservationId);
            GlobalTopology.Send.UseCorrelationId<ReservationCancellationRequested>(x => x.ReservationId);
            GlobalTopology.Send.UseCorrelationId<ReservationExpired>(x => x.ReservationId);

            _initialized = true;
        }
    }
}
