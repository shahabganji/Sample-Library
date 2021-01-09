using System;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Components.Tests
{
    using System.Threading.Tasks;
    using Contracts;
    using MassTransit;
    using MassTransit.Testing;
    using NUnit.Framework;
    using StateMachines;


    public class When_a_reservation_is_requested :
        StateMachineTestFixture<ReservationStateMachine, Reservation>
    {
        [Test]
        public async Task Should_create_a_saga_instance()
        {
            var bookId = NewId.NextGuid();
            var reservationId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<ReservationRequested>(new
            {
                InVar.Timestamp,
                ReservationId = reservationId,
                MemberId = memberId,
                BookId = bookId
            });

            Assert.IsTrue(await TestHarness.Consumed.Any<ReservationRequested>(), "Message not consumed");

            Assert.IsTrue(await SagaHarness.Consumed.Any<ReservationRequested>(), "Message not consumed by saga");

            Assert.That(await SagaHarness.Created.Any(x => x.CorrelationId == reservationId));

            var instance = SagaHarness.Created.ContainsInState(reservationId, Machine, Machine.Requested);
            Assert.IsNotNull(instance, "Saga instance not found");

            var existsId = await SagaHarness.Exists(reservationId, x => x.Requested);
            Assert.IsTrue(existsId.HasValue, "Saga did not exist");
        }
    }

    public class When_a_book_reservation_is_requested_for_an_available_book :
        StateMachineTestFixture<ReservationStateMachine, Reservation>
    {
        private IStateMachineSagaTestHarness<Book, BookStateMachine> BookSagaHarness;
        private BookStateMachine BookMachine;

        [Test]
        public async Task Should_reserve_the_book()
        {
            var bookId = NewId.NextGuid();
            var reservationId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookAdded>(new
            {
                BookId = bookId,
                Isbn = "0307969959",
                Title = "Neuromancer"
            });

            var bookExistsId = await BookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(bookExistsId.HasValue, "Book did not exist");


            await TestHarness.Bus.Publish<ReservationRequested>(new
            {
                InVar.Timestamp,
                ReservationId = reservationId,
                MemberId = memberId,
                BookId = bookId
            });

            Assert.IsTrue(await BookSagaHarness.Consumed.Any<ReservationRequested>(), "Message not consumed by saga");
            Assert.IsTrue(await SagaHarness.Consumed.Any<ReservationRequested>(), "Message not consumed by saga");

            // var reservation = SagaHarness.Sagas.(reservationId,Machine , x => x.Reserved);
            var reservation = await SagaHarness.Exists(reservationId, x => x.Reserved);
            Assert.IsNotNull(reservation, "Reservation did not exists");
        }

        [OneTimeSetUp]
        public void TestSetup()
        {
            BookSagaHarness = Provider.GetRequiredService<IStateMachineSagaTestHarness<Book, BookStateMachine>>();
            BookMachine = Provider.GetRequiredService<BookStateMachine>();
        }

        protected override void ConfigureMassTransit(IServiceCollectionBusConfigurator busConfigurator)
        {
            busConfigurator.AddSagaStateMachine<BookStateMachine, Book>()
                .InMemoryRepository();

            busConfigurator.AddPublishMessageScheduler();

            busConfigurator.AddSagaStateMachineTestHarness<BookStateMachine, Book>();
        }
    }

    public class When_reservation_expired :
        StateMachineTestFixture<ReservationStateMachine, Reservation>
    {
        private IStateMachineSagaTestHarness<Book, BookStateMachine> BookSagaHarness;
        private BookStateMachine BookMachine;

        [Test]
        public async Task Should_mark_the_book_as_available()
        {
            var bookId = NewId.NextGuid();
            var reservationId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookAdded>(new
            {
                BookId = bookId,
                Isbn = "0307969959",
                Title = "Neuromancer"
            });

            var bookExistsId = await BookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(bookExistsId.HasValue, "Book did not exist");

            await TestHarness.Bus.Publish<ReservationRequested>(new
            {
                InVar.Timestamp,
                ReservationId = reservationId,
                MemberId = memberId,
                BookId = bookId
            });

            var reservation = await SagaHarness.Exists(reservationId, x => x.Reserved);
            Assert.IsNotNull(reservation, "Reservation was not reserved");
            bookExistsId = await BookSagaHarness.Exists(bookId, x => x.Reserved);
            Assert.IsTrue(bookExistsId.HasValue, "Book was not reserved");

            await AdvanceSystemTime(TimeSpan.FromHours(24));

            var notExists = await SagaHarness.NotExists(reservationId);
            Assert.IsFalse(notExists.HasValue);

            bookExistsId = await BookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(bookExistsId.HasValue, "Book was not available after reservation expired");
        }

        [OneTimeSetUp]
        public void TestSetup()
        {
            BookSagaHarness = Provider.GetRequiredService<IStateMachineSagaTestHarness<Book, BookStateMachine>>();
            BookMachine = Provider.GetRequiredService<BookStateMachine>();
        }

        protected override void ConfigureMassTransit(IServiceCollectionBusConfigurator busConfigurator)
        {
            busConfigurator.AddSagaStateMachine<BookStateMachine, Book>()
                .InMemoryRepository();

            busConfigurator.AddPublishMessageScheduler();

            busConfigurator.AddSagaStateMachineTestHarness<BookStateMachine, Book>();
        }
    }

    public class When_reservation_expired_with_custom_duration :
        StateMachineTestFixture<ReservationStateMachine, Reservation>
    {
        private IStateMachineSagaTestHarness<Book, BookStateMachine> BookSagaHarness;
        private BookStateMachine BookMachine;

        [Test]
        public async Task Should_mark_the_book_as_available()
        {
            var bookId = NewId.NextGuid();
            var reservationId = NewId.NextGuid();
            var memberId = NewId.NextGuid();
            var duration = TimeSpan.FromHours(8);

            await TestHarness.Bus.Publish<BookAdded>(new
            {
                BookId = bookId,
                Isbn = "0307969959",
                Title = "Neuromancer"
            });

            var bookExistsId = await BookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(bookExistsId.HasValue, "Book did not exist");

            await TestHarness.Bus.Publish<ReservationRequested>(new
            {
                InVar.Timestamp,
                ReservationId = reservationId,
                MemberId = memberId,
                BookId = bookId,

                Duration = duration
            });

            var reservation = await SagaHarness.Exists(reservationId, x => x.Reserved);
            Assert.IsNotNull(reservation, "Reservation was not reserved");
            bookExistsId = await BookSagaHarness.Exists(bookId, x => x.Reserved);
            Assert.IsTrue(bookExistsId.HasValue, "Book was not reserved");

            await AdvanceSystemTime(TimeSpan.FromHours(24));

            bookExistsId = await BookSagaHarness.Exists(bookId, x => x.Reserved);
            Assert.IsTrue(bookExistsId.HasValue, "Book is not still reserved");

            await AdvanceSystemTime(TimeSpan.FromHours(24));

            var notExists = await SagaHarness.NotExists(reservationId);
            Assert.IsFalse(notExists.HasValue);

            bookExistsId = await BookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(bookExistsId.HasValue, "Book was not available after reservation expired");
        }

        [OneTimeSetUp]
        public void TestSetup()
        {
            BookSagaHarness = Provider.GetRequiredService<IStateMachineSagaTestHarness<Book, BookStateMachine>>();
            BookMachine = Provider.GetRequiredService<BookStateMachine>();
        }

        protected override void ConfigureMassTransit(IServiceCollectionBusConfigurator busConfigurator)
        {
            busConfigurator.AddSagaStateMachine<BookStateMachine, Book>()
                .InMemoryRepository();

            busConfigurator.AddPublishMessageScheduler();

            busConfigurator.AddSagaStateMachineTestHarness<BookStateMachine, Book>();
        }
    }

    public class When_reservation_is_canceled :
        StateMachineTestFixture<ReservationStateMachine, Reservation>
    {
        private IStateMachineSagaTestHarness<Book, BookStateMachine> BookSagaHarness;
        private BookStateMachine BookMachine;

        [Test]
        public async Task Should_mark_book_as_available()
        {
            var bookId = NewId.NextGuid();
            var reservationId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookAdded>(new
            {
                BookId = bookId,
                Isbn = "0307969959",
                Title = "Neuromancer"
            });

            var bookExistsId = await BookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(bookExistsId.HasValue, "Book did not exist");

            await TestHarness.Bus.Publish<ReservationRequested>(new
            {
                InVar.Timestamp,
                ReservationId = reservationId,
                MemberId = memberId,
                BookId = bookId
            });

            var reservation = await SagaHarness.Exists(reservationId, x => x.Reserved);
            Assert.IsNotNull(reservation, "Reservation was not reserved");
            bookExistsId = await BookSagaHarness.Exists(bookId, x => x.Reserved);
            Assert.IsTrue(bookExistsId.HasValue, "Book was not reserved");

            await TestHarness.Bus.Publish<ReservationCancellationRequested>(new
            {
                InVar.Timestamp,
                ReservationId = reservationId,
            });

            var notExists = await SagaHarness.NotExists(reservationId);
            Assert.IsFalse(notExists.HasValue);

            bookExistsId = await BookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(bookExistsId.HasValue, "Book was not available after reservation expired");
        }

        [OneTimeSetUp]
        public void TestSetup()
        {
            BookSagaHarness = Provider.GetRequiredService<IStateMachineSagaTestHarness<Book, BookStateMachine>>();
            BookMachine = Provider.GetRequiredService<BookStateMachine>();
        }

        protected override void ConfigureMassTransit(IServiceCollectionBusConfigurator busConfigurator)
        {
            busConfigurator.AddSagaStateMachine<BookStateMachine, Book>()
                .InMemoryRepository();

            busConfigurator.AddPublishMessageScheduler();

            busConfigurator.AddSagaStateMachineTestHarness<BookStateMachine, Book>();
        }
    }

    public class When_a_reserved_book_is_checked_out :
        StateMachineTestFixture<ReservationStateMachine, Reservation>
    {
        private IStateMachineSagaTestHarness<Book, BookStateMachine> BookSagaHarness;
        private BookStateMachine BookMachine;

        [Test]
        public async Task Should_reservation_be_removed()
        {
            var bookId = NewId.NextGuid();
            var reservationId = NewId.NextGuid();
            var memberId = NewId.NextGuid();

            await TestHarness.Bus.Publish<BookAdded>(new
            {
                BookId = bookId,
                Isbn = "0307969959",
                Title = "Neuromancer"
            });

            var bookExistsId = await BookSagaHarness.Exists(bookId, x => x.Available);
            Assert.IsTrue(bookExistsId.HasValue, "Book did not exist");

            await TestHarness.Bus.Publish<ReservationRequested>(new
            {
                InVar.Timestamp,
                ReservationId = reservationId,
                MemberId = memberId,
                BookId = bookId
            });

            var reservation = await SagaHarness.Exists(reservationId, x => x.Reserved);
            Assert.IsNotNull(reservation, "Reservation was not reserved");
            bookExistsId = await BookSagaHarness.Exists(bookId, x => x.Reserved);
            Assert.IsTrue(bookExistsId.HasValue, "Book was not reserved");

            await TestHarness.Bus.Publish<BookCheckedOut>(new
            {
                MemberId = memberId,
                BookId = bookId
            });

            var notExists = await SagaHarness.NotExists(reservationId);
            Assert.IsFalse(notExists.HasValue);

            bookExistsId = await BookSagaHarness.Exists(bookId, x => x.CheckedOut);
            Assert.IsTrue(bookExistsId.HasValue, "Book was not CheckedOut");
        }

        [OneTimeSetUp]
        public void TestSetup()
        {
            BookSagaHarness = Provider.GetRequiredService<IStateMachineSagaTestHarness<Book, BookStateMachine>>();
            BookMachine = Provider.GetRequiredService<BookStateMachine>();
        }

        protected override void ConfigureMassTransit(IServiceCollectionBusConfigurator busConfigurator)
        {
            busConfigurator.AddSagaStateMachine<BookStateMachine, Book>()
                .InMemoryRepository();

            busConfigurator.AddPublishMessageScheduler();

            busConfigurator.AddSagaStateMachineTestHarness<BookStateMachine, Book>();
        }
    }
}
