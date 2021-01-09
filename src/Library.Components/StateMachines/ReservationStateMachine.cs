using System;
using Automatonymous;
using Automatonymous.Binders;
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
            Event(() => BookCheckedOut,
                x => x.CorrelateBy((instance, context) => instance.BookId == context.Message.BookId));

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
                    .TransitionTo(Requested),
                When(ReservationExpired)
                    .Finalize()
            );

            During(Requested,
                When(BookReserved)
                    .Then(context => context.Instance.Reserved = context.Data.Timestamp)
                    .Schedule(ExpirationSchedule, context => context.Init<ReservationExpired>(new
                    {
                        context.Data.ReservationId
                    }), context => context.Data.Duration ?? TimeSpan.FromDays(1))
                    .TransitionTo(Reserved));

            During(Reserved,
                When(ReservationExpired)
                    .PublishBookReservationCanceled()
                    .Finalize(),
                When(ReservationCancellationRequested)
                    .PublishBookReservationCanceled()
                    .Unschedule(ExpirationSchedule)
                    .Finalize(),
                When(BookCheckedOut)
                    .Unschedule(ExpirationSchedule)
                    .Finalize()
            );


            // this tells the repository to delete that instance
            SetCompletedWhenFinalized();
        }

        public State Requested { get; }
        public State Reserved { get; }

        public Schedule<Reservation, ReservationExpired> ExpirationSchedule { get; }

        public Event<ReservationRequested> ReservationRequested { get; }
        public Event<BookReserved> BookReserved { get; }
        public Event<BookCheckedOut> BookCheckedOut { get; }

        public Event<ReservationExpired> ReservationExpired { get; }
        public Event<ReservationCancellationRequested> ReservationCancellationRequested { get; }
    }

    public static class ReservationStateMachineExtensions
    {
        public static EventActivityBinder<Reservation, T> PublishBookReservationCanceled<T>(
            this EventActivityBinder<Reservation, T> binder)
            where T : class
        {
            return binder.PublishAsync(context => context.Init<BookReservationCanceled>(new
            {
                ReservationId = context.CorrelationId,
                context.Instance.BookId
            }));
        }
    }
}
