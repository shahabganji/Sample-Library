using System;
using Automatonymous;
using Library.Contracts;
using MassTransit;

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

            Schedule(() => ExpirationSchedule, reservation => reservation.ExpirationTokenId,
                configurator => configurator.Delay = TimeSpan.FromHours(24));

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
                    .Schedule(ExpirationSchedule, context => context.Init<ReservationExpired>(new
                    {
                        context.Data.ReservationId
                    }))
                    .TransitionTo(Reserved));

            During(Reserved,
                When(ReservationExpired)
                    .PublishAsync(context => context.Init<BookReservationCanceled>(new
                    {
                        context.Instance.BookId
                    }))
                    .Finalize());

            // this tells the repository to delete that instance
            SetCompletedWhenFinalized();
        }

        public State Requested { get; }
        public State Reserved { get; }

        public Schedule<Reservation, ReservationExpired> ExpirationSchedule { get; }

        public Event<ReservationRequested> ReservationRequested { get; }
        public Event<BookReserved> BookReserved { get; }
        public Event<ReservationExpired> ReservationExpired { get; }
    }
}
