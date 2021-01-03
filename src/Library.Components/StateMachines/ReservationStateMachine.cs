using Automatonymous;
using Library.Contracts;

namespace Library.Components.StateMachines
{
    // ReSharper disable UnassignedGetOnlyAutoProperty MemberCanBePrivate.Global
    public class ReservationStateMachine :
        MassTransitStateMachine<Reservation>
    {
        static ReservationStateMachine()
        {
            MessageContracts.Initialize();
        }

        public ReservationStateMachine()
        {
            InstanceState(x => x.CurrentState, Requested, Reserved);

            Event(() => BookReserved, x => x.CorrelateById(m => m.Message.ReservationId));

            Initially(
                When(ReservationRequested)
                    .Then(context =>
                    {
                        context.Instance.Created = context.Data.Timestamp;
                        context.Instance.BookId = context.Data.BookId;
                        context.Instance.MemberId = context.Data.MemberId;
                    })
                    .TransitionTo(Requested)
            );

            During(Requested,
                When(BookReserved)
                    .Then(context => context.Instance.Reserved = context.Data.Timestamp)
                    .TransitionTo(Reserved));
        }

        public State Requested { get; }
        public State Reserved { get; }

        public Event<ReservationRequested> ReservationRequested { get; }
        public Event<BookReserved> BookReserved { get; }
    }
}
