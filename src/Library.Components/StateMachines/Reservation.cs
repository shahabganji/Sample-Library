using System;
using Automatonymous;

namespace Library.Components.StateMachines
{
    public class Reservation:
        SagaStateMachineInstance
    {
        public int CurrentState { get; set; }

        public DateTime Created { get; set; }
        public DateTime? Reserved { get; set; }

        public Guid MemberId { get; set; }
        public Guid BookId { get; set; }
        
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// We will store the scheduled reservation token in here
        /// </summary>
        public Guid? ExpirationTokenId { get; set; }
    }
}
