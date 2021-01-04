using MassTransit;

namespace Library.Components.StateMachines
{
    using Automatonymous;
    using Automatonymous.Binders;
    using Contracts;


    // ReSharper disable UnassignedGetOnlyAutoProperty MemberCanBePrivate.Global
    public sealed class BookStateMachine :
        MassTransitStateMachine<Book>
    {
        static BookStateMachine()
        {
            MessageContracts.Initialize();
        }

        public BookStateMachine()
        {
            InstanceState(x => x.CurrentState, Available, Reserved);

            Event(() => ReservationRequested, e => e.CorrelateById(m => m.Message.BookId));

            Initially(
                When(Added)
                    .CopyDataToInstance()
                    .TransitionTo(Available));

            During(Available,
                When(ReservationRequested)
                    .Then(context => { })
                    .PublishAsync(context => context.Init<BookReserved>(new
                    {
                        // this is different than the timestamp of the requested event
                        InVar.Timestamp,

                        context.Data.ReservationId,
                        context.Data.MemberId,
                        context.Data.BookId
                    }))
                    .TransitionTo(Reserved)
                );

            During(Reserved,
                When(BookReservationCanceled)
                    .TransitionTo(Available));
        }

        public Event<BookAdded> Added { get; }
        public Event<ReservationRequested> ReservationRequested { get; }
        public Event<BookReservationCanceled> BookReservationCanceled { get; }

        public State Available { get; }
        public State Reserved { get; }
    }


    public static class BookStateMachineExtensions
    {
        public static EventActivityBinder<Book, BookAdded> CopyDataToInstance(
            this EventActivityBinder<Book, BookAdded> binder)
        {
            return binder.Then(x =>
            {
                x.Instance.DateAdded = x.Data.Timestamp.Date;
                x.Instance.Title = x.Data.Title;
                x.Instance.Isbn = x.Data.Isbn;
            });
        }
    }
}
